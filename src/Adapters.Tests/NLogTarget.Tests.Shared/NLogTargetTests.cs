// -----------------------------------------------------------------------
// <copyright file="NLogTargetTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2014
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.NLogTarget.Tests
{
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.NLogTarget;
    using Microsoft.ApplicationInsights.Tracing.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NLog;
    using NLog.Config;
    using System;
    using System.Linq;

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

            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey();
            this.VerifyMessagesInMockChannel(aiLogger, instrumentationKey);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void TraceAreEnqueuedInChannelAndContainAllCustomProperties()
        {
            string instrumentationKey = "F8474271-D231-45B6-8DD4-D344C309AE69";

            Random random = new Random();
            int number = random.Next();

            Logger aiLogger = this.CreateTargetWithGivenInstrumentationKey(instrumentationKey);

            aiLogger.Debug("Message {0}, using instrumentation key:{1}", number, instrumentationKey);

            var telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.IsNotNull(telemetry, "Didn't get the log event from the channel");

            string level;
            telemetry.Properties.TryGetValue("Level", out level);
            Assert.AreEqual("Debug", level);

            Assert.IsTrue(telemetry.Properties.ContainsKey("SequenceID"));

            string loggerName;
            telemetry.Properties.TryGetValue("LoggerName", out loggerName);
            Assert.AreEqual("AITarget", loggerName);
            
            Assert.IsTrue(telemetry.Properties.ContainsKey("TimeStamp"));
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
                aiLogger.DebugException("testing exception scenario", exception);
            }

            var telemetry = (ExceptionTelemetry)this.adapterHelper.Channel.SentItems.FirstOrDefault();
            Assert.AreEqual(expectedException.Message, telemetry.Exception.Message);
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
            string instrumentationKey = null,
            Action<Logger> loggerAction = null)
        {
            // Mock channel to validate that our appender is trying to send logs
            TelemetryConfiguration.Active.TelemetryChannel = this.adapterHelper.Channel;

            ApplicationInsightsTarget target = new ApplicationInsightsTarget();

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
