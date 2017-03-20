namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
#if !NET40
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using System.Net;
#if !NET40
    using System.Net.Http;
#endif
    using System.Reflection;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.TestFramework;
#if NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if NET40 || NET45 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class TelemetryClientTest
    {
        [TestMethod]
        public void IsEnabledReturnsTrueIfTelemetryTrackingIsEnabledInConfiguration()
        {
            var configuration = new TelemetryConfiguration { DisableTelemetry = false };
            var client = new TelemetryClient(configuration);

            Assert.True(client.IsEnabled());
        }
        
        #region TrackEvent

        [TestMethod]
        public void TrackEventSendsEventTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingEventTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackEvent("TestEvent");

            var eventTelemetry = (EventTelemetry)sentTelemetry.Single();
            Assert.Equal("TestEvent", eventTelemetry.Name);
        }

        [TestMethod]
        public void TrackEventSendsEventTelemetryWithSpecifiedObjectTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackEvent(new EventTelemetry("TestEvent"));

            var eventTelemetry = (EventTelemetry)sentTelemetry.Single();
            Assert.Equal("TestEvent", eventTelemetry.Name);
        }

        [TestMethod]
        public void TrackEventWillSendPropertiesIfProvidedInline()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackEvent("Test", new Dictionary<string, string> { { "blah", "yoyo" } });

            var eventTelemetry = (EventTelemetry)sentTelemetry.Single();
            Assert.Equal("yoyo", eventTelemetry.Properties["blah"]);
        }

        #endregion

        #region Initialize

        [TestMethod]
        public void InitializeSetsDateTime()
        {
            EventTelemetry telemetry = new EventTelemetry("TestEvent");

            new TelemetryClient().Initialize(telemetry);

            Assert.True(telemetry.Timestamp != default(DateTimeOffset));
        }

        [TestMethod]
        public void InitializeSetsRoleInstance()
        {
            PlatformSingleton.Current = new StubPlatform { OnGetMachineName = () => "TestMachine" };

            EventTelemetry telemetry = new EventTelemetry("TestEvent");
            new TelemetryClient().Initialize(telemetry);

            Assert.Equal("TestMachine", telemetry.Context.Cloud.RoleInstance);
            Assert.Null(telemetry.Context.Internal.NodeName);

            PlatformSingleton.Current = null;
        }

        [TestMethod]
        public void InitializeDoesNotOverrideRoleInstance()
        {
            PlatformSingleton.Current = new StubPlatform { OnGetMachineName = () => "TestMachine" };

            EventTelemetry telemetry = new EventTelemetry("TestEvent");
            telemetry.Context.Cloud.RoleInstance = "MyMachineImplementation";

            new TelemetryClient().Initialize(telemetry);

            Assert.Equal("MyMachineImplementation", telemetry.Context.Cloud.RoleInstance);
            Assert.Equal("TestMachine", telemetry.Context.Internal.NodeName);

            PlatformSingleton.Current = null;
        }

        [TestMethod]
        public void InitializeDoesNotOverrideNodeName()
        {
            PlatformSingleton.Current = new StubPlatform { OnGetMachineName = () => "TestMachine" };

            EventTelemetry telemetry = new EventTelemetry("TestEvent");
            telemetry.Context.Internal.NodeName = "MyMachineImplementation";

            new TelemetryClient().Initialize(telemetry);

            Assert.Equal("TestMachine", telemetry.Context.Cloud.RoleInstance);
            Assert.Equal("MyMachineImplementation", telemetry.Context.Internal.NodeName);

            PlatformSingleton.Current = null;
        }

        #endregion

        #region TrackMetric

        [TestMethod]
        public void TrackMetricSendsSpecifiedAggregatedMetricTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackMetric(
                new MetricTelemetry()
                {
                    Name = "Test Metric",
                    Count = 5,
                    Sum = 40,
                    Min = 3.0,
                    Max = 4.0,
                    StandardDeviation = 1.0
                });

            var metric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("Test Metric", metric.Name);
            Assert.Equal(5, metric.Count);
            Assert.Equal(40, metric.Sum);
            Assert.Equal(3.0, metric.Min);
            Assert.Equal(4.0, metric.Max);
            Assert.Equal(1.0, metric.StandardDeviation);
        }

        [TestMethod]
        public void TrackMetricSendsMetricTelemetryWithSpecifiedNameAndValue()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

