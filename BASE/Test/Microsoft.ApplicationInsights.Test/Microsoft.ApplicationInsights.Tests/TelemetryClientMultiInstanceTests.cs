namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Trace;
    using Xunit;

    /// <summary>
    /// Regression tests for issue #3163 and the broader requirement that multiple
    /// <see cref="TelemetryClient"/> instances can share a single <see cref="TelemetryConfiguration"/>
    /// without throwing, and that each client's per-instance <see cref="TelemetryContext"/>
    /// is honored at the call site rather than leaking globally through the OpenTelemetry pipeline.
    /// </summary>
    [Collection("TelemetryClientTests")]
    public class TelemetryClientMultiInstanceTests : IDisposable
    {
        private readonly List<LogRecord> logItems = new List<LogRecord>();
        private readonly List<Activity> activityItems = new List<Activity>();
        private readonly TelemetryConfiguration configuration;

        public TelemetryClientMultiInstanceTests()
        {
            this.configuration = new TelemetryConfiguration
            {
                ConnectionString = "InstrumentationKey=" + Guid.NewGuid(),
                SamplingRatio = 1.0f,
            };

            this.configuration.ConfigureOpenTelemetryBuilder(b => b
                .WithLogging(l => l.AddInMemoryExporter(this.logItems))
                .WithTracing(t => t.AddInMemoryExporter(this.activityItems)));
        }

        public void Dispose()
        {
            this.logItems.Clear();
            this.activityItems.Clear();
            this.configuration?.Dispose();
        }

        /// <summary>
        /// Regression for issue #3163: constructing a second TelemetryClient against an
        /// already-built TelemetryConfiguration must not throw. Prior to this fix, the
        /// non-DI constructor unconditionally called PrependOpenTelemetryBuilderConfiguration,
        /// which threw InvalidOperationException after the first Build.
        /// </summary>
        [Fact]
        public void SecondTelemetryClient_AgainstBuiltConfiguration_DoesNotThrow()
        {
            var first = new TelemetryClient(this.configuration);

            // Should NOT throw "Configuration cannot be modified after it has been built."
            var second = new TelemetryClient(this.configuration);
            var third = new TelemetryClient(this.configuration);

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotNull(third);
            Assert.NotSame(first, second);
            Assert.NotSame(second, third);
            Assert.Same(first.TelemetryConfiguration, second.TelemetryConfiguration);
        }

        /// <summary>
        /// Constructing many TelemetryClients in parallel against the same configuration
        /// is safe.
        /// </summary>
        [Fact]
        public void ParallelTelemetryClientConstruction_DoesNotThrow()
        {
            // Ensure config is built first (idempotent Build).
            _ = new TelemetryClient(this.configuration);

            var clients = new TelemetryClient[32];
            Parallel.For(0, clients.Length, i =>
            {
                clients[i] = new TelemetryClient(this.configuration);
            });

            Assert.All(clients, c => Assert.NotNull(c));
        }

        /// <summary>
        /// Each TelemetryClient has its own TelemetryContext instance.
        /// Mutating one client's Context must not affect another client's Context.
        /// </summary>
        [Fact]
        public void EachTelemetryClient_HasIsolatedContext()
        {
            var clientA = new TelemetryClient(this.configuration);
            var clientB = new TelemetryClient(this.configuration);

            clientA.Context.Cloud.RoleName = "role-A";
            clientB.Context.Cloud.RoleName = "role-B";
            clientA.Context.User.Id = "user-A";
            clientB.Context.User.Id = "user-B";

            Assert.NotSame(clientA.Context, clientB.Context);
            Assert.Equal("role-A", clientA.Context.Cloud.RoleName);
            Assert.Equal("role-B", clientB.Context.Cloud.RoleName);
            Assert.Equal("user-A", clientA.Context.User.Id);
            Assert.Equal("user-B", clientB.Context.User.Id);
        }

        /// <summary>
        /// Per-client TelemetryContext must be enriched at the call site of each Track*
        /// method, not by a pipeline-wide processor. Verifies that telemetry produced
        /// by clientA carries clientA.Context.User.Id, and telemetry produced by clientB
        /// carries clientB.Context.User.Id — even though they share the same pipeline.
        /// </summary>
        [Fact]
        public void TrackTrace_FromDifferentClients_CarriesEachClientsContext()
        {
            var clientA = new TelemetryClient(this.configuration);
            var clientB = new TelemetryClient(this.configuration);
            clientA.Context.User.Id = "user-A";
            clientB.Context.User.Id = "user-B";

            clientA.TrackTrace("from-A");
            clientB.TrackTrace("from-B");
            clientA.Flush();

            var msgA = this.logItems.FirstOrDefault(l => l.FormattedMessage == "from-A" || l.Body == "from-A");
            var msgB = this.logItems.FirstOrDefault(l => l.FormattedMessage == "from-B" || l.Body == "from-B");

            Assert.NotNull(msgA);
            Assert.NotNull(msgB);

            var attrsA = msgA.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            var attrsB = msgB.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());

            Assert.Equal("user-A", attrsA["enduser.pseudo.id"]);
            Assert.Equal("user-B", attrsB["enduser.pseudo.id"]);
        }

        /// <summary>
        /// Item-level TelemetryContext (set on the telemetry object) must take precedence
        /// over client-level Context. Verifies skip-if-present merging.
        /// </summary>
        [Fact]
        public void ItemLevelContext_TakesPrecedenceOverClientContext()
        {
            var client = new TelemetryClient(this.configuration);
            client.Context.User.Id = "client-user";
            client.Context.Cloud.RoleName = "client-role"; // resource attribute, not in dictionary

            var trace = new TraceTelemetry("merged-trace");
            trace.Context.User.Id = "item-user"; // should win

            client.TrackTrace(trace);
            client.Flush();

            var record = this.logItems.FirstOrDefault(l => l.FormattedMessage == "merged-trace" || l.Body == "merged-trace");
            Assert.NotNull(record);

            var attrs = record.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("item-user", attrs["enduser.pseudo.id"]);
        }

        /// <summary>
        /// Two clients tracking dependencies (activity-based path) must each carry
        /// their own Operation.Name on the emitted Activity.
        /// </summary>
        [Fact]
        public void TrackDependency_FromDifferentClients_CarriesEachClientsContext()
        {
            var clientA = new TelemetryClient(this.configuration);
            var clientB = new TelemetryClient(this.configuration);
            clientA.Context.Operation.Name = "op-A";
            clientB.Context.Operation.Name = "op-B";

            clientA.TrackDependency(new DependencyTelemetry
            {
                Name = "dep-A",
                Target = "tgt",
                Type = "HTTP",
                Duration = TimeSpan.FromMilliseconds(1),
                Success = true,
            });

            clientB.TrackDependency(new DependencyTelemetry
            {
                Name = "dep-B",
                Target = "tgt",
                Type = "HTTP",
                Duration = TimeSpan.FromMilliseconds(1),
                Success = true,
            });

            clientA.Flush();

            var actA = this.activityItems.FirstOrDefault(a => a.DisplayName == "dep-A");
            var actB = this.activityItems.FirstOrDefault(a => a.DisplayName == "dep-B");
            Assert.NotNull(actA);
            Assert.NotNull(actB);

            Assert.Equal("op-A", actA.GetTagItem("microsoft.operation_name"));
            Assert.Equal("op-B", actB.GetTagItem("microsoft.operation_name"));
        }
    }
}
