namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Trace;
    using Xunit;

    /// <summary>
    /// Tests for TelemetryClient.Context → cached context tags behavior.
    /// Validates that public TelemetryContext properties are mapped to the correct
    /// OpenTelemetry semantic convention attributes and applied to all telemetry types.
    /// </summary>
    [Collection("TelemetryClientTests")]
    public class TelemetryClientContextTagsTest : IDisposable
    {
        private readonly List<LogRecord> logItems;
        private readonly List<Activity> activityItems;
        private readonly TelemetryClient telemetryClient;

        public TelemetryClientContextTagsTest()
        {
            var configuration = new TelemetryConfiguration();
            configuration.SamplingRatio = 1.0f;
            this.logItems = new List<LogRecord>();
            this.activityItems = new List<Activity>();
            var instrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + instrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b
                .WithLogging(l => l.AddInMemoryExporter(this.logItems))
                .WithTracing(t => t.AddInMemoryExporter(this.activityItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        public void Dispose()
        {
            this.activityItems?.Clear();
            this.logItems?.Clear();
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }  

        #region Log-based Telemetry — Context Tags Applied

        [Fact]
        public void TrackEvent_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "evt-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "evt-auth";
            this.telemetryClient.Context.User.AccountId = "evt-acct";
            this.telemetryClient.Context.Operation.Name = "EvtOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "evt-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.2";
            this.telemetryClient.Context.Session.Id = "evt-session";
            this.telemetryClient.Context.Device.Id = "evt-device";
            this.telemetryClient.Context.Device.Model = "Phone";
            this.telemetryClient.Context.Device.Type = "Mobile";
            this.telemetryClient.Context.Device.OperatingSystem = "Android 14";

            this.telemetryClient.TrackEvent("AllContextEvent");
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "AllContextEvent"));
            Assert.NotNull(logRecord);

            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("evt-user", attributes["enduser.pseudo.id"]);
            Assert.Equal("evt-auth", attributes["enduser.id"]);
            Assert.Equal("EvtOp", attributes["microsoft.operation_name"]);
            Assert.Equal("10.0.0.2", attributes["microsoft.client.ip"]);
            Assert.Equal("evt-session", attributes["microsoft.session.id"]);
            Assert.Equal("evt-device", attributes["ai.device.id"]);
            Assert.Equal("Phone", attributes["ai.device.model"]);
            Assert.Equal("Mobile", attributes["ai.device.type"]);
            Assert.Equal("Android 14", attributes["ai.device.osVersion"]);
            Assert.Equal("evt-bot", attributes["microsoft.synthetic_source"]);
            Assert.Equal("evt-acct", attributes["microsoft.user.account_id"]);
        }

        [Fact]
        public void TrackTrace_WithSeverityAndProperties_IncludesClientContextTags()
        {
            this.telemetryClient.Context.User.AuthenticatedUserId = "auth-trace";

            this.telemetryClient.TrackTrace("msg", SeverityLevel.Warning, new Dictionary<string, string> { { "key", "val" } });
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Warning);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("auth-trace", attributes["enduser.id"]);
            Assert.Equal("val", attributes["key"]);
        }

        [Fact]
        public void TrackTrace_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "trc-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "trc-auth";
            this.telemetryClient.Context.User.AccountId = "trc-acct";
            this.telemetryClient.Context.Operation.Name = "TrcOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "trc-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.3";
            this.telemetryClient.Context.Session.Id = "trc-session";
            this.telemetryClient.Context.Device.Id = "trc-device";
            this.telemetryClient.Context.Device.Model = "Desktop";
            this.telemetryClient.Context.Device.Type = "PC";
            this.telemetryClient.Context.Device.OperatingSystem = "Windows 10";

            this.telemetryClient.TrackTrace("AllContextTrace");
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("trc-user", attributes["enduser.pseudo.id"]);
            Assert.Equal("trc-auth", attributes["enduser.id"]);
            Assert.Equal("TrcOp", attributes["microsoft.operation_name"]);
            Assert.Equal("10.0.0.3", attributes["microsoft.client.ip"]);
            Assert.Equal("trc-session", attributes["microsoft.session.id"]);
            Assert.Equal("trc-device", attributes["ai.device.id"]);
            Assert.Equal("Desktop", attributes["ai.device.model"]);
            Assert.Equal("PC", attributes["ai.device.type"]);
            Assert.Equal("Windows 10", attributes["ai.device.osVersion"]);
            Assert.Equal("trc-bot", attributes["microsoft.synthetic_source"]);
            Assert.Equal("trc-acct", attributes["microsoft.user.account_id"]);
        }

        [Fact]
        public void TrackException_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "exc-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "exc-auth";
            this.telemetryClient.Context.User.AccountId = "exc-acct";
            this.telemetryClient.Context.Operation.Name = "ExcOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "exc-bot";
            this.telemetryClient.Context.Location.Ip = "172.16.0.1";
            this.telemetryClient.Context.Session.Id = "exc-session";
            this.telemetryClient.Context.Device.Id = "exc-device";
            this.telemetryClient.Context.Device.Model = "Tablet";
            this.telemetryClient.Context.Device.Type = "Portable";
            this.telemetryClient.Context.Device.OperatingSystem = "Linux 6.1";

            this.telemetryClient.TrackException(new InvalidOperationException("boom"));
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Error);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("exc-user", attributes["enduser.pseudo.id"]);
            Assert.Equal("exc-auth", attributes["enduser.id"]);
            Assert.Equal("ExcOp", attributes["microsoft.operation_name"]);
            Assert.Equal("172.16.0.1", attributes["microsoft.client.ip"]);
            Assert.Equal("exc-session", attributes["microsoft.session.id"]);
            Assert.Equal("exc-device", attributes["ai.device.id"]);
            Assert.Equal("Tablet", attributes["ai.device.model"]);
            Assert.Equal("Portable", attributes["ai.device.type"]);
            Assert.Equal("Linux 6.1", attributes["ai.device.osVersion"]);
            Assert.Equal("exc-bot", attributes["microsoft.synthetic_source"]);
            Assert.Equal("exc-acct", attributes["microsoft.user.account_id"]);
        }

        [Fact]
        public void TrackAvailability_IncludesClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "avail-user";

            var avail = new AvailabilityTelemetry("Test", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1), "WestUS", true);
            this.telemetryClient.TrackAvailability(avail);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.availability.name" && a.Value?.ToString() == "Test"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("avail-user", attributes["enduser.pseudo.id"]);
        }

        #endregion

        #region Log-based Telemetry — 3-Tier Priority Merge


        [Fact]
        public void TrackEvent_ItemPropertiesOverrideContextTags()
        {
            // Client context (lowest priority)
            this.telemetryClient.Context.User.Id = "context-user";
            this.telemetryClient.Context.Location.Ip = "1.1.1.1";

            // Item properties (highest priority)
            var props = new Dictionary<string, string>
            {
                { "enduser.pseudo.id", "item-user" },
            };

            this.telemetryClient.TrackEvent("TestEvent", props);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());

            // Item properties override context tags
            Assert.Equal("item-user", attributes["enduser.pseudo.id"]);
            // Non-overridden context tag still present
            Assert.Equal("1.1.1.1", attributes["microsoft.client.ip"]);
        }

        [Fact]
        public void TrackEvent_ItemPropertiesOverrideGlobalPropertiesOverrideContextTags()
        {
            // Tier 1: Client context (lowest)
            this.telemetryClient.Context.User.Id = "context-user";
            this.telemetryClient.Context.Location.Ip = "1.1.1.1";

            // Tier 2: GlobalProperties (medium)
            this.telemetryClient.Context.GlobalProperties["enduser.pseudo.id"] = "global-user";
            this.telemetryClient.Context.GlobalProperties["microsoft.client.ip"] = "2.2.2.2";
            this.telemetryClient.Context.GlobalProperties["extra"] = "from-global";

            // Tier 3: Item properties (highest)
            var props = new Dictionary<string, string>
            {
                { "enduser.pseudo.id", "item-user" },
                { "item-only", "from-item" },
            };

            this.telemetryClient.TrackEvent("TestEvent", props);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());

            // Item wins over global and context
            Assert.Equal("item-user", attributes["enduser.pseudo.id"]);
            // Global wins over context
            Assert.Equal("2.2.2.2", attributes["microsoft.client.ip"]);
            // Non-overridden from each tier
            Assert.Equal("from-global", attributes["extra"]);
            Assert.Equal("from-item", attributes["item-only"]);
        }

        [Fact]
        public void TrackTrace_Telemetry_ItemContextOverridesClientContext()
        {
            // Client-level context
            this.telemetryClient.Context.User.Id = "client-user";
            this.telemetryClient.Context.Location.Ip = "1.1.1.1";

            // Item-level context (via ApplyContextToProperties)
            var trace = new TraceTelemetry("test");
            trace.Context.User.Id = "item-user";

            this.telemetryClient.TrackTrace(trace);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());

            // Item-level context overrides client-level
            Assert.Equal("item-user", attributes["enduser.pseudo.id"]);
            // Client-level context still present for non-overridden keys
            Assert.Equal("1.1.1.1", attributes["microsoft.client.ip"]);
        }

        [Fact]
        public void TrackException_Telemetry_ItemContextOverridesClientContext()
        {
            this.telemetryClient.Context.User.Id = "client-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "client-auth";

            var exc = new ExceptionTelemetry(new Exception("test"));
            exc.Context.User.AuthenticatedUserId = "item-auth";

            this.telemetryClient.TrackException(exc);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Error);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());

            // Item override
            Assert.Equal("item-auth", attributes["enduser.id"]);
            // Client-level preserved
            Assert.Equal("client-user", attributes["enduser.pseudo.id"]);
        }

        [Fact]
        public void TrackEvent_AllTiersMergeWithNoKeyOverlap()
        {
            // Tier 1: context tags
            this.telemetryClient.Context.User.Id = "user-from-context";

            // Tier 2: GlobalProperties (different key)
            this.telemetryClient.Context.GlobalProperties["environment"] = "production";

            // Tier 3: Item properties (different key)
            var props = new Dictionary<string, string>
            {
                { "request-id", "req-123" },
            };

            this.telemetryClient.TrackEvent("TestEvent", props);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());

            // All three tiers present
            Assert.Equal("user-from-context", attributes["enduser.pseudo.id"]);
            Assert.Equal("production", attributes["environment"]);
            Assert.Equal("req-123", attributes["request-id"]);
        }

        #endregion

        #region Activity-based Telemetry — Context Tags Applied

        [Fact]
        public void TrackRequest_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "req-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "req-auth";
            this.telemetryClient.Context.User.AccountId = "req-acct";
            this.telemetryClient.Context.Location.Ip = "10.0.0.1";
            this.telemetryClient.Context.Session.Id = "req-session";
            this.telemetryClient.Context.Device.Id = "req-device";
            this.telemetryClient.Context.Device.Model = "Laptop";
            this.telemetryClient.Context.Device.Type = "PC";
            this.telemetryClient.Context.Device.OperatingSystem = "macOS 14";
            this.telemetryClient.Context.Operation.SyntheticSource = "test-runner";

            var request = new RequestTelemetry("GET /api", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), "200", true);
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "req-user"));
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.id" && t.Value == "req-auth"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.client.ip" && t.Value == "10.0.0.1"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.session.id" && t.Value == "req-session"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.id" && t.Value == "req-device"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.model" && t.Value == "Laptop"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.type" && t.Value == "PC"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.osVersion" && t.Value == "macOS 14"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.synthetic_source" && t.Value == "test-runner"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.user.account_id" && t.Value == "req-acct"));
        }

        [Fact]
        public void TrackDependency_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "dep-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "dep-auth";
            this.telemetryClient.Context.User.AccountId = "dep-acct";
            this.telemetryClient.Context.Operation.Name = "ParentOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "dep-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.4";
            this.telemetryClient.Context.Session.Id = "dep-session";
            this.telemetryClient.Context.Device.Id = "dep-device";
            this.telemetryClient.Context.Device.Model = "Watch";
            this.telemetryClient.Context.Device.Type = "Wearable";
            this.telemetryClient.Context.Device.OperatingSystem = "watchOS 10";

            var dep = new DependencyTelemetry
            {
                Type = "HTTP",
                Name = "GET /external",
                Duration = TimeSpan.FromMilliseconds(100),
                Success = true
            };
            this.telemetryClient.TrackDependency(dep);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "dep-user"));
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.id" && t.Value == "dep-auth"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.operation_name" && t.Value == "ParentOp"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.client.ip" && t.Value == "10.0.0.4"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.session.id" && t.Value == "dep-session"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.id" && t.Value == "dep-device"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.model" && t.Value == "Watch"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.type" && t.Value == "Wearable"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.osVersion" && t.Value == "watchOS 10"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.synthetic_source" && t.Value == "dep-bot"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.user.account_id" && t.Value == "dep-acct"));
        }

        #endregion

        #region Activity-based Telemetry — Priority Override

        [Fact]
        public void TrackRequest_ItemContextOverridesClientContext()
        {
            // Client-level
            this.telemetryClient.Context.User.Id = "client-user";
            this.telemetryClient.Context.Location.Ip = "1.1.1.1";

            // Item-level
            var request = new RequestTelemetry("GET /api", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(50), "200", true);
            request.Context.User.Id = "item-user";

            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];

            // Item-level overrides client-level (SetTag upserts)
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "item-user"));
            // Non-overridden client-level preserved
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.client.ip" && t.Value == "1.1.1.1"));
        }

        [Fact]
        public void TrackDependency_ItemContextOverridesClientContext()
        {
            this.telemetryClient.Context.User.Id = "client-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "client-auth";

            var dep = new DependencyTelemetry
            {
                Type = "SQL",
                Name = "Query",
                Duration = TimeSpan.FromMilliseconds(30),
                Success = true,
            };
            dep.Context.User.AuthenticatedUserId = "item-auth";

            this.telemetryClient.TrackDependency(dep);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];

            Assert.True(activity.Tags.Any(t => t.Key == "enduser.id" && t.Value == "item-auth"));
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "client-user"));
        }

        [Fact]
        public void TrackDependency_ItemOperationNameOverridesClientOperationName()
        {
            // Client-level operation name
            this.telemetryClient.Context.Operation.Name = "ClientOp";

            // Item-level operation name
            var dep = new DependencyTelemetry
            {
                Type = "HTTP",
                Name = "GET /external",
                Duration = TimeSpan.FromMilliseconds(50),
                Success = true,
            };
            dep.Context.Operation.Name = "ItemOp";

            this.telemetryClient.TrackDependency(dep);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];

            // Item-level Operation.Name should override client-level
            var opNameTag = activity.Tags.FirstOrDefault(t => t.Key == "microsoft.operation_name");
            Assert.Equal("ItemOp", opNameTag.Value);
        }

        #endregion

        #region StartOperation — Context Tags Applied

        [Fact]
        public void StartOperation_Request_AppliesClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "op-user";
            this.telemetryClient.Context.Location.Ip = "10.0.0.5";
            this.telemetryClient.Context.Session.Id = "op-session";

            using (var operation = this.telemetryClient.StartOperation<RequestTelemetry>("GET /api"))
            {
                // Activity is live — context processor runs on end
            }

            this.telemetryClient.Flush();

            Assert.True(this.activityItems.Count >= 1);
            var activity = this.activityItems.First(a => a.DisplayName == "GET /api");
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "op-user"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.client.ip" && t.Value == "10.0.0.5"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.session.id" && t.Value == "op-session"));
        }

        [Fact]
        public void StartOperation_Dependency_AppliesClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "dep-op-user";
            this.telemetryClient.Context.Device.Id = "dep-op-device";

            using (var operation = this.telemetryClient.StartOperation<DependencyTelemetry>("SQL Query"))
            {
            }

            this.telemetryClient.Flush();

            Assert.True(this.activityItems.Count >= 1);
            var activity = this.activityItems.First(a => a.DisplayName == "SQL Query");
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "dep-op-user"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.id" && t.Value == "dep-op-device"));
        }

        [Fact]
        public void StartOperation_GlobalPropertiesApplied()
        {
            this.telemetryClient.Context.User.Id = "op-user";
            this.telemetryClient.Context.GlobalProperties["env"] = "prod";
            this.telemetryClient.Context.GlobalProperties["region"] = "westus";

            using (var operation = this.telemetryClient.StartOperation<RequestTelemetry>("GET /health"))
            {
            }

            this.telemetryClient.Flush();

            Assert.True(this.activityItems.Count >= 1);
            var activity = this.activityItems.First(a => a.DisplayName == "GET /health");
            Assert.True(activity.Tags.Any(t => t.Key == "env" && t.Value == "prod"));
            Assert.True(activity.Tags.Any(t => t.Key == "region" && t.Value == "westus"));
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "op-user"));
        }

        #endregion

        #region GlobalProperties — Activity-based Telemetry

        [Fact]
        public void TrackRequest_GlobalPropertiesAppliedAsTags()
        {
            this.telemetryClient.Context.GlobalProperties["env"] = "staging";
            this.telemetryClient.Context.GlobalProperties["deployment"] = "blue";

            var request = new RequestTelemetry("GET /api", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(10), "200", true);
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];
            Assert.True(activity.Tags.Any(t => t.Key == "env" && t.Value == "staging"));
            Assert.True(activity.Tags.Any(t => t.Key == "deployment" && t.Value == "blue"));
        }

        [Fact]
        public void TrackDependency_GlobalPropertiesAppliedAsTags()
        {
            this.telemetryClient.Context.GlobalProperties["tenant"] = "contoso";

            var dep = new DependencyTelemetry { Type = "HTTP", Name = "GET /ext", Duration = TimeSpan.FromMilliseconds(50), Success = true };
            this.telemetryClient.TrackDependency(dep);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            Assert.True(this.activityItems[0].Tags.Any(t => t.Key == "tenant" && t.Value == "contoso"));
        }

        [Fact]
        public void TrackRequest_GlobalPropertiesDoNotOverrideItemProperties()
        {
            this.telemetryClient.Context.GlobalProperties["key1"] = "global-val";

            var request = new RequestTelemetry("GET /", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(10), "200", true);
            request.Properties["key1"] = "item-val";
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            // Item-level wins — SetTag from Track* runs before processor's SetTagIfAbsent
            Assert.True(this.activityItems[0].Tags.Any(t => t.Key == "key1" && t.Value == "item-val"));
        }

        #endregion

        #region GlobalProperties — Log-based Telemetry

        [Fact]
        public void TrackEvent_GlobalPropertiesAppliedAsAttributes()
        {
            this.telemetryClient.Context.GlobalProperties["env"] = "production";
            this.telemetryClient.Context.GlobalProperties["version"] = "1.2.3";

            this.telemetryClient.TrackEvent("GlobalPropEvent");
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "GlobalPropEvent"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("production", attributes["env"]);
            Assert.Equal("1.2.3", attributes["version"]);
        }

        [Fact]
        public void TrackTrace_GlobalPropertiesAppliedAsAttributes()
        {
            this.telemetryClient.Context.GlobalProperties["component"] = "worker";

            this.telemetryClient.TrackTrace("GlobalPropTrace");
            this.telemetryClient.Flush();

            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("worker", attributes["component"]);
        }

        [Fact]
        public void TrackEvent_GlobalPropertiesDoNotOverrideItemProperties()
        {
            this.telemetryClient.Context.GlobalProperties["key1"] = "global-val";

            var props = new Dictionary<string, string> { { "key1", "item-val" } };
            this.telemetryClient.TrackEvent("OverrideEvent", props);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "OverrideEvent"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            // Item-level wins
            Assert.Equal("item-val", attributes["key1"]);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void TrackEvent_NoContextSet_EmitsWithoutError()
        {
            // Context is default/empty — no context tags
            this.telemetryClient.TrackEvent("NoContext");
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "NoContext"));
            Assert.NotNull(logRecord);

            // Should NOT have any context tag attributes
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.False(attributes.ContainsKey("enduser.pseudo.id"));
            Assert.False(attributes.ContainsKey("enduser.id"));
            Assert.False(attributes.ContainsKey("microsoft.operation_name"));
            Assert.False(attributes.ContainsKey("microsoft.client.ip"));
            Assert.False(attributes.ContainsKey("microsoft.session.id"));
            Assert.False(attributes.ContainsKey("ai.device.id"));
            Assert.False(attributes.ContainsKey("ai.device.model"));
            Assert.False(attributes.ContainsKey("ai.device.type"));
            Assert.False(attributes.ContainsKey("microsoft.synthetic_source"));
            Assert.False(attributes.ContainsKey("microsoft.user.account_id"));
        }

        [Fact]
        public void TrackTrace_NullProperties_ContextTagsStillApplied()
        {
            this.telemetryClient.Context.User.Id = "null-props-user";

            this.telemetryClient.TrackTrace("msg", properties: null);
            this.telemetryClient.Flush();

            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("null-props-user", attributes["enduser.pseudo.id"]);
        }

        [Fact]
        public void TrackRequest_NoContextSet_EmitsWithoutContextTags()
        {
            var request = new RequestTelemetry("GET /", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(10), "200", true);
            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];

            Assert.False(activity.Tags.Any(t => t.Key == "enduser.pseudo.id"));
            Assert.False(activity.Tags.Any(t => t.Key == "enduser.id"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.client.ip"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.session.id"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.device.id"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.device.model"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.device.type"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.synthetic_source"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.user.account_id"));
        }

        #endregion
    }
}
