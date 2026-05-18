namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Internal;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Trace;
    using Xunit;

    [Collection("TelemetryClientTests")]
    public class TelemetryClientPerClientScopingTests : IDisposable
    {
        private readonly TelemetryConfiguration configuration;
        private readonly List<Activity> activityItems;
        private readonly List<LogRecord> logItems;
        private readonly List<OpenTelemetry.Metrics.Metric> metricItems;
        private readonly TelemetryClient clientA;
        private readonly TelemetryClient clientB;

        public TelemetryClientPerClientScopingTests()
        {
            this.configuration = new TelemetryConfiguration();
            this.configuration.SamplingRatio = 1.0f;
            this.configuration.ConnectionString = "InstrumentationKey=" + Guid.NewGuid();
            this.activityItems = new List<Activity>();
            this.logItems = new List<LogRecord>();
            this.metricItems = new List<OpenTelemetry.Metrics.Metric>();
            this.configuration.ConfigureOpenTelemetryBuilder(builder => builder
                .WithTracing(tracing => tracing.AddInMemoryExporter(this.activityItems))
                .WithLogging(logging => logging.AddInMemoryExporter(this.logItems))
                .WithMetrics(metrics => metrics.AddInMemoryExporter(this.metricItems)));
            this.clientA = new TelemetryClient(this.configuration);
            this.clientB = new TelemetryClient(this.configuration);
        }

        public void Dispose()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }

            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CLOUD_ROLE_NAME", null);
            Environment.SetEnvironmentVariable("APPLICATIONINSIGHTS_CLOUD_ROLE_INSTANCE", null);
            this.configuration.Dispose();
        }

        [Fact]
        public void TwoClients_DistinctContext_NoCrossContamination_TrackDependency()
        {
            this.clientA.Context.Cloud.RoleName = "A";
            this.clientB.Context.Cloud.RoleName = "B";

            this.clientA.TrackDependency(new DependencyTelemetry { Type = "HTTP", Name = "dep-a", Duration = TimeSpan.FromMilliseconds(1), Success = true });
            this.clientB.TrackDependency(new DependencyTelemetry { Type = "HTTP", Name = "dep-b", Duration = TimeSpan.FromMilliseconds(1), Success = true });
            this.clientA.Flush();

            Assert.Equal("A", this.GetActivityTagValue("dep-a", SemanticConventions.AttributeServiceName));
            Assert.Equal("B", this.GetActivityTagValue("dep-b", SemanticConventions.AttributeServiceName));
        }

        [Fact]
        public void TwoClients_DistinctContext_NoCrossContamination_TrackRequest()
        {
            this.clientA.Context.Cloud.RoleName = "A";
            this.clientB.Context.Cloud.RoleName = "B";

            this.clientA.TrackRequest(new RequestTelemetry("req-a", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(1), "200", true));
            this.clientB.TrackRequest(new RequestTelemetry("req-b", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(1), "200", true));
            this.clientA.Flush();

            Assert.Equal("A", this.GetActivityTagValue("req-a", SemanticConventions.AttributeServiceName));
            Assert.Equal("B", this.GetActivityTagValue("req-b", SemanticConventions.AttributeServiceName));
        }

        [Fact]
        public void TwoClients_DistinctContext_NoCrossContamination_TrackEvent()
        {
            this.clientA.Context.Cloud.RoleName = "A";
            this.clientB.Context.Cloud.RoleName = "B";

            this.clientA.TrackEvent("event-a");
            this.clientB.TrackEvent("event-b");
            this.clientA.Flush();

            Assert.Equal("A", this.GetLogAttributeValue("event-a", "microsoft.custom_event.name", SemanticConventions.AttributeServiceName));
            Assert.Equal("B", this.GetLogAttributeValue("event-b", "microsoft.custom_event.name", SemanticConventions.AttributeServiceName));
        }

        [Fact]
        public void TwoClients_DistinctContext_NoCrossContamination_TrackTrace()
        {
            this.clientA.Context.Cloud.RoleName = "A";
            this.clientB.Context.Cloud.RoleName = "B";

            this.clientA.TrackTrace("trace-a", new Dictionary<string, string> { ["test.name"] = "trace-a" });
            this.clientB.TrackTrace("trace-b", new Dictionary<string, string> { ["test.name"] = "trace-b" });
            this.clientA.Flush();

            Assert.Equal("A", this.GetLogAttributeValue("trace-a", "test.name", SemanticConventions.AttributeServiceName));
            Assert.Equal("B", this.GetLogAttributeValue("trace-b", "test.name", SemanticConventions.AttributeServiceName));
        }

        [Fact]
        public void TwoClients_DistinctContext_NoCrossContamination_TrackException()
        {
            this.clientA.Context.Cloud.RoleName = "A";
            this.clientB.Context.Cloud.RoleName = "B";

            this.clientA.TrackException(new ExceptionTelemetry(new InvalidOperationException("exception-a")));
            this.clientB.TrackException(new ExceptionTelemetry(new InvalidOperationException("exception-b")));
            this.clientA.Flush();

            Assert.Equal("A", this.logItems.Single(item => item.FormattedMessage == "exception-a").Attributes.First(attribute => attribute.Key == SemanticConventions.AttributeServiceName).Value?.ToString());
            Assert.Equal("B", this.logItems.Single(item => item.FormattedMessage == "exception-b").Attributes.First(attribute => attribute.Key == SemanticConventions.AttributeServiceName).Value?.ToString());
        }

        [Fact]
        public void TwoClients_DistinctContext_NoCrossContamination_TrackAvailability()
        {
            this.clientA.Context.Cloud.RoleName = "A";
            this.clientB.Context.Cloud.RoleName = "B";

            this.clientA.TrackAvailability(new AvailabilityTelemetry("availability-a", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(1), "westus", true));
            this.clientB.TrackAvailability(new AvailabilityTelemetry("availability-b", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(1), "westus", true));
            this.clientA.Flush();

            Assert.Equal("A", this.GetLogAttributeValue("availability-a", "microsoft.availability.name", SemanticConventions.AttributeServiceName));
            Assert.Equal("B", this.GetLogAttributeValue("availability-b", "microsoft.availability.name", SemanticConventions.AttributeServiceName));
        }

        [Fact]
        public void TwoClients_DistinctContext_NoCrossContamination_TrackMetric()
        {
            this.clientA.Context.Cloud.RoleName = "A";
            this.clientB.Context.Cloud.RoleName = "B";

            this.clientA.TrackMetric(new MetricTelemetry("shared-metric", 1));
            this.clientB.TrackMetric(new MetricTelemetry("shared-metric", 2));
            this.clientA.Flush();

            var metric = this.metricItems.Single(m => m.Name == "shared-metric");
            var serviceNames = new List<string>();
            foreach (var point in metric.GetMetricPoints())
            {
                if (point.GetHistogramCount() <= 0)
                {
                    continue;
                }

                foreach (var tag in point.Tags)
                {
                    if (tag.Key == SemanticConventions.AttributeServiceName && tag.Value != null)
                    {
                        serviceNames.Add(tag.Value.ToString());
                    }
                }
            }

            Assert.Contains("A", serviceNames);
            Assert.Contains("B", serviceNames);
        }

        [Fact]
        public void Precedence_ItemContext_OverridesClientContext()
        {
            this.clientA.Context.Cloud.RoleName = "client";
            var telemetry = new EventTelemetry("item-overrides-client");
            telemetry.Context.Cloud.RoleName = "item";

            this.clientA.TrackEvent(telemetry);
            this.clientA.Flush();

            Assert.Equal("item", this.GetLogAttributeValue("item-overrides-client", "microsoft.custom_event.name", SemanticConventions.AttributeServiceName));
        }

        [Fact]
        public void Precedence_ClientContext_OverridesDefaultContext()
        {
            this.configuration.DefaultContext.User.Id = "default";
            this.clientA.Context.User.Id = "client";

            this.clientA.TrackEvent("client-overrides-default");
            this.clientA.Flush();

            Assert.Equal("client", this.GetLogAttributeValue("client-overrides-default", "microsoft.custom_event.name", SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void StartOperation_NewActivity_PicksUpClientContext()
        {
            this.clientA.Context.Cloud.RoleName = "A";

            using (this.clientA.StartOperation<RequestTelemetry>("op-a"))
            {
            }

            this.clientA.Flush();
            Assert.Equal("A", this.GetActivityTagValue("op-a", SemanticConventions.AttributeServiceName));
        }

        [Fact]
        public void StartOperation_ExistingActivity_PicksUpClientContext()
        {
            this.clientA.Context.Cloud.RoleName = "A";

            using var source = new ActivitySource(TelemetryConfiguration.ApplicationInsightsActivitySourceName);
            using var activity = source.StartActivity("existing-op");
            Assert.NotNull(activity);

            using (this.clientA.StartOperation<RequestTelemetry>(activity))
            {
            }

            this.clientA.Flush();
            var capturedActivity = Assert.Single(this.activityItems);
            Assert.Equal("A", capturedActivity.GetTagItem(SemanticConventions.AttributeServiceName)?.ToString());
        }

        [Fact]
        public void BareActivitySource_NoClient_PicksUpDefaultContextOnly()
        {
            this.configuration.DefaultContext.Operation.Name = "default-op";

            using var source = new ActivitySource(TelemetryConfiguration.ApplicationInsightsActivitySourceName);
            using (var activity = source.StartActivity("bare-activity"))
            {
                Assert.NotNull(activity);
                activity.Stop();
            }

            Assert.Equal("default-op", this.GetActivityTagValue("bare-activity", SemanticConventions.AttributeMicrosoftOperationName));
        }

        [Fact]
        public void GlobalProperties_PerClient_NoCrossContamination()
        {
            this.clientA.Context.GlobalProperties["k"] = "A";
            this.clientB.Context.GlobalProperties["k"] = "B";

            this.clientA.TrackEvent("globals-a");
            this.clientB.TrackEvent("globals-b");
            this.clientA.Flush();

            Assert.Equal("A", this.GetLogAttributeValue("globals-a", "microsoft.custom_event.name", "k"));
            Assert.Equal("B", this.GetLogAttributeValue("globals-b", "microsoft.custom_event.name", "k"));
        }

        private string GetActivityTagValue(string activityName, string tagName)
        {
            var activity = this.activityItems.Single(item => item.DisplayName == activityName);
            return activity.GetTagItem(tagName)?.ToString();
        }

        private string GetLogAttributeValue(string identityValue, string identityKey, string attributeKey)
        {
            var logRecord = this.logItems.Single(item => item.Attributes != null && item.Attributes.Any(attribute => attribute.Key == identityKey && attribute.Value?.ToString() == identityValue));
            return logRecord.Attributes.FirstOrDefault(attribute => attribute.Key == attributeKey).Value?.ToString();
        }
    }
}
