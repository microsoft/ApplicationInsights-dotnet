namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.ApplicationInsights.DataContracts;

    using TaskEx = System.Threading.Tasks.Task;

    class TestHealthHeartbeatProvider : HealthHeartbeatProvider
    {
        public List<MetricTelemetry> sentMessages = new List<MetricTelemetry>();

        public void SimulateSend()
        {
            this.Send();
        }
        
        protected new void Send()
        {
            var heartbeat = this.GatherData();
            this.sentMessages.Add(heartbeat);
        }
    }

    [TestClass]
    class HealthHeartbeatTests
    {
        [TestMethod]
        public void InitializeHealthHeartbeat()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null, delayMs: HealthHeartbeatProvider.DefaultHeartbeatIntervalMs, allowedPayloadFields: HealthHeartbeatProvider.DefaultAllowedFieldsInHeartbeatPayload);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatTwiceDoesntFail()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null, delayMs: HealthHeartbeatProvider.DefaultHeartbeatIntervalMs, allowedPayloadFields: HealthHeartbeatProvider.DefaultAllowedFieldsInHeartbeatPayload);
                hbeat.Initialize(configuration: null, delayMs: HealthHeartbeatProvider.DefaultHeartbeatIntervalMs, allowedPayloadFields: HealthHeartbeatProvider.DefaultAllowedFieldsInHeartbeatPayload);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultInterval()
        {
            int nonDefaultInterval = HealthHeartbeatProvider.DefaultHeartbeatIntervalMs * 2;

            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null, delayMs: nonDefaultInterval);
                Assert.AreEqual(nonDefaultInterval, hbeat.HeartbeatIntervalMs);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNullFieldsFails()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                try
                {
                    hbeat.Initialize(configuration: null, delayMs: HealthHeartbeatProvider.DefaultHeartbeatIntervalMs, allowedPayloadFields: null);
                    Assert.Fail("Initialization without allowed payload fields should throw.");
                }
                catch (Exception)
                {
                    // all good
                }

            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithZeroIntervalFails()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                try
                {
                    hbeat.Initialize(configuration: null, delayMs: 0, allowedPayloadFields: HealthHeartbeatProvider.DefaultAllowedFieldsInHeartbeatPayload);
                    Assert.Fail("Initialization without allowed payload fields should throw.");
                }
                catch (Exception)
                {
                    // all good
                }

            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultFieldsToEnable()
        {
            string specificFieldsToEnable = string.Concat(HealthHeartbeatDefaultPayload.FieldRuntimeFrameworkVer, ",", HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer);

            using (var hbeat = new TestHealthHeartbeatProvider())
            {
                int testDelay = 5;
                hbeat.Initialize(configuration: null, delayMs: testDelay, allowedPayloadFields: specificFieldsToEnable);
                Assert.AreEqual(0, String.CompareOrdinal(hbeat.EnabledPayloadFields, specificFieldsToEnable));

                // wait for 3* the delayMs, we should see some payload items with these payload fields.
                Thread.Sleep(testDelay * 3);

                var sentHeartBeat = hbeat.sentMessages.First();
                Assert.IsNotNull(sentHeartBeat);
                
                foreach (var kvp in sentHeartBeat.Properties)
                {
                    Assert.IsTrue(string.CompareOrdinal(kvp.Key, specificFieldsToEnable) >= 0);
                }
            }
        }

        public class TestHeartbeatPayload : IHealthHeartbeatPayloadExtension
        {
            public Stack<KeyValuePair<string, object>> customProperties = new Stack<KeyValuePair<string, object>>();
            public int currentUnhealthyCount = 0;

            public TestHeartbeatPayload()
            {
            }

            public IEnumerable<KeyValuePair<string, object>> GetPayloadProperties()
            {
                return this.customProperties.ToArray();
            }

            public int CurrentUnhealthyCount => this.GetUnhealthyCountAndReset();

            public string Name => "TestHeartbeatPayload";

            private int GetUnhealthyCountAndReset()
            {
                int unhealthyCountThisTime = this.currentUnhealthyCount;
                this.currentUnhealthyCount = 0;
                return unhealthyCountThisTime;
            }
        }

        [TestMethod]
        public void CanExtendHeartbeatPayload()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                TestHeartbeatPayload payloadProperties = new TestHeartbeatPayload();
                hbeat.RegisterHeartbeatPayload(payloadProperties);
            }
        }

        [TestMethod]
        [Ignore]
        public void CanSetDelayBetweenHeartbeats()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void CanSetDelayBetweenHeartbeatsViaConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void DiagnosticsTelemetryModuleCreatesHeartbeatModule()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsDataByDefault()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsUserSpecifiedData()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFields()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFieldsSpecifiedInConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatMetricIsZeroForNoFailureConditionPresent()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatMetricIsNonZeroWhenFailureConditionPresent()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatSentAtProperIntervals()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatSentToMultipleConfiguredComponents()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HealthHeartbeatDisabledInConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void HeartbeatMetricCountAccountsForAllFailures()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void SentHeartbeatContainsExpectedDefaultFields()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void PayloadExtensionHandlesSingleFieldNameCollision()
        {
            string fieldName1 = "osType";

            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore]
        public void PayloadExtensionHandlesMultipleFieldNameCollision()
        {
            string fieldName1 = "payloadEx";
            string fieldName2 = "payloadEx";

            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DefaultPayloadIncludesAppInsightsSdkVersion()
        {
            var defaultPayload = new HealthHeartbeatDefaultPayload("*");
            var defaultProps = defaultPayload.GetPayloadProperties();
            Assert.IsTrue(
                defaultProps.Any(a =>
                { return a.Key.Equals(HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer, StringComparison.Ordinal); }));
        }

        [TestMethod]
        public void DefaultPayloadIncludesOnlySpecifiedProperties()
        {
            string allowedProps = string.Concat(HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer, ",", HealthHeartbeatDefaultPayload.FieldTargetFramework);
            var defaultPayload = new HealthHeartbeatDefaultPayload(allowedProps);
            var defaultProps = defaultPayload.GetPayloadProperties();
            Assert.IsTrue(
                defaultProps.All(a =>
                {
                    return a.Key.Equals(HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer, StringComparison.Ordinal)
                      ||
                      a.Key.Equals(HealthHeartbeatDefaultPayload.FieldTargetFramework, StringComparison.Ordinal);
                }));
        }

    }
}
