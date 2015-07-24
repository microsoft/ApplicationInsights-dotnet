namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
#if CORE_PCL || NET45 || WINRT
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.TestFramework;
#if NET35 || NET40
    using Microsoft.Diagnostics.Tracing;
#endif
#if NET40 || NET45 || NET35
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;
    using AssertEx = Xunit.AssertEx;

    [TestClass]
    public class TelemetryClientTest
    {
        private const string RequiredFieldText = "is a required field";

        [TestMethod]
        public void ContextIsLazilyInitializedFromConfigurationToDefferCosts()
        {
            var configuration = new TelemetryConfiguration();
            TelemetryClient client = new TelemetryClient(configuration);

            configuration.ContextInitializers.Add(new StubContextInitializer { OnInitialize = context => context.User.Id = "Test User Id" });
            TelemetryContext clientContext = client.Context;

            Assert.Equal("Test User Id", clientContext.User.Id);
        }
        
        [TestMethod]
        public void ChannelIsLazilyInitializedFromConfiguration()
        {
            var configuration = new TelemetryConfiguration();
            TelemetryClient client = new TelemetryClient(configuration);

            configuration.TelemetryChannel = new StubTelemetryChannel();
            ITelemetryChannel clientChannel = client.Channel;

            Assert.Same(configuration.TelemetryChannel, clientChannel);
        }

        [TestMethod]
        public void ContextInitializtionShouldInitializeInternalContext()
        {
            // Assume SessionContext.Serialize is called, which is tested by ComponentContextTest
            TelemetryClient client = new TelemetryClient(TelemetryConfiguration.Active);
            client.Context.Session.Id = "TestValue";

            Assert.NotEmpty(client.Context.Internal.SdkVersion);
        }
        
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
        public void TrackEventWillUseRequiredFieldTextForTheEventNameWhenTheEventNameIsEmptyToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackEvent((string)null);

            var eventTelemetry = (EventTelemetry)sentTelemetry.Single();
            Assert.Contains(RequiredFieldText, eventTelemetry.Name, StringComparison.OrdinalIgnoreCase);
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
        public void TrackEventWillUseABlankObjectAsTheEventToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackEvent((EventTelemetry)null);

            var eventTelemetry = (EventTelemetry)sentTelemetry.Single();
            Assert.Contains(RequiredFieldText, eventTelemetry.Name, StringComparison.OrdinalIgnoreCase);
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
        public void TrackMetricPopulatesRequiredNameWhenItIsNullOrEmptyToLetUserKnowAboutTheProblem()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackMetric(null, 42);

            var metric = (MetricTelemetry)sentTelemetry.Single();
            Assert.Contains(RequiredFieldText, metric.Name, StringComparison.OrdinalIgnoreCase);
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
        public void TrackMetricSendsNewMetricTelemetryGivenNullMetricTElemetryToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackMetric(null);

            var metric = (MetricTelemetry)sentTelemetry.Single();
            Assert.Contains(RequiredFieldText, metric.Name, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, metric.Value);
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
        public void TrackTraceWillUseRequiredFieldAsTextForTheTraceNameWhenTheTraceNameIsEmptyToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackTrace((string)null);

            var trace = (TraceTelemetry)sentTelemetry.Single();
            Assert.Contains(RequiredFieldText, trace.Message, StringComparison.OrdinalIgnoreCase);
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
        public void TrackTraceWillUseABlankObjectAsTheTraceToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackTrace((TraceTelemetry)null);

            var trace = (TraceTelemetry)sentTelemetry.Single();
            Assert.Contains(RequiredFieldText, trace.Message, StringComparison.OrdinalIgnoreCase);
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
            Assert.Contains(RequiredFieldText, exceptionTelemetry.Exception.Message, StringComparison.OrdinalIgnoreCase);
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
        public void TrackPageViewWillUseRequiredFieldAsTextForThePageNameWhenTheNameIsEmptyToHideUserErrors()
        {
            var sentTelemetry = new List<ITelemetry>();
            var client = this.InitializeTelemetryClient(sentTelemetry);

            client.TrackPageView((string)null);

            var pageViewTelemetry = (PageViewTelemetry)sentTelemetry.Single();
            Assert.Contains(RequiredFieldText, pageViewTelemetry.Name, StringComparison.OrdinalIgnoreCase);
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
            Assert.Equal("command name", dependency.CommandName);
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
        public void TrackMethodDontThrowsWhenInstrumentationKeyIsEmptyAndNotSendingTheTelemetryItem()
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
        public void TrackUsesInstrumentationKeyFromConfigurationWhenTheInstrumenationKeyIsEmpty()
        {
            ITelemetry sentTelemetry = null;
            var channel = new StubTelemetryChannel { OnSend = telemetry => sentTelemetry = telemetry };
            var configuration = new TelemetryConfiguration { TelemetryChannel = channel };
            var client = new TelemetryClient(configuration);
            var observe = client.Context.InstrumentationKey;
            
            string expectedKey = Guid.NewGuid().ToString();
            configuration.InstrumentationKey = expectedKey;
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
        public void TrackDoesNotSendDataWhenTelemetryIsDisabled()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { DisableTelemetry = true };

            var client = new TelemetryClient(configuration) { Channel = channel };

            client.Track(new StubTelemetry());

            Assert.Equal(0, sentTelemetry.Count);
        }

        [TestMethod]
        public void TrackDoesNotThrowExceptionsDuringTelemetryIntializersInitialize()
        {
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };
            var telemetryInitializer = new StubTelemetryInitializer();
            telemetryInitializer.OnInitialize = item => { throw new Exception(); };
            configuration.TelemetryInitializers.Add(telemetryInitializer);
            var client = new TelemetryClient(configuration) { Channel = new StubTelemetryChannel() };
            Assert.DoesNotThrow(() => client.Track(new StubTelemetry()));
        }

#if !Wp80 && !WINDOWS_PHONE
        [TestMethod]
        public void TrackLogsDiagnosticsMessageOnExceptionsDuringTelemetryIntializersInitialize()
        {
            using (var listener = new TestEventListener())
            {
                listener.EnableEvents(CoreEventSource.Log, EventLevel.Error);

                var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };
                var telemetryInitializer = new StubTelemetryInitializer();
                var exceptionMessage = "Test exception message";
                telemetryInitializer.OnInitialize = item => { throw new Exception(exceptionMessage); };
                configuration.TelemetryInitializers.Add(telemetryInitializer);

                var client = new TelemetryClient(configuration) { Channel = new StubTelemetryChannel() };
                client.Track(new StubTelemetry());

                var exceptionExplanation = "Exception while initializing " + typeof(StubTelemetryInitializer).FullName;
                var diagnosticsMessage = (string)listener.Messages.First().Payload[0];
                Assert.Contains(exceptionExplanation, diagnosticsMessage, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(exceptionMessage, diagnosticsMessage, StringComparison.OrdinalIgnoreCase);
            }
        }
#endif
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

            Assert.DoesNotThrow(() => client.Track(new SessionStateTelemetry()));
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

        #endregion

        private TelemetryClient InitializeTelemetryClient(ICollection<ITelemetry> sentTelemetry)
        {
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var telemetryConfiguration = new TelemetryConfiguration { InstrumentationKey = Guid.NewGuid().ToString() };
            var client = new TelemetryClient(telemetryConfiguration) { Channel = channel };
            return client;
        }
    }
}