#pragma warning disable CS0618
            client.TrackMetric("TestMetric", 42);
#pragma warning restore CS0618

            var metric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("TestMetric", metric.Name);

#pragma warning disable CS0618
            Assert.Equal(42, metric.Value);
#pragma warning restore CS0618
        }

        [TestMethod]
        public void TrackMetricSendsSpecifiedMetricTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

#pragma warning disable CS0618
            client.TrackMetric(new MetricTelemetry("TestMetric", 42));
#pragma warning restore CS0618

            var metric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("TestMetric", metric.Name);

#pragma warning disable CS0618
            Assert.Equal(42, metric.Value);
#pragma warning restore CS0618
        }

        [TestMethod]
        public void TrackMetricSendsMetricTelemetryWithGivenNameValueAndProperties()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

#pragma warning disable CS0618
            client.TrackMetric("TestMetric", 4.2, new Dictionary<string, string> { { "blah", "yoyo" } });
#pragma warning restore CS0618

            var metric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("TestMetric", metric.Name);

#pragma warning disable CS0618
            Assert.Equal(4.2, metric.Value);
#pragma warning restore CS0618

            Assert.Equal("yoyo", metric.Properties["blah"]);
        }

        [TestMethod]
        public void TrackMetricIgnoresNullPropertiesArgumentToAvoidCrashingUserApp()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

#pragma warning disable CS0618
            client.TrackMetric("TestMetric", 4.2, null);
#pragma warning restore CS0618

            var metric = (MetricTelemetry)sentTelemetry.Single();

            Assert.Equal("TestMetric", metric.Name);
#pragma warning disable CS0618
            Assert.Equal(4.2, metric.Value);
