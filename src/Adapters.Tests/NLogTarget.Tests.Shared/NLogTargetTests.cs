namespace Microsoft.ApplicationInsights.NLogTarget.Tests
{
    using System;
    using System.Linq;

    using Microsoft.ApplicationInsights.CommonTestShared;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.NLogTarget;
    using Microsoft.ApplicationInsights.Tracing.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NLog;
    using NLog.Config;

    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Disposing the object on the TestCleanup method")]
    public class ApplicationInsightsTargetTests
    {
        private AdapterHelper adapterHelper;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            this.adapterHelper = new AdapterHelper();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.adapterHelper.Dispose();
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void InitializeTargetNotThrowsWhenInstrumentationKeyIsNull()
        {
            try
            {
                this.CreateTargetWithGivenInstrumentationKey(null);
            }
            catch (NLogConfigurationException ex)
            {
                Assert.Fail("Not expecting to get NLogConfigurationException but was thrown {0}", ex.Message);
            }
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void InitializeTargetNotThrowsWhenInstrumentationKeyIsEmptyString()
        {
            try
            {
                this.CreateTargetWithGivenInstrumentationKey(string.Empty);
            }
            catch (NLogConfigurationException ex)
            {
                Assert.Fail("Expected NLogConfigurationException but none was thrown with message:{0}", ex.Message);
            }
        }
        
        [TestMethod]
        [TestCategory("NLogTarget")]
        public void ExceptionsDoNotEscapeNLog()
        {
            string instrumentationKey = "93d9c2b7-e633-4571-8520-d391511a1df5";

            Action<Logger> loggerAction = aiLogger => aiLogger.Trace("Hello World");
            this.CreateTargetWithGivenInstrumentationKey(instrumentationKey, loggerAction);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TracesAreEnqueuedInChannel()
        {
            string instrumentationKey = "93d9c2b7-e633-4571-8520-d391511a1df5";

            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey(instrumentationKey);
            this.VerifyMessagesInMockChannel(aiLogger, instrumentationKey);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void InstrumentationKeyIsReadFromEnvironment()
        {
            string instrumentationKey = "F8474271-D231-45B6-8DD4-D344C309AE69";

            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey(instrumentationKey);
            this.VerifyMessagesInMockChannel(aiLogger, instrumentationKey);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceAreEnqueuedInChannelAndContainAllProperties()
        {
            string instrumentationKey = "F8474271-D231-45B6-8DD4-D344C309AE69";

            Random random = new Random();
            int number = random.Next();

            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey(instrumentationKey);

            aiLogger.Debug("Message {0}, using instrumentation key:{1}", number, instrumentationKey);

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.IsNotNull(telemetry, "Didn't get the log event from the channel");

            string loggerName;
            telemetry.Properties.TryGetValue("LoggerName", out loggerName);
            Assert.AreEqual("AITarget", loggerName);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void SdkVersionIsCorrect()
        {
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();

            aiLogger.Debug("Message");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.First();

            string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(ApplicationInsightsTarget), prefix: "nlog:");
            Assert.AreEqual(expectedVersion, telemetry.Context.GetInternalContext().SdkVersion);
        }

        [TestMethod]
        [Ignore]
        [TestCategory("NLogTarget")]
        public void TelemetryIsAcceptedByValidateEndpoint()
        {
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();

            aiLogger.Debug("Message");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.First();

            Assert.IsNull(TelemetrySender.ValidateEndpointSend(telemetry));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceHasTimestamp()
        {
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();

            aiLogger.Debug("Message");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.IsNotNull(telemetry, "Didn't get the log event from the channel");

            Assert.AreNotEqual((default(DateTimeOffset)), telemetry.Timestamp);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceMessageCanBeFormedUsingLayout()
        {
            ApplicationInsightsTarget target = new ApplicationInsightsTarget();
            target.Layout = @"${uppercase:${level}} ${message}";

            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey("test", null, target);
            
            aiLogger.Debug("Message");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.IsNotNull(telemetry, "Didn't get the log event from the channel");

            Assert.AreEqual("DEBUG Message", telemetry.Message);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceMessageWithoutLayoutDefaultsToMessagePassed()
        {
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();

            aiLogger.Debug("My Message");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.IsNotNull(telemetry, "Didn't get the log event from the channel");

            Assert.AreEqual("My Message", telemetry.Message);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceHasSequesnceId()
        {
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();

            aiLogger.Debug("Message");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.IsNotNull(telemetry, "Didn't get the log event from the channel");

            Assert.AreNotEqual("0", telemetry.Sequence);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceHasCustomProperties()
        {
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();

            var eventInfo = new LogEventInfo(LogLevel.Trace, "TestLogger", "Hello!");
            eventInfo.Properties["Name"] = "Value";
            aiLogger.Log(eventInfo);

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.IsNotNull(telemetry, "Didn't get the log event from the channel");

            Assert.AreEqual("Value", telemetry.Context.Properties["Name"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceAreEnqueuedInChannelAndContainExceptionMessage()
        {
            string instrumentationKey = "F8474271-D231-45B6-8DD4-D344C309AE69";
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey(instrumentationKey);
            Exception expectedException;

            try
            {
                throw new Exception("Test logging exception");
            }
            catch (Exception exception)
            {
                expectedException = exception;
                aiLogger.Debug(exception, "testing exception scenario");
            }

            var telemetry = (ExceptionTelemetry)this.adapterHelper.Channel.SentItems.First();
            Assert.AreEqual(expectedException.Message, telemetry.Exception.Message);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void CustomMessageIsAddedToExceptionTelemetryCustomProperties()
        {
            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();
            
            try
            {
                throw new Exception("Test logging exception");
            }
            catch (Exception exception)
            {
                aiLogger.Debug(exception, "custom message");
            }

            ExceptionTelemetry telemetry = (ExceptionTelemetry)this.adapterHelper.Channel.SentItems.First();
            Assert.IsTrue(telemetry.Properties["Message"].StartsWith("custom message"));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogTraceIsSentAsVerboseTraceItem()
        {
            var aiLogger = this.CreateTargetWithGivenInstrumentationKey("F8474271-D231-45B6-8DD4-D344C309AE69");
            
            aiLogger.Trace("trace");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.AreEqual(SeverityLevel.Verbose, telemetry.SeverityLevel);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogDebugIsSentAsVerboseTraceItem()
        {
            var aiLogger = this.CreateTargetWithGivenInstrumentationKey("F8474271-D231-45B6-8DD4-D344C309AE69");

            aiLogger.Debug("trace");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.AreEqual(SeverityLevel.Verbose, telemetry.SeverityLevel);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogInfoIsSentAsInformationTraceItem()
        {
            var aiLogger = this.CreateTargetWithGivenInstrumentationKey("F8474271-D231-45B6-8DD4-D344C309AE69");

            aiLogger.Info("trace");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.AreEqual(SeverityLevel.Information, telemetry.SeverityLevel);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogWarnIsSentAsWarningTraceItem()
        {
            var aiLogger = this.CreateTargetWithGivenInstrumentationKey("F8474271-D231-45B6-8DD4-D344C309AE69");

            aiLogger.Warn("trace");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.AreEqual(SeverityLevel.Warning, telemetry.SeverityLevel);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogErrorIsSentAsErrorTraceItem()
        {
            var aiLogger = this.CreateTargetWithGivenInstrumentationKey("F8474271-D231-45B6-8DD4-D344C309AE69");

            aiLogger.Error("trace");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.AreEqual(SeverityLevel.Error, telemetry.SeverityLevel);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogFatalIsSentAsCriticalTraceItem()
        {
            var aiLogger = this.CreateTargetWithGivenInstrumentationKey("F8474271-D231-45B6-8DD4-D344C309AE69");

            aiLogger.Fatal("trace");

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.AreEqual(SeverityLevel.Critical, telemetry.SeverityLevel);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogPropertyDuplicateKeyDuplicateValue()
        {
            var aiTarget = new PrivateObject(typeof(ApplicationInsightsTarget));
            var logEventInfo = new LogEventInfo();
            var loggerNameVal = "thisisaloggername";

            logEventInfo.LoggerName = loggerNameVal;
            logEventInfo.Properties.Add("LoggerName", loggerNameVal);

            var traceTelemetry = new TraceTelemetry();

            aiTarget.Invoke("BuildPropertyBag", logEventInfo, traceTelemetry);

            Assert.IsTrue(traceTelemetry.Properties.ContainsKey("LoggerName"));
            Assert.AreEqual(loggerNameVal, traceTelemetry.Properties["LoggerName"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogPropertyDuplicateKeyDifferentValue()
        {
            var aiTarget = new PrivateObject(typeof(ApplicationInsightsTarget));
            var logEventInfo = new LogEventInfo();
            var loggerNameVal = "thisisaloggername";
            var loggerNameVal2 = "thisisadifferentloggername";

            logEventInfo.LoggerName = loggerNameVal;
            logEventInfo.Properties.Add("LoggerName", loggerNameVal2);

            var traceTelemetry = new TraceTelemetry();

            aiTarget.Invoke("BuildPropertyBag", logEventInfo, traceTelemetry);

            Assert.IsTrue(traceTelemetry.Properties.ContainsKey("LoggerName"));
            Assert.AreEqual(loggerNameVal, traceTelemetry.Properties["LoggerName"]);

            Assert.IsTrue(traceTelemetry.Properties.ContainsKey("LoggerName_1"));
            Assert.AreEqual(loggerNameVal2, traceTelemetry.Properties["LoggerName_1"]);
        }

        private void VerifyMessagesInMockChannel(Logger aiLogger, string instrumentationKey)
        {
            aiLogger.Trace("Sample trace message");
            aiLogger.Debug("Sample debug message");
            aiLogger.Info("Sample informational message");
            aiLogger.Warn("Sample warning message");
            aiLogger.Error("Sample error message");
            aiLogger.Fatal("Sample fatal error message");

            AdapterHelper.ValidateChannel(this.adapterHelper, instrumentationKey, 6);
        }

        private Logger CreateTargetWithGivenInstrumentationKey(
            string instrumentationKey = "TEST",
            Action<Logger> loggerAction = null,
            ApplicationInsightsTarget target = null)
        {
            // Mock channel to validate that our appender is trying to send logs
            TelemetryConfiguration.Active.TelemetryChannel = this.adapterHelper.Channel;

            if (target == null)
            {
                target = new ApplicationInsightsTarget();
            }

            target.InstrumentationKey = instrumentationKey;
            LoggingRule rule = new LoggingRule("*", LogLevel.Trace, target);
            LoggingConfiguration config = new LoggingConfiguration();
            config.AddTarget("AITarget", target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
            Logger aiLogger = LogManager.GetLogger("AITarget");

            if (loggerAction != null)
            {
                loggerAction(aiLogger);
                target.Dispose();
                return null;
            }

            return aiLogger;
        }
    }
}
