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
    using OpenTelemetry.Metrics;
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
        private readonly List<OpenTelemetry.Metrics.Metric> metricItems;
        private readonly List<Activity> activityItems;
        private readonly TelemetryClient telemetryClient;

        public TelemetryClientContextTagsTest()
        {
            var configuration = new TelemetryConfiguration();
            configuration.SamplingRatio = 1.0f;
            this.logItems = new List<LogRecord>();
            this.metricItems = new List<OpenTelemetry.Metrics.Metric>();
            this.activityItems = new List<Activity>();
            var instrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + instrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b
                .WithLogging(l => l.AddInMemoryExporter(this.logItems))
                .WithMetrics(m => m.AddInMemoryExporter(this.metricItems))
                .WithTracing(t => t.AddInMemoryExporter(this.activityItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        public void Dispose()
        {
            this.activityItems?.Clear();
            this.metricItems?.Clear();
            this.logItems?.Clear();
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }

        #region BuildContextTags — Attribute Key Mapping

        [Fact]
        public void BuildContextTags_UserIdMapsToEnduserPseudoId()
        {
            this.telemetryClient.Context.User.Id = "user-123";

            var tags = this.telemetryClient.ContextTags;

            Assert.True(tags.ContainsKey("enduser.pseudo.id"));
            Assert.Equal("user-123", tags["enduser.pseudo.id"]);
        }

        [Fact]
        public void BuildContextTags_AuthenticatedUserIdMapsToEnduserId()
        {
            this.telemetryClient.Context.User.AuthenticatedUserId = "auth-456";

            var tags = this.telemetryClient.ContextTags;

            Assert.True(tags.ContainsKey("enduser.id"));
            Assert.Equal("auth-456", tags["enduser.id"]);
        }

        [Fact]
        public void BuildContextTags_UserAgentMapsToUserAgentOriginal()
        {
            this.telemetryClient.Context.User.UserAgent = "Mozilla/5.0";

            var tags = this.telemetryClient.ContextTags;

            Assert.True(tags.ContainsKey("user_agent.original"));
            Assert.Equal("Mozilla/5.0", tags["user_agent.original"]);
        }

        [Fact]
        public void BuildContextTags_OperationNameMapsToMicrosoftOperationName()
        {
            this.telemetryClient.Context.Operation.Name = "GET /api/users";

            var tags = this.telemetryClient.ContextTags;

            Assert.True(tags.ContainsKey("microsoft.operation_name"));
            Assert.Equal("GET /api/users", tags["microsoft.operation_name"]);
        }

        [Fact]
        public void BuildContextTags_LocationIpMapsToMicrosoftClientIp()
        {
            this.telemetryClient.Context.Location.Ip = "10.0.0.1";

            var tags = this.telemetryClient.ContextTags;

            Assert.True(tags.ContainsKey("microsoft.client.ip"));
            Assert.Equal("10.0.0.1", tags["microsoft.client.ip"]);
        }

        [Fact]
        public void BuildContextTags_AllPropertiesSet_ContainsAllAttributes()
        {
            this.telemetryClient.Context.User.Id = "user-1";
            this.telemetryClient.Context.User.AuthenticatedUserId = "auth-1";
            this.telemetryClient.Context.User.UserAgent = "TestAgent/1.0";
            this.telemetryClient.Context.User.AccountId = "acct-1";
            this.telemetryClient.Context.Operation.Name = "TestOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "bot";
            this.telemetryClient.Context.Location.Ip = "192.168.1.1";
            this.telemetryClient.Context.Session.Id = "session-1";
            this.telemetryClient.Context.Session.IsFirst = true;
            this.telemetryClient.Context.Device.Id = "device-1";
            this.telemetryClient.Context.Device.Model = "Surface Pro";
            this.telemetryClient.Context.Device.OemName = "Microsoft";
            this.telemetryClient.Context.Device.Type = "Tablet";
            this.telemetryClient.Context.Device.OperatingSystem = "Windows 11";

            var tags = this.telemetryClient.ContextTags;

            Assert.Equal(14, tags.Count);
            Assert.Equal("user-1", tags["enduser.pseudo.id"]);
            Assert.Equal("auth-1", tags["enduser.id"]);
            Assert.Equal("TestAgent/1.0", tags["user_agent.original"]);
            Assert.Equal("TestOp", tags["microsoft.operation_name"]);
            Assert.Equal("192.168.1.1", tags["microsoft.client.ip"]);
            Assert.Equal("session-1", tags["microsoft.session.id"]);
            Assert.Equal("True", tags["ai.session.isFirst"]);
            Assert.Equal("device-1", tags["ai.device.id"]);
            Assert.Equal("Surface Pro", tags["ai.device.model"]);
            Assert.Equal("Microsoft", tags["ai.device.oemName"]);
            Assert.Equal("Tablet", tags["ai.device.type"]);
            Assert.Equal("Windows 11", tags["ai.device.osVersion"]);
            Assert.Equal("bot", tags["microsoft.synthetic_source"]);
            Assert.Equal("acct-1", tags["microsoft.user.account_id"]);
        }

        #endregion

        #region BuildContextTags — Null/Empty Exclusion

        [Fact]
        public void BuildContextTags_NullUserIdExcluded()
        {
            this.telemetryClient.Context.User.Id = null;
            this.telemetryClient.Context.Location.Ip = "10.0.0.1";

            var tags = this.telemetryClient.ContextTags;

            Assert.False(tags.ContainsKey("enduser.pseudo.id"));
            Assert.True(tags.ContainsKey("microsoft.client.ip"));
        }

        [Fact]
        public void BuildContextTags_EmptyStringExcluded()
        {
            this.telemetryClient.Context.User.Id = string.Empty;
            this.telemetryClient.Context.User.AuthenticatedUserId = "";
            this.telemetryClient.Context.User.UserAgent = "";
            this.telemetryClient.Context.User.AccountId = "";
            this.telemetryClient.Context.Operation.Name = "";
            this.telemetryClient.Context.Operation.SyntheticSource = "";
            this.telemetryClient.Context.Location.Ip = "";
            this.telemetryClient.Context.Session.Id = "";
            this.telemetryClient.Context.Device.Id = "";
            this.telemetryClient.Context.Device.Model = "";
            this.telemetryClient.Context.Device.OemName = "";
            this.telemetryClient.Context.Device.Type = "";
            this.telemetryClient.Context.Device.OperatingSystem = "";

            var tags = this.telemetryClient.ContextTags;

            Assert.Equal(0, tags.Count);
        }

        [Fact]
        public void BuildContextTags_MixedSetAndUnset_OnlyIncludesNonEmpty()
        {
            this.telemetryClient.Context.User.Id = "user-1";
            // AuthenticatedUserId not set (null)
            this.telemetryClient.Context.User.UserAgent = "";  // empty
            this.telemetryClient.Context.Operation.Name = "MyOp";
            // Location.Ip not set (null)

            var tags = this.telemetryClient.ContextTags;

            Assert.Equal(2, tags.Count);
            Assert.True(tags.ContainsKey("enduser.pseudo.id"));
            Assert.True(tags.ContainsKey("microsoft.operation_name"));
            Assert.False(tags.ContainsKey("enduser.id"));
            Assert.False(tags.ContainsKey("user_agent.original"));
            Assert.False(tags.ContainsKey("microsoft.client.ip"));
        }

        [Fact]
        public void BuildContextTags_NoContextSet_ReturnsEmptyDictionary()
        {
            // Context is default (all properties null)
            var tags = this.telemetryClient.ContextTags;

            Assert.NotNull(tags);
            Assert.Equal(0, tags.Count);
        }

        #endregion

       

        #region Log-based Telemetry — Context Tags Applied

        [Fact]
        public void TrackEvent_String_IncludesClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "ctx-user";
            this.telemetryClient.Context.Location.Ip = "10.0.0.1";

            this.telemetryClient.TrackEvent("TestEvent");
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name" && a.Value?.ToString() == "TestEvent"));
            Assert.NotNull(logRecord);

            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("ctx-user", attributes["enduser.pseudo.id"]);
            Assert.Equal("10.0.0.1", attributes["microsoft.client.ip"]);
        }

        [Fact]
        public void TrackEvent_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "evt-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "evt-auth";
            this.telemetryClient.Context.User.UserAgent = "EvtAgent/1.0";
            this.telemetryClient.Context.User.AccountId = "evt-acct";
            this.telemetryClient.Context.Operation.Name = "EvtOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "evt-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.2";
            this.telemetryClient.Context.Session.Id = "evt-session";
            this.telemetryClient.Context.Session.IsFirst = true;
            this.telemetryClient.Context.Device.Id = "evt-device";
            this.telemetryClient.Context.Device.Model = "Phone";
            this.telemetryClient.Context.Device.OemName = "Samsung";
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
            Assert.Equal("EvtAgent/1.0", attributes["user_agent.original"]);
            Assert.Equal("EvtOp", attributes["microsoft.operation_name"]);
            Assert.Equal("10.0.0.2", attributes["microsoft.client.ip"]);
            Assert.Equal("evt-session", attributes["microsoft.session.id"]);
            Assert.Equal("True", attributes["ai.session.isFirst"]);
            Assert.Equal("evt-device", attributes["ai.device.id"]);
            Assert.Equal("Phone", attributes["ai.device.model"]);
            Assert.Equal("Samsung", attributes["ai.device.oemName"]);
            Assert.Equal("Mobile", attributes["ai.device.type"]);
            Assert.Equal("Android 14", attributes["ai.device.osVersion"]);
            Assert.Equal("evt-bot", attributes["microsoft.synthetic_source"]);
            Assert.Equal("evt-acct", attributes["microsoft.user.account_id"]);
        }

        [Fact]
        public void TrackTrace_String_IncludesClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "trace-user";

            this.telemetryClient.TrackTrace("hello");
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("trace-user", attributes["enduser.pseudo.id"]);
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
            this.telemetryClient.Context.User.UserAgent = "TrcAgent/1.0";
            this.telemetryClient.Context.User.AccountId = "trc-acct";
            this.telemetryClient.Context.Operation.Name = "TrcOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "trc-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.3";
            this.telemetryClient.Context.Session.Id = "trc-session";
            this.telemetryClient.Context.Session.IsFirst = false;
            this.telemetryClient.Context.Device.Id = "trc-device";
            this.telemetryClient.Context.Device.Model = "Desktop";
            this.telemetryClient.Context.Device.OemName = "HP";
            this.telemetryClient.Context.Device.Type = "PC";
            this.telemetryClient.Context.Device.OperatingSystem = "Windows 10";

            this.telemetryClient.TrackTrace("AllContextTrace");
            this.telemetryClient.Flush();

            Assert.True(this.logItems.Count > 0);
            var logRecord = this.logItems.Last();
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("trc-user", attributes["enduser.pseudo.id"]);
            Assert.Equal("trc-auth", attributes["enduser.id"]);
            Assert.Equal("TrcAgent/1.0", attributes["user_agent.original"]);
            Assert.Equal("TrcOp", attributes["microsoft.operation_name"]);
            Assert.Equal("10.0.0.3", attributes["microsoft.client.ip"]);
            Assert.Equal("trc-session", attributes["microsoft.session.id"]);
            Assert.Equal("False", attributes["ai.session.isFirst"]);
            Assert.Equal("trc-device", attributes["ai.device.id"]);
            Assert.Equal("Desktop", attributes["ai.device.model"]);
            Assert.Equal("HP", attributes["ai.device.oemName"]);
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
            this.telemetryClient.Context.User.UserAgent = "ExcAgent/1.0";
            this.telemetryClient.Context.User.AccountId = "exc-acct";
            this.telemetryClient.Context.Operation.Name = "ExcOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "exc-bot";
            this.telemetryClient.Context.Location.Ip = "172.16.0.1";
            this.telemetryClient.Context.Session.Id = "exc-session";
            this.telemetryClient.Context.Session.IsFirst = true;
            this.telemetryClient.Context.Device.Id = "exc-device";
            this.telemetryClient.Context.Device.Model = "Tablet";
            this.telemetryClient.Context.Device.OemName = "Asus";
            this.telemetryClient.Context.Device.Type = "Portable";
            this.telemetryClient.Context.Device.OperatingSystem = "Linux 6.1";

            this.telemetryClient.TrackException(new InvalidOperationException("boom"));
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l => l.LogLevel == LogLevel.Error);
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());
            Assert.Equal("exc-user", attributes["enduser.pseudo.id"]);
            Assert.Equal("exc-auth", attributes["enduser.id"]);
            Assert.Equal("ExcAgent/1.0", attributes["user_agent.original"]);
            Assert.Equal("ExcOp", attributes["microsoft.operation_name"]);
            Assert.Equal("172.16.0.1", attributes["microsoft.client.ip"]);
            Assert.Equal("exc-session", attributes["microsoft.session.id"]);
            Assert.Equal("True", attributes["ai.session.isFirst"]);
            Assert.Equal("exc-device", attributes["ai.device.id"]);
            Assert.Equal("Tablet", attributes["ai.device.model"]);
            Assert.Equal("Asus", attributes["ai.device.oemName"]);
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
        public void TrackEvent_GlobalPropertiesOverrideContextTags()
        {
            // Client context (lowest priority)
            this.telemetryClient.Context.User.Id = "context-user";

            // GlobalProperties (medium priority) - override the same key
            this.telemetryClient.Context.GlobalProperties["enduser.pseudo.id"] = "global-user";

            this.telemetryClient.TrackEvent("TestEvent");
            this.telemetryClient.Flush();

            var logRecord = this.logItems.FirstOrDefault(l =>
                l.Attributes != null && l.Attributes.Any(a =>
                    a.Key == "microsoft.custom_event.name"));
            Assert.NotNull(logRecord);
            var attributes = logRecord.Attributes.ToDictionary(a => a.Key, a => a.Value?.ToString());

            // GlobalProperties should override context tag
            Assert.Equal("global-user", attributes["enduser.pseudo.id"]);
        }

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
            this.telemetryClient.Context.Session.IsFirst = true;
            this.telemetryClient.Context.Device.Id = "req-device";
            this.telemetryClient.Context.Device.Model = "Laptop";
            this.telemetryClient.Context.Device.OemName = "Dell";
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
            Assert.True(activity.Tags.Any(t => t.Key == "ai.session.isFirst" && t.Value == "True"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.id" && t.Value == "req-device"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.model" && t.Value == "Laptop"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.oemName" && t.Value == "Dell"));
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
            this.telemetryClient.Context.User.UserAgent = "DepAgent/1.0";
            this.telemetryClient.Context.User.AccountId = "dep-acct";
            this.telemetryClient.Context.Operation.Name = "ParentOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "dep-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.4";
            this.telemetryClient.Context.Session.Id = "dep-session";
            this.telemetryClient.Context.Session.IsFirst = true;
            this.telemetryClient.Context.Device.Id = "dep-device";
            this.telemetryClient.Context.Device.Model = "Watch";
            this.telemetryClient.Context.Device.OemName = "Apple";
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
            Assert.True(activity.Tags.Any(t => t.Key == "user_agent.original" && t.Value == "DepAgent/1.0"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.operation_name" && t.Value == "ParentOp"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.client.ip" && t.Value == "10.0.0.4"));
            Assert.True(activity.Tags.Any(t => t.Key == "microsoft.session.id" && t.Value == "dep-session"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.session.isFirst" && t.Value == "True"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.id" && t.Value == "dep-device"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.model" && t.Value == "Watch"));
            Assert.True(activity.Tags.Any(t => t.Key == "ai.device.oemName" && t.Value == "Apple"));
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
        public void TrackRequest_ItemPropertiesOverrideGlobalPropertiesOverrideContextTags()
        {
            // Tier 1: Context tags (lowest)
            this.telemetryClient.Context.User.Id = "ctx-user";

            // Tier 2: GlobalProperties (medium)
            this.telemetryClient.Context.GlobalProperties["enduser.pseudo.id"] = "global-user";
            this.telemetryClient.Context.GlobalProperties["env"] = "staging";

            // Tier 3: Item properties (highest)
            var request = new RequestTelemetry("GET /", DateTimeOffset.UtcNow, TimeSpan.FromMilliseconds(10), "200", true);
            request.Properties["enduser.pseudo.id"] = "item-user";
            request.Properties["req-id"] = "r-1";

            this.telemetryClient.TrackRequest(request);
            this.telemetryClient.Flush();

            Assert.Equal(1, this.activityItems.Count);
            var activity = this.activityItems[0];

            // Item > Global > Context
            Assert.True(activity.Tags.Any(t => t.Key == "enduser.pseudo.id" && t.Value == "item-user"));
            Assert.True(activity.Tags.Any(t => t.Key == "env" && t.Value == "staging"));
            Assert.True(activity.Tags.Any(t => t.Key == "req-id" && t.Value == "r-1"));
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

        #region Metric Telemetry — Context Tags Applied

        [Fact]
        public void TrackMetric_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "metric-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "metric-auth";
            this.telemetryClient.Context.User.UserAgent = "MetricAgent/1.0";
            this.telemetryClient.Context.User.AccountId = "metric-acct";
            this.telemetryClient.Context.Operation.Name = "MetricOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "metric-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.5";
            this.telemetryClient.Context.Session.Id = "metric-session";
            this.telemetryClient.Context.Session.IsFirst = true;
            this.telemetryClient.Context.Device.Id = "metric-device";
            this.telemetryClient.Context.Device.Model = "Server";
            this.telemetryClient.Context.Device.OemName = "Lenovo";
            this.telemetryClient.Context.Device.Type = "Rack";

            this.telemetryClient.TrackMetric("AllContextMetric", 42.0);
            this.telemetryClient.Flush();

            Assert.True(this.metricItems.Count > 0);
            var metric = this.metricItems.FirstOrDefault(m => m.Name == "AllContextMetric");
            Assert.NotNull(metric);

            foreach (var point in metric.GetMetricPoints())
            {
                var tagDict = new Dictionary<string, string>();
                foreach (var tag in point.Tags)
                {
                    tagDict[tag.Key] = tag.Value?.ToString();
                }

                Assert.Equal("metric-user", tagDict["enduser.pseudo.id"]);
                Assert.Equal("metric-auth", tagDict["enduser.id"]);
                Assert.Equal("MetricAgent/1.0", tagDict["user_agent.original"]);
                Assert.Equal("MetricOp", tagDict["microsoft.operation_name"]);
                Assert.Equal("10.0.0.5", tagDict["microsoft.client.ip"]);
                Assert.Equal("metric-session", tagDict["microsoft.session.id"]);
                Assert.Equal("True", tagDict["ai.session.isFirst"]);
                Assert.Equal("metric-device", tagDict["ai.device.id"]);
                Assert.Equal("Server", tagDict["ai.device.model"]);
                Assert.Equal("Lenovo", tagDict["ai.device.oemName"]);
                Assert.Equal("Rack", tagDict["ai.device.type"]);
                Assert.Equal("metric-bot", tagDict["microsoft.synthetic_source"]);
                Assert.Equal("metric-acct", tagDict["microsoft.user.account_id"]);
                break;
            }
        }

        [Fact]
        public void TrackMetric_PropertiesOverrideContextTags()
        {
            this.telemetryClient.Context.User.Id = "ctx-user";

            this.telemetryClient.TrackMetric("TestMetric", 10.0, new Dictionary<string, string>
            {
                { "enduser.pseudo.id", "prop-user" },
                { "custom", "value" },
            });
            this.telemetryClient.Flush();

            var metric = this.metricItems.FirstOrDefault(m => m.Name == "TestMetric");
            Assert.NotNull(metric);

            foreach (var point in metric.GetMetricPoints())
            {
                string userId = null;
                string custom = null;
                foreach (var tag in point.Tags)
                {
                    if (tag.Key == "enduser.pseudo.id")
                        userId = tag.Value?.ToString();
                    if (tag.Key == "custom")
                        custom = tag.Value?.ToString();
                }
                // TagList appends in order; OTel should use the last value for duplicate keys
                // Context tags are added first, then properties, so properties win
                Assert.NotNull(userId);
                Assert.Equal("prop-user", userId);
                Assert.NotNull(custom);
                Assert.Equal("value", custom);
                break;
            }
        }

        [Fact]
        public void GetMetric_TrackValue_IncludesAllClientContextTags()
        {
            this.telemetryClient.Context.User.Id = "getmetric-user";
            this.telemetryClient.Context.User.AuthenticatedUserId = "getmetric-auth";
            this.telemetryClient.Context.User.UserAgent = "GetMetricAgent/1.0";
            this.telemetryClient.Context.User.AccountId = "getmetric-acct";
            this.telemetryClient.Context.Operation.Name = "GetMetricOp";
            this.telemetryClient.Context.Operation.SyntheticSource = "getmetric-bot";
            this.telemetryClient.Context.Location.Ip = "10.0.0.6";
            this.telemetryClient.Context.Session.Id = "getmetric-session";
            this.telemetryClient.Context.Session.IsFirst = true;
            this.telemetryClient.Context.Device.Id = "getmetric-device";
            this.telemetryClient.Context.Device.Model = "VM";
            this.telemetryClient.Context.Device.OemName = "Azure";
            this.telemetryClient.Context.Device.Type = "Cloud";

            var metric = this.telemetryClient.GetMetric("AllContextGetMetric");
            metric.TrackValue(5.0);

            this.telemetryClient.Flush();

            var collected = this.metricItems.FirstOrDefault(m => m.Name == "AllContextGetMetric");
            Assert.NotNull(collected);

            foreach (var point in collected.GetMetricPoints())
            {
                var tagDict = new Dictionary<string, string>();
                foreach (var tag in point.Tags)
                {
                    tagDict[tag.Key] = tag.Value?.ToString();
                }

                Assert.Equal("getmetric-user", tagDict["enduser.pseudo.id"]);
                Assert.Equal("getmetric-auth", tagDict["enduser.id"]);
                Assert.Equal("GetMetricAgent/1.0", tagDict["user_agent.original"]);
                Assert.Equal("GetMetricOp", tagDict["microsoft.operation_name"]);
                Assert.Equal("10.0.0.6", tagDict["microsoft.client.ip"]);
                Assert.Equal("getmetric-session", tagDict["microsoft.session.id"]);
                Assert.Equal("True", tagDict["ai.session.isFirst"]);
                Assert.Equal("getmetric-device", tagDict["ai.device.id"]);
                Assert.Equal("VM", tagDict["ai.device.model"]);
                Assert.Equal("Azure", tagDict["ai.device.oemName"]);
                Assert.Equal("Cloud", tagDict["ai.device.type"]);
                Assert.Equal("getmetric-bot", tagDict["microsoft.synthetic_source"]);
                Assert.Equal("getmetric-acct", tagDict["microsoft.user.account_id"]);
                break;
            }
        }

        [Fact]
        public void GetMetric_TrackValue_OneDimension_IncludesContextTagsAndDimension()
        {
            this.telemetryClient.Context.User.Id = "dim-user";

            var metric = this.telemetryClient.GetMetric("OneDimMetric", "region");
            metric.TrackValue(10.0, "us-west");

            this.telemetryClient.Flush();

            var collected = this.metricItems.FirstOrDefault(m => m.Name == "OneDimMetric");
            Assert.NotNull(collected);

            foreach (var point in collected.GetMetricPoints())
            {
                bool hasUserId = false;
                bool hasRegion = false;
                foreach (var tag in point.Tags)
                {
                    if (tag.Key == "enduser.pseudo.id" && tag.Value?.ToString() == "dim-user")
                        hasUserId = true;
                    if (tag.Key == "region" && tag.Value?.ToString() == "us-west")
                        hasRegion = true;
                }
                Assert.True(hasUserId, "Context tag should appear on metric");
                Assert.True(hasRegion, "Dimension should appear on metric");
                break;
            }
        }

        [Fact]
        public void GetMetric_TrackValue_TwoDimensions_IncludesContextTagsAndDimensions()
        {
            this.telemetryClient.Context.Location.Ip = "192.168.1.1";

            var metric = this.telemetryClient.GetMetric("TwoDimMetric", "region", "status");
            metric.TrackValue(7.0, "eu-west", "success");

            this.telemetryClient.Flush();

            var collected = this.metricItems.FirstOrDefault(m => m.Name == "TwoDimMetric");
            Assert.NotNull(collected);

            foreach (var point in collected.GetMetricPoints())
            {
                bool hasIp = false;
                bool hasRegion = false;
                bool hasStatus = false;
                foreach (var tag in point.Tags)
                {
                    if (tag.Key == "microsoft.client.ip" && tag.Value?.ToString() == "192.168.1.1")
                        hasIp = true;
                    if (tag.Key == "region" && tag.Value?.ToString() == "eu-west")
                        hasRegion = true;
                    if (tag.Key == "status" && tag.Value?.ToString() == "success")
                        hasStatus = true;
                }
                Assert.True(hasIp, "Context tag should appear on metric");
                Assert.True(hasRegion, "Dimension 1 should appear on metric");
                Assert.True(hasStatus, "Dimension 2 should appear on metric");
                break;
            }
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
            Assert.False(attributes.ContainsKey("user_agent.original"));
            Assert.False(attributes.ContainsKey("microsoft.operation_name"));
            Assert.False(attributes.ContainsKey("microsoft.client.ip"));
            Assert.False(attributes.ContainsKey("microsoft.session.id"));
            Assert.False(attributes.ContainsKey("ai.session.isFirst"));
            Assert.False(attributes.ContainsKey("ai.device.id"));
            Assert.False(attributes.ContainsKey("ai.device.model"));
            Assert.False(attributes.ContainsKey("ai.device.oemName"));
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
        public void TrackMetric_NullProperties_ContextTagsStillApplied()
        {
            this.telemetryClient.Context.User.Id = "null-metric-user";

            this.telemetryClient.TrackMetric("NullPropMetric", 1.0, null);
            this.telemetryClient.Flush();

            var metric = this.metricItems.FirstOrDefault(m => m.Name == "NullPropMetric");
            Assert.NotNull(metric);

            foreach (var point in metric.GetMetricPoints())
            {
                bool hasUserId = false;
                foreach (var tag in point.Tags)
                {
                    if (tag.Key == "enduser.pseudo.id" && tag.Value?.ToString() == "null-metric-user")
                        hasUserId = true;
                }
                Assert.True(hasUserId, "Context tag should appear even with null properties");
                break;
            }
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
            Assert.False(activity.Tags.Any(t => t.Key == "user_agent.original"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.client.ip"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.session.id"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.session.isFirst"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.device.id"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.device.model"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.device.oemName"));
            Assert.False(activity.Tags.Any(t => t.Key == "ai.device.type"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.synthetic_source"));
            Assert.False(activity.Tags.Any(t => t.Key == "microsoft.user.account_id"));
        }

        [Fact]
        public void GetMetric_NoContextSet_TrackValueSucceeds()
        {
            // No context set
            var metric = this.telemetryClient.GetMetric("EmptyCtxMetric");
            metric.TrackValue(3.0);
            this.telemetryClient.Flush();

            var collected = this.metricItems.FirstOrDefault(m => m.Name == "EmptyCtxMetric");
            Assert.NotNull(collected);

            foreach (var point in collected.GetMetricPoints())
            {
                Assert.True(point.GetHistogramCount() > 0);
                break;
            }
        }

        #endregion
    }
}