#pragma warning restore CS0618
            Assert.Empty(metric.Properties);
        }

        #endregion

        #region TrackTrace

        [TestMethod]
        public void TrackTraceSendsTraceTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingTraceTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackTrace("TestTrace");

            var trace = (TraceTelemetry)sentTelemetry.Single();
            Assert.Equal("TestTrace", trace.Message);
        }

        [TestMethod]
        public void TrackTraceSendsTraceTelemetryWithSpecifiedObjectTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackTrace(new TraceTelemetry { Message = "TestTrace" });

            var trace = (TraceTelemetry)sentTelemetry.Single();
            Assert.Equal("TestTrace", trace.Message);
        }

        [TestMethod]
        public void TrackTraceWillSendSeverityLevelIfProvidedInline()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackTrace("Test", SeverityLevel.Error);

            var trace = (TraceTelemetry)sentTelemetry.Single();
            Assert.Equal(SeverityLevel.Error, trace.SeverityLevel);
        }

        [TestMethod]
        public void TrackTraceWillNotSetSeverityLevelIfCustomerProvidedOnlyName()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackTrace("Test");

            var trace = (TraceTelemetry)sentTelemetry.Single();
            Assert.Equal(null, trace.SeverityLevel);
        }

        #endregion

        #region TrackException

        [TestMethod]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingExceptionTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            Exception ex = new Exception();
            client.TrackException(ex);

            var exceptionTelemetry = (ExceptionTelemetry)sentTelemetry.Single();
            Assert.Same(ex, exceptionTelemetry.Exception);
        }

        [TestMethod]
        public void TrackExceptionWillUseRequiredFieldAsTextForTheExceptionNameWhenTheExceptionNameIsEmptyToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackException((Exception)null);

            var exceptionTelemetry = (ExceptionTelemetry)sentTelemetry.Single();
            Assert.NotNull(exceptionTelemetry.Exception);
            Assert.Equal("n/a", exceptionTelemetry.Exception.Message);
        }

        [TestMethod]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedObjectTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            Exception ex = new Exception();
            client.TrackException(new ExceptionTelemetry(ex));

            var exceptionTelemetry = (ExceptionTelemetry)sentTelemetry.Single();
            Assert.Same(ex, exceptionTelemetry.Exception);
        }

        [TestMethod]
        public void TrackExceptionWillUseABlankObjectAsTheExceptionToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackException((ExceptionTelemetry)null);

            var exceptionTelemetry = (ExceptionTelemetry)sentTelemetry.Single();
            Assert.NotNull(exceptionTelemetry.Exception);
        }

        [TestMethod]
        public void TrackExceptionWillNotSetSeverityLevelIfOnlyExceptionProvided()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackException(new Exception());

            var exceptionTelemetry = (ExceptionTelemetry)sentTelemetry.Single();
            Assert.Equal(null, exceptionTelemetry.SeverityLevel);
        }

        #endregion

        #region TrackPageView

        [TestMethod]
        public void TrackPageViewSendsPageViewTelemetryWithGivenNameToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackPageView("TestName");

            var pageView = (PageViewTelemetry)sentTelemetry.Single();
            Assert.Equal("TestName", pageView.Name);
        }

        [TestMethod]
        public void TrackPageViewSendsGivenPageViewTelemetryToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var pageViewTelemetry = new PageViewTelemetry("TestName");
            client.TrackPageView(pageViewTelemetry);

            var channelPageView = (PageViewTelemetry)sentTelemetry.Single();
            Assert.Same(pageViewTelemetry, channelPageView);
        }

        #endregion

        #region TrackRequest

        [TestMethod]
        public void TrackRequestSendsRequestTelemetryWithGivenNameTimestampDurationAndSuccessToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var timestamp = DateTimeOffset.Now;
            client.TrackRequest("name", timestamp, TimeSpan.FromSeconds(42), "500", false);

            var request = (RequestTelemetry)sentTelemetry.Single();

            Assert.Equal("name", request.Name);
            Assert.Equal(timestamp, request.Timestamp);
            Assert.Equal("500", request.ResponseCode);
            Assert.Equal(TimeSpan.FromSeconds(42), request.Duration);
            Assert.Equal(false, request.Success);
        }

        [TestMethod]
        public void TrackRequestSendsGivenRequestTelemetryToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var clientRequest = new RequestTelemetry();
            client.TrackRequest(clientRequest);

            var channelRequest = (RequestTelemetry)sentTelemetry.Single();
            Assert.Same(clientRequest, channelRequest);
        }

        #endregion

        #region TrackDependency

        [TestMethod]
        public void TrackDependencySendsDependencyTelemetryWithGivenNameCommandnameTimestampDurationAndSuccessToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var timestamp = DateTimeOffset.Now;
            client.TrackDependency("name", "command name", timestamp, TimeSpan.FromSeconds(42), false);

            var dependency = (DependencyTelemetry)sentTelemetry.Single();

            Assert.Equal("name", dependency.Name);
            Assert.Equal("command name", dependency.Data);
            Assert.Equal(timestamp, dependency.Timestamp);
            Assert.Equal(TimeSpan.FromSeconds(42), dependency.Duration);
            Assert.Equal(false, dependency.Success);
        }

        [TestMethod]
        public void TrackDependencySendsGivenDependencyTelemetryToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var clientDependency = new DependencyTelemetry();
            client.TrackDependency(clientDependency);

            var channelDependency = (DependencyTelemetry)sentTelemetry.Single();
            Assert.Same(clientDependency, channelDependency);
        }

        #endregion

        #region TrackAvailability

        [TestMethod]
        public void TrackAvailabilitySendsAvailabilityTelemetryWithGivenNameRunlocationRunIdDurationResultAndMessageToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var timestamp = DateTimeOffset.Now;
            client.TrackAvailability("test name", timestamp, TimeSpan.FromSeconds(42), "test location", true);

            var availability = (AvailabilityTelemetry)sentTelemetry.Single();

            Assert.Equal("test name", availability.Name);
            Assert.Equal("test location", availability.RunLocation);
            Assert.Equal(timestamp, availability.Timestamp);
            Assert.Equal(TimeSpan.FromSeconds(42), availability.Duration);
            Assert.Equal(true, availability.Success);
        }

        [TestMethod]
        public void TrackAvailabilityTracksCustomDimensions()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var timestamp = DateTimeOffset.Now;
            var customDimensions = new Dictionary<string,string>()
                {
                    ["Blah"] = "yoyo"
                };
            
            client.TrackAvailability("test name", timestamp, TimeSpan.FromSeconds(42), "test location", true, properties: customDimensions);

            var availability = (AvailabilityTelemetry)sentTelemetry.Single();

            Assert.Equal("yoyo", availability.Properties["Blah"]);
            Assert.Equal(0, availability.Metrics.Count);
        }

        [TestMethod]
        public void TrackAvailabilityTracksCustomMetrics()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var timestamp = DateTimeOffset.Now;

            var customMetrics = new Dictionary<string, double>()
            {
                ["QueueLength"] = 10
            };

            client.TrackAvailability("test name", timestamp, TimeSpan.FromSeconds(42), "test location", true, metrics: customMetrics);

            var availability = (AvailabilityTelemetry)sentTelemetry.Single();

            Assert.Equal(0, availability.Properties.Count);
            Assert.Equal(10, availability.Metrics["QueueLength"]);
        }

        [TestMethod]
        public void TrackAvailabilitySendsGivenAvailabilityTelemetryToTelemetryChannel()
        {
            var sentTelemetry = new List<ITelemetry>();
            TelemetryClient client = this.InitializeTelemetryClient(sentTelemetry);

            var clientAvailability = new AvailabilityTelemetry();
            client.TrackAvailability(clientAvailability);

            var channelAvailability = (AvailabilityTelemetry)sentTelemetry.Single();
            Assert.Same(clientAvailability, channelAvailability);
        }

        #endregion

        #region Track

        [TestMethod]
        public void TrackMethodIsPublicToAllowDefiningTelemetryTypesOutsideOfCore()
        {
            Assert.True(typeof(TelemetryClient).GetTypeInfo().GetDeclaredMethod("Track").IsPublic);
        }

        [TestMethod]
        public void TrackMethodIsInvisibleThroughIntelliSenseSoThatCustomersDontGetConfused()
        {
            var attribute = typeof(TelemetryClient).GetTypeInfo().GetDeclaredMethod("Track").GetCustomAttributes(false).OfType<EditorBrowsableAttribute>().Single();
            Assert.Equal(EditorBrowsableState.Never, attribute.State);
        }

        [TestMethod]
        public void TrackMethodDoesNotThrowWhenInstrumentationKeyIsEmptyAndNotSendingTheTelemetryItem()
        {
            var channel = new StubTelemetryChannel { ThrowError = true };
            TelemetryConfiguration.Active = new TelemetryConfiguration(string.Empty, channel);
            Assert.DoesNotThrow(() => new TelemetryClient().Track(new StubTelemetry()));
        }

        [TestMethod]
        public void DefaultChannelInConfigurationIsCreatedByConstructorWhenNotSpecified()
        {
            TelemetryConfiguration configuration = new TelemetryConfiguration(Guid.NewGuid().ToString());
            Assert.NotNull(configuration.TelemetryChannel);
        }

        [TestMethod]
        public void TrackUsesInstrumentationKeyFromClientContextIfSetInCodeFirst()
        {
            ClearActiveTelemetryConfiguration();
            PlatformSingleton.Current = new StubPlatform();
            string message = "Test Message";

            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var configuration = new TelemetryConfiguration(string.Empty, channel);
            var client = new TelemetryClient(configuration);

            string environmentKey = Guid.NewGuid().ToString();
            string expectedKey = Guid.NewGuid().ToString();
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", environmentKey); // Set via the environment variable.

            client.Context.InstrumentationKey = expectedKey;
            Assert.DoesNotThrow(() => client.TrackTrace(message));
            Assert.Equal(expectedKey, sentTelemetry.Context.InstrumentationKey);

            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);

            PlatformSingleton.Current = null;
        }

        [TestMethod]
        public void TrackUsesInstrumentationKeyFromConfigIfEnvironmentVariableIsEmpty()
        {
            PlatformSingleton.Current = new StubPlatform();

            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var configuration = new TelemetryConfiguration(string.Empty, channel);
            var client = new TelemetryClient(configuration);

            string expectedKey = Guid.NewGuid().ToString();
            configuration.InstrumentationKey = expectedKey; // Set in config
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null); // Not set via env. variable
            Assert.DoesNotThrow(() => client.TrackTrace("Test Message"));

            Assert.Equal(expectedKey, sentTelemetry.Context.InstrumentationKey);

            PlatformSingleton.Current = null;
        }

        [TestMethod]
        public void TrackDoesNotInitializeInstrumentationKeyFromConfigWhenItWasSetExplicitly()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString(), new StubTelemetryChannel());
            var client = new TelemetryClient(configuration);

            var expectedKey = Guid.NewGuid().ToString();
            client.Context.InstrumentationKey = expectedKey;
            client.TrackTrace("Test Message");

            Assert.Equal(expectedKey, client.Context.InstrumentationKey);
        }

        [TestMethod]
        public void TrackDoesNotSendDataWhenTelemetryIsDisabled()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration(string.Empty, channel) { DisableTelemetry = true };

            var client = new TelemetryClient(configuration) {};

            client.Track(new StubTelemetry());

            Assert.Equal(0, sentTelemetry.Count);
        }

        [TestMethod]
        public void TrackRespectsInstrumentaitonKeyOfTelemetryItem()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };

            // No instrumentation key set here.
            var configuration = new TelemetryConfiguration(string.Empty, channel);

            var initializedTelemetry = new List<ITelemetry>();
            var telemetryInitializer = new StubTelemetryInitializer();
            telemetryInitializer.OnInitialize = item => initializedTelemetry.Add(item);
            configuration.TelemetryInitializers.Add(telemetryInitializer);

            var client = new TelemetryClient(configuration);

            var telemetry = new StubTelemetry();
            telemetry.Context.InstrumentationKey = "Foo";
            client.Track( telemetry );

            Assert.Equal(1, sentTelemetry.Count);
            Assert.Equal(1, initializedTelemetry.Count);
        }

        [TestMethod]
        public void TrackRespectsInstrumentaitonKeySetByTelemetryInitializer()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };

            // No instrumentation key set here.
            var configuration = new TelemetryConfiguration(string.Empty, channel);

            var initializedTelemetry = new List<ITelemetry>();
            var telemetryInitializer = new StubTelemetryInitializer();
            telemetryInitializer.OnInitialize = item =>
            {
                item.Context.InstrumentationKey = "Foo";
                initializedTelemetry.Add(item);
            };

            configuration.TelemetryInitializers.Add(telemetryInitializer);

            var client = new TelemetryClient(configuration);

            var telemetry = new StubTelemetry();
            client.Track(telemetry);

            Assert.Equal(1, sentTelemetry.Count);
            Assert.Equal(1, initializedTelemetry.Count);
        }

        [TestMethod]
        public void TrackDoesNotThrowExceptionsDuringTelemetryIntializersInitialize()
        {
            var configuration = new TelemetryConfiguration("Test key", new StubTelemetryChannel());
            var telemetryInitializer = new StubTelemetryInitializer();
            telemetryInitializer.OnInitialize = item => { throw new Exception(); };
            configuration.TelemetryInitializers.Add(telemetryInitializer);
            var client = new TelemetryClient(configuration);
            Assert.DoesNotThrow(() => client.Track(new StubTelemetry()));
        }

        [TestMethod]
        public void TrackLogsDiagnosticsMessageOnExceptionsDuringTelemetryIntializersInitialize()
        {
            using (var listener = new TestEventListener())
            {
                listener.EnableEvents(CoreEventSource.Log, EventLevel.Error);

                var configuration = new TelemetryConfiguration("Test key", new StubTelemetryChannel());
                var telemetryInitializer = new StubTelemetryInitializer();
                var exceptionMessage = "Test exception message";
                telemetryInitializer.OnInitialize = item => { throw new Exception(exceptionMessage); };
                configuration.TelemetryInitializers.Add(telemetryInitializer);

                var client = new TelemetryClient(configuration);
                client.Track(new StubTelemetry());

                var exceptionExplanation = "Exception while initializing " + typeof(StubTelemetryInitializer).FullName;
                var diagnosticsMessage = (string)listener.Messages.First().Payload[0];
                Assert.Contains(exceptionExplanation, diagnosticsMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(exceptionMessage, diagnosticsMessage, StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void TrackDoesNotAddDeveloperModeCustomPropertyIfDeveloperModeIsSetToFalse()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel
            {
                OnSend = telemetry => sentTelemetry = telemetry,
                DeveloperMode = false
            };
            var configuration = new TelemetryConfiguration("Test key", channel);
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());

            Assert.False(((ISupportProperties)sentTelemetry).Properties.ContainsKey("DeveloperMode"));
        }

        [TestMethod]
        public void TrackAddsDeveloperModeCustomPropertyWhenDeveloperModeIsTrue()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel
            {
                OnSend = telemetry => sentTelemetry = telemetry,
                DeveloperMode = true
            };
            var configuration = new TelemetryConfiguration("Test key", channel);
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());

            Assert.Equal("true", ((ISupportProperties)sentTelemetry).Properties["DeveloperMode"]);
        }

        [TestMethod]
        public void TrackDoesNotTryAddingDeveloperModeCustomPropertyWhenTelemetryDoesNotSupportCustomProperties()
        {
            var channel = new StubTelemetryChannel { DeveloperMode = true };
            var configuration = new TelemetryConfiguration("Test Key", channel);
            var client = new TelemetryClient(configuration);

#pragma warning disable 618
            Assert.DoesNotThrow(() => client.Track(new SessionStateTelemetry()));
#pragma warning disable 618
        }

        [TestMethod]
        public void TrackAddsTimestampWhenMissing()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel
            {
                OnSend = telemetry => sentTelemetry = telemetry
            };
            var configuration = new TelemetryConfiguration("Test key", channel);
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());

            Assert.NotEqual(DateTimeOffset.MinValue, sentTelemetry.Timestamp);
        }

        [TestMethod]
        public void TrackWritesTelemetryToDebugOutputIfIKeyEmpty()
        {
            ClearActiveTelemetryConfiguration();
            string actualMessage = null;
            var debugOutput = new StubDebugOutput
            {
                OnWriteLine = message =>
                {
                    System.Diagnostics.Debug.WriteLine("1");
                    actualMessage = message;
                },
                OnIsAttached = () => true,
            };

            PlatformSingleton.Current = new StubPlatform { OnGetDebugOutput = () => debugOutput };
            var channel = new StubTelemetryChannel { DeveloperMode = true };
            var configuration = new TelemetryConfiguration(string.Empty, channel);
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());
            
            Assert.True(actualMessage.StartsWith("Application Insights Telemetry (unconfigured): "));
            PlatformSingleton.Current = null;
        }

        [TestMethod]
        public void TrackWritesTelemetryToDebugOutputIfIKeyNotEmpty()
        {
            string actualMessage = null;
            var debugOutput = new StubDebugOutput
            {
                OnWriteLine = message => actualMessage = message,
                OnIsAttached = () => true,
            };

            PlatformSingleton.Current = new StubPlatform { OnGetDebugOutput = () => debugOutput };
            var channel = new StubTelemetryChannel { DeveloperMode = true };
            var configuration = new TelemetryConfiguration("123", channel);
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());
            
            Assert.True(actualMessage.StartsWith("Application Insights Telemetry: "));
            PlatformSingleton.Current = null;
        }


        [TestMethod]
        public void TrackDoesNotWriteTelemetryToDebugOutputIfNotInDeveloperMode()
        {
            ClearActiveTelemetryConfiguration();
            string actualMessage = null;
            var debugOutput = new StubDebugOutput { OnWriteLine = message => actualMessage = message };
            PlatformSingleton.Current = new StubPlatform { OnGetDebugOutput = () => debugOutput };
            var channel = new StubTelemetryChannel();
            var configuration = new TelemetryConfiguration("Test key", channel);
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());
            PlatformSingleton.Current = null;
            Assert.Null(actualMessage);
        }

        [TestMethod]
        public void TrackCopiesPropertiesFromClientToTelemetry()
        {
            var configuration = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
            var client = new TelemetryClient(configuration);
            client.Context.Properties["TestProperty"] = "TestValue";
            client.Context.InstrumentationKey = "Test Key";

            var telemetry = new StubTelemetry();
            client.Track(telemetry);

            Assert.Equal(client.Context.Properties.ToArray(), telemetry.Properties.ToArray());
        }

        [TestMethod]
        public void TrackDoesNotOverwriteTelemetryPropertiesWithClientPropertiesBecauseExplicitlySetValuesTakePrecedence()
        {
            var configuration = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel());
            var client = new TelemetryClient(configuration);
            client.Context.Properties["TestProperty"] = "ClientValue";
            client.Context.InstrumentationKey = "Test Key";

            var telemetry = new StubTelemetry { Properties = { { "TestProperty", "TelemetryValue" } } };
            client.Track(telemetry);

            Assert.Equal("TelemetryValue", telemetry.Properties["TestProperty"]);
        }

        [TestMethod]
        public void TrackCopiesPropertiesFromClientToTelemetryBeforeInvokingInitializersBecauseExplicitlySetValuesTakePrecedence()
        {
            const string PropertyName = "TestProperty";

            string valueInInitializer = null;
            var initializer = new StubTelemetryInitializer();
            initializer.OnInitialize = telemetry => valueInInitializer = ((ISupportProperties)telemetry).Properties[PropertyName];

            var configuration = new TelemetryConfiguration(string.Empty, new StubTelemetryChannel()) { TelemetryInitializers = { initializer } };

            var client = new TelemetryClient(configuration);
            client.Context.Properties[PropertyName] = "ClientValue";
            client.Context.InstrumentationKey = "Test Key";

            client.Track(new StubTelemetry());

            Assert.Equal(client.Context.Properties[PropertyName], valueInInitializer);
        }

        [TestMethod]
        public void TrackAddsSdkVerionByDefault()
        {
            // split version by 4 numbers manually so we do not do the same as in the product code and actually test it
            string versonStr = Assembly.GetAssembly(typeof(TelemetryConfiguration)).GetCustomAttributes(false)
                    .OfType<AssemblyFileVersionAttribute>()
                    .First()
                    .Version;
            string[] versionParts = new Version(versonStr).ToString().Split('.');

            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString(), new StubTelemetryChannel());
            var client = new TelemetryClient(configuration);

            client.Context.InstrumentationKey = "Test";
            EventTelemetry eventTelemetry = new EventTelemetry("test");
            client.Track(eventTelemetry);

            Assert.Equal("dotnet:"+ string.Join(".", versionParts[0], versionParts[1], versionParts[2]) + "-" + versionParts[3], eventTelemetry.Context.Internal.SdkVersion);
        }

        [TestMethod]
        public void TrackDoesNotOverrideSdkVersion()
        {
            var configuration = new TelemetryConfiguration(Guid.NewGuid().ToString(), new StubTelemetryChannel());
            var client = new TelemetryClient(configuration);

            client.Context.InstrumentationKey = "Test";
            EventTelemetry eventTelemetry = new EventTelemetry("test");
            eventTelemetry.Context.Internal.SdkVersion = "test";
            client.Track(eventTelemetry);

            Assert.Equal("test", eventTelemetry.Context.Internal.SdkVersion);
        }

        #endregion

        #region Sampling

        [TestMethod]
        public void AllTelemetryIsSentWithDefaultSamplingRate()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration("Test key", channel);
            var client = new TelemetryClient(configuration);

            const int ItemsToGenerate = 100;

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                client.TrackRequest(new RequestTelemetry());
            }

            Assert.Equal(ItemsToGenerate, sentTelemetry.Count);
        }

        #endregion

        #region ValidateEndpoint

        [TestMethod]
        public void SendEventToValidateEndpoint()
        {
            string unicodeString = "русский\\#/\x0000\x0001\x0002\x0003\x0004\x0005\x0006\x0007\x0008\x009Farabicشلاؤيثبلاهتنمةىخحضقسفعشلاؤيصثبل c\n\r\t";

            EventTelemetry telemetry1 = new EventTelemetry(unicodeString);
            MetricTelemetry telemetry2 = new MetricTelemetry("name", 100);
            DependencyTelemetry telemetry3 = new DependencyTelemetry("name", "commandName", DateTimeOffset.UtcNow, TimeSpan.FromHours(3), true);
            ExceptionTelemetry telemetry4 = new ExceptionTelemetry(new ArgumentException("Test"));
            MetricTelemetry telemetry5 = new MetricTelemetry("name", 100);
            PageViewTelemetry telemetry6 = new PageViewTelemetry("name");
#pragma warning disable 618
            PerformanceCounterTelemetry telemetry7 = new PerformanceCounterTelemetry("category", "name", "instance", 100);
#pragma warning restore 618
            RequestTelemetry telemetry8 = new RequestTelemetry("name", DateTimeOffset.UtcNow, TimeSpan.FromHours(2), "200", true);
#pragma warning disable 618
            SessionStateTelemetry telemetry9 = new SessionStateTelemetry(SessionState.Start);
#pragma warning restore 618
            TraceTelemetry telemetry10 = new TraceTelemetry("text");
            AvailabilityTelemetry telemetry11 = new AvailabilityTelemetry("name", DateTimeOffset.UtcNow, TimeSpan.FromHours(10), "location", true, "message");

            var telemetryItems = new List<ITelemetry>
            {
                telemetry1,
                telemetry2,
                telemetry3,
                telemetry4,
                telemetry5,
                telemetry6,
                telemetry7,
                telemetry8,
                telemetry9,
                telemetry10,
                telemetry11
            };

            // ChuckNorrisTeamUnitTests resource in Prototypes5
            var config = new TelemetryConfiguration("fafa4b10-03d3-4bb0-98f4-364f0bdf5df8");
            var telemetryClient = new TelemetryClient(config);
            telemetryClient.Context.Properties.Add(unicodeString, unicodeString);
            
            telemetryClient.Initialize(telemetry1);
            telemetryClient.Initialize(telemetry2);
            telemetryClient.Initialize(telemetry3);
            telemetryClient.Initialize(telemetry4);
            telemetryClient.Initialize(telemetry5);
            telemetryClient.Initialize(telemetry6);
            telemetryClient.Initialize(telemetry7);
            telemetryClient.Initialize(telemetry8);
            telemetryClient.Initialize(telemetry9);
            telemetryClient.Initialize(telemetry10);
            telemetryClient.Initialize(telemetry11);

            string json = JsonSerializer.SerializeAsString(telemetryItems);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

#if NET40
            WebRequest request = WebRequest.Create("https://dc.services.visualstudio.com/v2/validate");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = jsonBytes.Length;
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            WebResponse response = request.GetResponse();
            var result = (HttpWebResponse)response;
            if (result.StatusCode != HttpStatusCode.OK)
            {
                var responseStream = response.GetResponseStream();
                using (var reader = new System.IO.StreamReader(responseStream))
                {
                    Trace.WriteLine(reader.ReadToEnd());
                }
            }
#else
            HttpClient client = new HttpClient();
            var result = client.PostAsync(
                "https://dc.services.visualstudio.com/v2/validate",
                new ByteArrayContent(jsonBytes)).GetAwaiter().GetResult();
            if (result.StatusCode != HttpStatusCode.OK)
            {
                var response = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Trace.WriteLine(response);
            }
#endif

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        [TestMethod]
        public void SerailizeRemovesEmptyPropertiesAndProducesValidJson()
        {
            var telemetryIn = new ExceptionTelemetry(new ApplicationException());
            telemetryIn.Properties.Add("MyKey", null);

            string json = JsonSerializer.SerializeAsString(telemetryIn);
            ExceptionTelemetry telemetryOut = Newtonsoft.Json.JsonConvert.DeserializeObject<ExceptionTelemetry>(json);
            Assert.Equal(0, telemetryOut.Properties.Count);
        }

        #endregion

        private TelemetryClient InitializeTelemetryClient(ICollection<ITelemetry> sentTelemetry)
        {
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var telemetryConfiguration = new TelemetryConfiguration(Guid.NewGuid().ToString(), channel);
            var client = new TelemetryClient(telemetryConfiguration);
            return client;
        }

        /// <summary>
        /// Resets the TelemetryConfiguration.Active default instance to null so that the iKey auto population paths can be followed for testing.
        /// </summary>
        private void ClearActiveTelemetryConfiguration()
        {
            TelemetryConfiguration.Active = null;
        }
    }
}
