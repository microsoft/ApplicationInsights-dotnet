namespace Microsoft.ApplicationInsights.NLogTarget.Tests
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.NLogTarget;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NLog;
    using NLog.Config;
    using NLog.Targets;
    using TargetPropertyWithContext = Microsoft.ApplicationInsights.NLogTarget.TargetPropertyWithContext;

    [TestClass]
    public class ApplicationInsightsTargetTests
    {
        private const string DefaultInstrumentationKey = "F8474271-D231-45B6-8DD4-D344C309AE69";
        private const string DefaultConnectionString = "InstrumentationKey=" + DefaultInstrumentationKey;

        private TelemetryTestEnvironment telemetryEnvironment;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            LogManager.ThrowExceptions = true;
            this.telemetryEnvironment = new TelemetryTestEnvironment();
            this.telemetryEnvironment.Collector.Clear();
            LogManager.Configuration = null;
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                LogManager.Configuration = null;
            }
            finally
            {
                LogManager.Shutdown();
            }

            NLog.GlobalDiagnosticsContext.Clear();
            this.telemetryEnvironment?.Dispose();
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void InitializeTargetThrowsWhenConnectionStringIsNull()
        {
            var target = new ApplicationInsightsTarget
            {
                ConnectionString = null
            };

            var rule = new LoggingRule("*", LogLevel.Trace, target);
            var config = new LoggingConfiguration();
            config.AddTarget("AITarget", target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
            var logger = LogManager.GetLogger("AITarget");

            Assert.ThrowsException<NLogConfigurationException>(() => logger.Info("trigger"));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void InitializeTargetThrowsWhenConnectionStringIsEmpty()
        {
            var target = new ApplicationInsightsTarget
            {
                ConnectionString = string.Empty
            };

            var rule = new LoggingRule("*", LogLevel.Trace, target);
            var config = new LoggingConfiguration();
            config.AddTarget("AITarget", target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
            var logger = LogManager.GetLogger("AITarget");

            Assert.ThrowsException<NLogConfigurationException>(() => logger.Info("trigger"));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void ExceptionsDoNotEscapeNLog()
        {
            var logger = this.CreateLogger();
            logger.Trace("Hello World");
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task TracesAreCapturedByExporter()
        {
            var logger = this.CreateLogger();
            this.EmitAllLevels(logger);

            await this.telemetryEnvironment.WaitForTelemetryAsync(expectedItemCount: 6).ConfigureAwait(false);

            var traces = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>();
            Assert.AreEqual(6, traces.Count, $"Collector saw {this.telemetryEnvironment.Collector.TotalEnvelopesSeen} envelopes");
            foreach (var trace in traces)
            {
                Assert.AreEqual(DefaultInstrumentationKey, trace.InstrumentationKey, "Unexpected instrumentation key");
            }
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task ConnectionStringIsResolvedFromEnvironment()
        {
            const string variableName = "APPINSIGHTS_CONNECTION_STRING";
            var connectionString = $"InstrumentationKey={Guid.NewGuid():D}";
            Environment.SetEnvironmentVariable(variableName, connectionString);

            try
            {
                var target = new ApplicationInsightsTarget
                {
                    ConnectionString = $"${{environment:variable={variableName}}}"
                };

                var logger = this.CreateLogger(connectionString: null, target: target);
                logger.Info("environment based");

                await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

                var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
                Assert.AreEqual(ExtractInstrumentationKey(connectionString), trace.InstrumentationKey);
            }
            finally
            {
                Environment.SetEnvironmentVariable(variableName, null);
            }
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task ConnectionStringIsResolvedFromLayout()
        {
            var connectionString = $"InstrumentationKey={Guid.NewGuid():D}";
            var gdcKey = Guid.NewGuid().ToString();
            NLog.GlobalDiagnosticsContext.Set(gdcKey, connectionString);

            var target = new ApplicationInsightsTarget
            {
                ConnectionString = $"${{gdc:item={gdcKey}}}"
            };

            var logger = this.CreateLogger(connectionString: null, target: target);
            logger.Info("layout based");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual(ExtractInstrumentationKey(connectionString), trace.InstrumentationKey);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task TracePayloadIncludesProperties()
        {
            var logger = this.CreateLogger();
            var value = Guid.NewGuid().ToString("N");

            logger.Debug("Message {0}", value);

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual("AITarget", trace.Properties["LoggerName"]);
            Assert.IsTrue(trace.Properties.Values.Any(v => v.Contains(value, StringComparison.Ordinal)));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task SdkVersionIsPresentInExportedProperties()
        {
            var logger = this.CreateLogger();
            logger.Debug("Message");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.IsTrue(trace.Properties.TryGetValue("ai.internal.sdkVersion", out var sdkVersion));

            var expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(prefix: "nlog:", loggerType: typeof(ApplicationInsightsTarget));
            Assert.AreEqual(expectedVersion, sdkVersion);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task TraceHasTimestamp()
        {
            var logger = this.CreateLogger();
            logger.Debug("Message");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreNotEqual(default, trace.Timestamp);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task TraceMessageRespectsLayout()
        {
            var target = new ApplicationInsightsTarget
            {
                Layout = @"${uppercase:${level}} ${message}"
            };

            var logger = this.CreateLogger(connectionString: DefaultConnectionString, target: target);
            logger.Debug("Message");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual("DEBUG Message", trace.Message);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task TraceMessageDefaultsToOriginal()
        {
            var logger = this.CreateLogger();
            logger.Debug("My Message");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual("My Message", trace.Message);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        [Ignore("Sequence number is managed by the exporter and not exposed in the shim telemetry payload.")]
        public void TraceHasSequenceId()
        {
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task TraceHasCustomProperties()
        {
            var logger = this.CreateLogger();
            var eventInfo = new LogEventInfo(LogLevel.Trace, "TestLogger", "Hello!");
            eventInfo.Properties["Name"] = "Value";
            logger.Log(eventInfo);

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual("Value", trace.Properties["Name"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task GlobalDiagnosticContextPropertiesAreAddedToProperties()
        {
            var target = new ApplicationInsightsTarget();
            target.ContextProperties.Add(new TargetPropertyWithContext("global_prop", "${gdc:item=global_prop}"));
            var logger = this.CreateLogger(target: target);

            NLog.GlobalDiagnosticsContext.Set("global_prop", "global_value");
            logger.Debug("Message");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual("global_value", trace.Properties["global_prop"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task GlobalDiagnosticContextPropertiesSupplementEventProperties()
        {
            var target = new ApplicationInsightsTarget();
            target.ContextProperties.Add(new TargetPropertyWithContext("global_prop", "${gdc:item=global_prop}"));
            var logger = this.CreateLogger(target: target);

            NLog.GlobalDiagnosticsContext.Set("global_prop", "global_value");

            var eventInfo = new LogEventInfo(LogLevel.Trace, "TestLogger", "Hello!");
            eventInfo.Properties["Name"] = "Value";
            logger.Log(eventInfo);

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual("global_value", trace.Properties["global_prop"]);
            Assert.AreEqual("Value", trace.Properties["Name"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
#pragma warning disable CA1707 // Identifiers should not contain underscores
        public async Task EventPropertyKeyNameIsAppendedWith_1_WhenConflictingWithGlobalDiagnosticContext()
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            var target = new ApplicationInsightsTarget();
            target.ContextProperties.Add(new TargetPropertyWithContext("Name", "${gdc:item=Name}"));
            var logger = this.CreateLogger(target: target);

            NLog.GlobalDiagnosticsContext.Set("Name", "Global Value");
            var eventInfo = new LogEventInfo(LogLevel.Trace, "TestLogger", "Hello!");
            eventInfo.Properties["Name"] = "Value";
            logger.Log(eventInfo);

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual("Global Value", trace.Properties["Name"]);
            Assert.AreEqual("Value", trace.Properties["Name_1"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task ExceptionTelemetryContainsExceptionDetails()
        {
            var logger = this.CreateLogger();
            Exception expectedException;

            try
            {
                throw new Exception("Test logging exception");
            }
            catch (Exception exception)
            {
                expectedException = exception;
                logger.Debug(exception, "testing exception scenario");
            }

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var exceptionTelemetry = this.telemetryEnvironment.Collector.GetTelemetryOfType<ExceptionTelemetryEnvelope>().Single();
            Assert.AreEqual("System.Exception: Test logging exception", exceptionTelemetry.Message);
            Assert.AreEqual(expectedException.GetType().FullName, exceptionTelemetry.TypeName);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task CustomMessageIsAddedToExceptionTelemetryCustomProperties()
        {
            var logger = this.CreateLogger();

            try
            {
                throw new Exception("Test logging exception");
            }
            catch (Exception exception)
            {
                logger.Debug(exception, "custom message");
            }

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var exceptionTelemetry = this.telemetryEnvironment.Collector.GetTelemetryOfType<ExceptionTelemetryEnvelope>().Single();
            Assert.AreEqual("System.Exception: Test logging exception", exceptionTelemetry.Message);
            Assert.IsTrue(exceptionTelemetry.Properties.TryGetValue("Message", out var messageProperty) && messageProperty.StartsWith("custom message", StringComparison.Ordinal));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task NLogTraceIsSentAsVerboseTraceItem()
        {
            var logger = this.CreateLogger();
            logger.Trace("trace");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual(SeverityLevel.Verbose, ToSeverityLevel(trace.SeverityLevel));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task NLogDebugIsSentAsVerboseTraceItem()
        {
            var logger = this.CreateLogger();
            logger.Debug("trace");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual(SeverityLevel.Verbose, ToSeverityLevel(trace.SeverityLevel));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task NLogInfoIsSentAsInformationTraceItem()
        {
            var logger = this.CreateLogger();
            logger.Info("trace");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual(SeverityLevel.Information, ToSeverityLevel(trace.SeverityLevel));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task NLogWarnIsSentAsWarningTraceItem()
        {
            var logger = this.CreateLogger();
            logger.Warn("trace");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual(SeverityLevel.Warning, ToSeverityLevel(trace.SeverityLevel));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task NLogErrorIsSentAsErrorTraceItem()
        {
            var logger = this.CreateLogger();
            logger.Error("trace");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual(SeverityLevel.Error, ToSeverityLevel(trace.SeverityLevel));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public async Task NLogFatalIsSentAsCriticalTraceItem()
        {
            var logger = this.CreateLogger();
            logger.Fatal("trace");

            await this.telemetryEnvironment.WaitForTelemetryAsync(1).ConfigureAwait(false);

            var trace = this.telemetryEnvironment.Collector.GetTelemetryOfType<TraceTelemetryEnvelope>().Single();
            Assert.AreEqual(SeverityLevel.Critical, ToSeverityLevel(trace.SeverityLevel));
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogPropertyDuplicateKeyDuplicateValue()
        {
            var aiTarget = new ApplicationInsightsTarget();
            var logEventInfo = new LogEventInfo();
            var loggerNameVal = "thisisaloggername";

            logEventInfo.LoggerName = loggerNameVal;
            logEventInfo.Properties.Add("LoggerName", loggerNameVal);

            var traceTelemetry = new TraceTelemetry();

            aiTarget.BuildPropertyBag(logEventInfo, traceTelemetry);

            Assert.IsTrue(traceTelemetry.Properties.ContainsKey("LoggerName"));
            Assert.AreEqual(loggerNameVal, traceTelemetry.Properties["LoggerName"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogPropertyDuplicateKeyDifferentValue()
        {
            var aiTarget = new ApplicationInsightsTarget();
            var logEventInfo = new LogEventInfo();
            var loggerNameVal = "thisisaloggername";
            var loggerNameVal2 = "thisisadifferentloggername";

            logEventInfo.LoggerName = loggerNameVal;
            logEventInfo.Properties.Add("LoggerName", loggerNameVal2);

            var traceTelemetry = new TraceTelemetry();

            aiTarget.BuildPropertyBag(logEventInfo, traceTelemetry);

            Assert.IsTrue(traceTelemetry.Properties.ContainsKey("LoggerName"));
            Assert.AreEqual(loggerNameVal, traceTelemetry.Properties["LoggerName"]);

            Assert.IsTrue(traceTelemetry.Properties.ContainsKey("LoggerName_1"));
            Assert.AreEqual(loggerNameVal2, traceTelemetry.Properties["LoggerName_1"]);
        }

        [TestMethod]
        [TestCategory("NLogTarget")]
        public void NLogTargetFlushesTelemetryClient()
        {
            var logger = this.CreateLogger();

            var flushEvent = new ManualResetEvent(false);
            Exception flushException = null;
            NLog.Common.AsyncContinuation asyncContinuation = ex =>
            {
                flushException = ex;
                flushEvent.Set();
            };

            logger.Factory.Flush(asyncContinuation, 5000);
            Assert.IsTrue(flushEvent.WaitOne(TimeSpan.FromSeconds(5)));
            Assert.IsNull(flushException, "Flush should complete without error");
        }

        private Logger CreateLogger(string connectionString = DefaultConnectionString, ApplicationInsightsTarget target = null)
        {
            target ??= new ApplicationInsightsTarget();

            if (connectionString != null)
            {
                target.ConnectionString = NormalizeConnectionString(connectionString);
            }

            var rule = new LoggingRule("*", LogLevel.Trace, target);
            var config = new LoggingConfiguration();
            config.AddTarget("AITarget", target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
            var logger = LogManager.GetLogger("AITarget");

            this.telemetryEnvironment.ConfigureTarget(target);

            return logger;
        }

        private void EmitAllLevels(Logger logger)
        {
            logger.Trace("Sample trace message");
            logger.Debug("Sample debug message");
            logger.Info("Sample informational message");
            logger.Warn("Sample warning message");
            logger.Error("Sample error message");
            logger.Fatal("Sample fatal error message");
        }

        private static string ExtractInstrumentationKey(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return string.Empty;
            }

            var parts = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var kvp = part.Split(new[] { '=' }, 2);
                if (kvp.Length == 2 && kvp[0].Trim().Equals("InstrumentationKey", StringComparison.OrdinalIgnoreCase))
                {
                    return kvp[1].Trim();
                }
            }

            return string.Empty;
        }

        private static string NormalizeConnectionString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            if (value.Contains("${", StringComparison.Ordinal))
            {
                return value;
            }

            if (value.Contains('=') )
            {
                return value;
            }

            return $"InstrumentationKey={value}";
        }

        private static SeverityLevel? ToSeverityLevel(int? severity)
        {
            if (!severity.HasValue)
            {
                return null;
            }

            return (SeverityLevel)severity.Value;
        }
    }
}
