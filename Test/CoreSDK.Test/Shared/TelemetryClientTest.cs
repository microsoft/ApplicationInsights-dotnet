namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
#if CORE_PCL || NET45 || NET46
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using System.Net;
    using System.Net.Http;
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

        [TestMethod]
        public void InitializeSetsDateTime()
        {
            EventTelemetry telemetry = new EventTelemetry("TestEvent");

            new TelemetryClient().Initialize(telemetry);

            Assert.True(telemetry.Timestamp != default(DateTimeOffset));
        }

        #endregion

        #region TrackMetric

        [TestMethod]
        public void TrackMetricSendsMetricTelemetryWithSpecifiedNameAndValue()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackMetric("TestMetric", 42);

            var metric = (MetricTelemetry)sentTelemetry.Single();
            Assert.Equal("TestMetric", metric.Name);
            Assert.Equal(42, metric.Value);
        }

        [TestMethod]
        public void TrackMetricSendsSpecifiedMetricTelemetry()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackMetric(new MetricTelemetry("TestMetric", 42));

            var metric = (MetricTelemetry)sentTelemetry.Single();
            Assert.Equal("TestMetric", metric.Name);
            Assert.Equal(42, metric.Value);
        }

        [TestMethod]
        public void TrackMetricSendsMetricTelemetryWithGivenNameValueAndProperties()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackMetric("TestMetric", 4.2, new Dictionary<string, string> { { "blah", "yoyo" } });

            var metric = (MetricTelemetry)sentTelemetry.Single();
            Assert.Equal("TestMetric", metric.Name);
            Assert.Equal(4.2, metric.Value);
            Assert.Equal("yoyo", metric.Properties["blah"]);
        }

        [TestMethod]
        public void TrackMetricIgnoresNullPropertiesArgumentToAvoidCrashingUserApp()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackMetric("TestMetric", 4.2, null);

            var metric = (MetricTelemetry)sentTelemetry.Single();
            Assert.Equal("TestMetric", metric.Name);
            Assert.Equal(4.2, metric.Value);
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
            Assert.Equal(TimeSpan.FromSeconds(42), availability.Duration);
            Assert.Equal(true, availability.Success);
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
            TelemetryConfiguration.Active = new TelemetryConfiguration
            {
                InstrumentationKey = string.Empty,
                TelemetryChannel = channel
            };

            Assert.DoesNotThrow(() => new TelemetryClient().Track(new StubTelemetry()));
        }

        [TestMethod]
        public void TrackUsesInstrumentationKeyIfSetInCodeFirst()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var configuration = new TelemetryConfiguration { TelemetryChannel = channel };
            var client = new TelemetryClient(configuration);

            string expectedKey = Guid.NewGuid().ToString();
            client.Context.InstrumentationKey = expectedKey; // Set in code
            configuration.InstrumentationKey = Guid.NewGuid().ToString(); // Set in config
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", expectedKey); // Set via env. variable
            Assert.DoesNotThrow(() => client.TrackTrace("Test Message"));

            Assert.Equal(expectedKey, sentTelemetry.Context.InstrumentationKey);

            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
        }

        [TestMethod]
        public void TrackUsesInstrumentationKeyFromEnvironmentIfEmptyInCode()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var configuration = new TelemetryConfiguration { TelemetryChannel = channel };
            var client = new TelemetryClient(configuration);

            string expectedKey = Guid.NewGuid().ToString();
            configuration.InstrumentationKey = Guid.NewGuid().ToString(); // Set in config
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", expectedKey); // Set via env. variable
            Assert.DoesNotThrow(() => client.TrackTrace("Test Message"));

            Assert.Equal(expectedKey, sentTelemetry.Context.InstrumentationKey);

            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
        }

        [TestMethod]
        public void TrackUsesInstrumentationKeyFromConfigIfEnvironmentVariableIsEmpty()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var configuration = new TelemetryConfiguration { TelemetryChannel = channel };
            var client = new TelemetryClient(configuration);

            string expectedKey = Guid.NewGuid().ToString();
            configuration.InstrumentationKey = expectedKey; // Set in config
            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null); // Not set via env. variable
            Assert.DoesNotThrow(() => client.TrackTrace("Test Message"));

            Assert.Equal(expectedKey, sentTelemetry.Context.InstrumentationKey);
        }

        [TestMethod]
        public void TrackDoesNotInitializeInstrumentationKeyWhenItWasSetExplicitly()
        {
            var configuration = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel(), InstrumentationKey = Guid.NewGuid().ToString() };
            var client = new TelemetryClient(configuration);

            var expectedKey = Guid.NewGuid().ToString();
            client.Context.InstrumentationKey = expectedKey;
            client.TrackTrace("Test Message");

            Assert.Equal(expectedKey, client.Context.InstrumentationKey);
        }

        [TestMethod]
        public void TrackDoesNotUsesInstrumentationKeyFromEnvironmentVariableWhenTheInstrumenationKeyIsPresentInConfiguration()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var configuration = new TelemetryConfiguration { TelemetryChannel = channel};
            var client = new TelemetryClient(configuration);
            var observe = client.Context.InstrumentationKey;

            string expectedKey = Guid.NewGuid().ToString();
            configuration.InstrumentationKey = expectedKey;

            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", Guid.NewGuid().ToString());
            
            Assert.DoesNotThrow(() => client.TrackTrace("Test Message"));
            Assert.Equal(expectedKey, sentTelemetry.Context.InstrumentationKey);

            Environment.SetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY", null);
        }

        [TestMethod]
        public void TrackDoesNotSendDataWhenTelemetryIsDisabled()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { DisableTelemetry = true , TelemetryChannel = channel };

            var client = new TelemetryClient(configuration) {};

            client.Track(new StubTelemetry());

            Assert.Equal(0, sentTelemetry.Count);
        }

        [TestMethod]
        public void TrackRespectsInstrumentaitonKeyOfTelemetryItem()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { 
                // no instrumentation key set here
                TelemetryChannel = channel
            };

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
            var configuration = new TelemetryConfiguration
            {
                // no instrumentation key set here
                TelemetryChannel = channel
            };

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
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key", TelemetryChannel = new StubTelemetryChannel() };
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

                var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key", TelemetryChannel = new StubTelemetryChannel() };
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
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = channel,
                InstrumentationKey = "Test key"
            };
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
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = channel,
                InstrumentationKey = "Test key"
            };
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());

            Assert.Equal("true", ((ISupportProperties)sentTelemetry).Properties["DeveloperMode"]);
        }

        [TestMethod]
        public void TrackDoesNotTryAddingDeveloperModeCustomPropertyWhenTelemetryDoesNotSupportCustomProperties()
        {
            var channel = new StubTelemetryChannel { DeveloperMode = true };
            var configuration = new TelemetryConfiguration { TelemetryChannel = channel, InstrumentationKey = "Test Key" };
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
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = channel,
                InstrumentationKey = "Test key"
            };
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());

            Assert.NotEqual(DateTimeOffset.MinValue, sentTelemetry.Timestamp);
        }

        [TestMethod]
        public void TrackWritesTelemetryToDebugOutputIfIKeyEmpty()
        {
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
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = channel,
                InstrumentationKey = ""

            };
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
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = channel,
                InstrumentationKey = "123"

            };
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());
            
            Assert.True(actualMessage.StartsWith("Application Insights Telemetry: "));
            PlatformSingleton.Current = null;
        }


        [TestMethod]
        public void TrackDoesNotWriteTelemetryToDebugOutputIfNotInDeveloperMode()
        {
            string actualMessage = null;
            var debugOutput = new StubDebugOutput { OnWriteLine = message => actualMessage = message };
            PlatformSingleton.Current = new StubPlatform { OnGetDebugOutput = () => debugOutput };
            var channel = new StubTelemetryChannel();
            var configuration = new TelemetryConfiguration
            {
                TelemetryChannel = channel,
                InstrumentationKey = "Test key"
            };
            var client = new TelemetryClient(configuration);

            client.Track(new StubTelemetry());
            PlatformSingleton.Current = null;
            Assert.Null(actualMessage);
        }

        [TestMethod]
        public void TrackCopiesPropertiesFromClientToTelemetry()
        {
            var configuration = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
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
            var configuration = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel() };
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

            var configuration = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel(), TelemetryInitializers = { initializer } };

            var client = new TelemetryClient(configuration);
            client.Context.Properties[PropertyName] = "ClientValue";
            client.Context.InstrumentationKey = "Test Key";

            client.Track(new StubTelemetry());

            Assert.Equal(client.Context.Properties[PropertyName], valueInInitializer);
        }

        [TestMethod]
        public void TrackWhenChannelIsNullWillThrowInvalidOperationException()
        {
            var config = new TelemetryConfiguration();
            config.InstrumentationKey = "Foo";
            var client = new TelemetryClient(config);

            Assert.Throws<InvalidOperationException>(() => client.TrackTrace("test trace"));
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

            var configuration = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel(), InstrumentationKey = Guid.NewGuid().ToString() };
            var client = new TelemetryClient(configuration);

            client.Context.InstrumentationKey = "Test";
            EventTelemetry eventTelemetry = new EventTelemetry("test");
            client.Track(eventTelemetry);

            Assert.Equal("dotnet:"+ string.Join(".", versionParts[0], versionParts[1], versionParts[2]) + "-" + versionParts[3], eventTelemetry.Context.Internal.SdkVersion);
        }

        [TestMethod]
        public void TrackDoesNotOverrideSdkVersion()
        {
            var configuration = new TelemetryConfiguration { TelemetryChannel = new StubTelemetryChannel(), InstrumentationKey = Guid.NewGuid().ToString() };
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
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key", TelemetryChannel = channel };

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
            var config = new TelemetryConfiguration { InstrumentationKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8" };
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

            HttpClient client = new HttpClient();
            var result = client.PostAsync(
                "https://dc.services.visualstudio.com/v2/validate",
                new ByteArrayContent(Encoding.UTF8.GetBytes(json))).GetAwaiter().GetResult();

            if (result.StatusCode != HttpStatusCode.OK)
            {
                var response = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                Trace.WriteLine(response);
            }

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        }

        #endregion

        private TelemetryClient InitializeTelemetryClient(ICollection<ITelemetry> sentTelemetry)
        {
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var telemetryConfiguration = new TelemetryConfiguration { InstrumentationKey = Guid.NewGuid().ToString(), TelemetryChannel = channel};
            var client = new TelemetryClient(telemetryConfiguration);
            return client;
        }
    }
}
