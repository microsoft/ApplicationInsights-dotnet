namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenTelemetry;
    using OpenTelemetry.Logs;

    [TestClass]
    public class TelemetryClientTest
    {
        private List<LogRecord> logItems;
        private TelemetryClient telemetryClient;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.logItems = new List<LogRecord>();
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + configuration.InstrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b.WithLogging(l => l.AddInMemoryExporter(logItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        #region TrackException

        [TestMethod]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedNameToProvideSimplestWayOfSendingExceptionTelemetry()
        {
            Exception ex = new Exception("Test exception message");
            this.telemetryClient.TrackException(ex);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreSame(ex, logRecord.Exception);
            Assert.AreEqual(LogLevel.Error, logRecord.LogLevel);
        }

        [TestMethod]
        public void TrackExceptionWillUseRequiredFieldAsTextForTheExceptionNameWhenTheExceptionNameIsEmptyToHideUserErrors()
        {
            this.telemetryClient.TrackException((Exception)null);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("n/a", logRecord.Exception.Message);
        }

        [TestMethod]
        public void TrackExceptionSendsExceptionTelemetryWithSpecifiedObjectTelemetry()
        {
            Exception ex = new Exception("Test telemetry exception");
            this.telemetryClient.TrackException(new ExceptionTelemetry(ex));

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("Test telemetry exception", logRecord.Exception.Message);
        }

        [TestMethod]
        public void TrackExceptionWillUseABlankObjectAsTheExceptionToHideUserErrors()
        {
            this.telemetryClient.TrackException((ExceptionTelemetry)null);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
        }

        [TestMethod]
        public void TrackExceptionUsesErrorLogLevelByDefault()
        {
            this.telemetryClient.TrackException(new Exception());

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            Assert.AreEqual(LogLevel.Error, this.logItems[0].LogLevel);
        }

        [TestMethod]
        public void TrackExceptionWithExceptionTelemetryRespectsSeverityLevel()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Critical error"))
            {
                SeverityLevel = SeverityLevel.Critical
            };
            this.telemetryClient.TrackException(telemetry);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            Assert.AreEqual(LogLevel.Critical, this.logItems[0].LogLevel);
        }

        [TestMethod]
        public void TrackExceptionWithPropertiesIncludesPropertiesInLogRecord()
        {
            var properties = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            this.telemetryClient.TrackException(new Exception("Test"), properties);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            
            // Properties should be in the log record attributes
            var hasKey1 = false;
            var hasKey2 = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "key1" && attr.Value?.ToString() == "value1")
                        hasKey1 = true;
                    if (attr.Key == "key2" && attr.Value?.ToString() == "value2")
                        hasKey2 = true;
                }
            }

            Assert.IsTrue(hasKey1, "Property key1 should be in log record");
            Assert.IsTrue(hasKey2, "Property key2 should be in log record");
        }

        [TestMethod]
        public void TrackExceptionWithExceptionTelemetryIncludesProperties()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Test exception"));
            telemetry.Properties["customKey"] = "customValue";
            
            this.telemetryClient.TrackException(telemetry);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            
            var hasCustomKey = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "customKey" && attr.Value?.ToString() == "customValue")
                    {
                        hasCustomKey = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(hasCustomKey, "Custom property should be in log record");
        }

        [TestMethod]
        public void TrackExceptionWithInnerExceptionPreservesInnerException()
        {
            var innerException = new InvalidOperationException("Inner exception message");
            var outerException = new ApplicationException("Outer exception message", innerException);
            
            this.telemetryClient.TrackException(outerException);

            this.telemetryClient.Flush();

            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("Outer exception message", logRecord.Exception.Message);
            
            // The exception should have inner exception
            Assert.IsNotNull(logRecord.Exception.InnerException);
            Assert.AreEqual("Inner exception message", logRecord.Exception.InnerException.Message);
        }

        #endregion

        private double ComputeSomethingHeavy()
        {
            var random = new Random();
            double res = 0;
            for (int i = 0; i < 10000; i++)
            {
                res += Math.Sqrt(random.NextDouble());
            }

            return res;
        }
    }
}
