namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;

    [TestClass]
    public class HealthHeartbeatTests
    {
        [TestMethod]
        public void InitializeHealthHeartbeat()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatTwiceDoesntFail()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatDefaultsAreSetProperly()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                Assert.IsNull(hbeat.DisabledHeartbeatProperties);
                Assert.AreEqual(HealthHeartbeatProvider.DefaultHeartbeatIntervalMs, hbeat.HeartbeatInterval.TotalMilliseconds);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultInterval()
        {
            TimeSpan nonDefaultInterval = TimeSpan.FromMilliseconds(10000);

            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: nonDefaultInterval);
                Assert.AreEqual(nonDefaultInterval, hbeat.HeartbeatInterval.TotalMilliseconds);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNullFieldsFails()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                bool initResult = hbeat.Initialize(configuration: null, timeBetweenHeartbeats: TimeSpan.FromMilliseconds(10000), disabledDefaultFields: null);
                Assert.IsTrue(initResult, "Initialization without allowed dissallowed fields should be fine.");
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithZeroIntervalFails()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                bool initResult = hbeat.Initialize(configuration: null, timeBetweenHeartbeats: TimeSpan.FromMilliseconds(0), disabledDefaultFields: null);
                Assert.IsFalse(initResult, "Initialization without a valid delay value (0) should fail.");
            }
        }

        [TestMethod]
        public void CanExtendHeartbeatPayload()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                try
                {
                    hbeat.SetHealthProperty(new HealthHeartbeatProperty("test01", "this is a value", true));
                    hbeat.SetHealthProperty(new HealthHeartbeatProperty("test02", DateTime.Now, true));
                    hbeat.SetHealthProperty(new HealthHeartbeatProperty("test03", 245.678, true));
                    hbeat.SetHealthProperty(new HealthHeartbeatProperty("test04", new List<string>() { "one", "two", "three" }, true));
                }
                catch (Exception e)
                {
                    
                    Assert.Fail(string.Format(CultureInfo.CurrentCulture, "Registration of a heartbeat payload provider throws exception '{0}", e.ToInvariantString()));
                }
            }
        }

        [TestMethod]
        public void CanSetDelayBetweenHeartbeats()
        {
            TimeSpan userSetInterval = TimeSpan.FromMilliseconds(7252.0);

            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                Assert.AreNotEqual(userSetInterval, hbeat.HeartbeatInterval.TotalMilliseconds);

                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: userSetInterval);
                Assert.AreEqual(userSetInterval, hbeat.HeartbeatInterval);
            }
        }

        [TestMethod]
        [Ignore("Not ready yet")]
        public void CanSetDelayBetweenHeartbeatsViaConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        public void DiagnosticsTelemetryModuleCreatesHeartbeatModule()
        {
            using (var diagModule = new DiagnosticsTelemetryModule())
            {
                diagModule.Initialize(new TelemetryConfiguration());
                Assert.IsNotNull(diagModule.HeartbeatProvider);
            }
        }

        [TestMethod]
        public void HeartbeatPayloadContainsDataByDefault()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                var hbeatPayloadData = hbeat.GetGatheredDataProperties();
                Assert.IsNotNull(hbeatPayloadData);
            }
            throw new NotImplementedException();
        }

        [TestMethod]
        public void HeartbeatPayloadContainsUserSpecifiedData()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                string testerKey = "tester123";
                hbeat.SetHealthProperty(new HealthHeartbeatProperty(testerKey, "test", true));
                hbeat.SimulateSend();
                bool contentFound = false;
                foreach (var msg in hbeat.sentMessages)
                {
                    contentFound = msg.Properties.Any(a => a.Key.Equals(testerKey, StringComparison.OrdinalIgnoreCase));
                    if (contentFound)
                    {
                        break;
                    }
                }
                Assert.IsTrue(contentFound, "Provided custom payload provider to heartbeat but never received any messages with its content");
            }
        }

        [TestMethod]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFields()
        {
            List<string> specificFieldsToEnable = new List<string>();
            for (int i = 0; i < HealthHeartbeatDefaultPayload.DefaultFields.Length; ++i)
            {
                if (i % 2 == 0)
                {
                    specificFieldsToEnable.Add(HealthHeartbeatDefaultPayload.DefaultFields[i]);
                }
            }

            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: null, allowedPayloadFields: specificFieldsToEnable);
                Assert.AreEqual(hbeat.DisabledHeartbeatProperties.Count(), specificFieldsToEnable.Count);
                foreach (string fld in hbeat.DisabledHeartbeatProperties)
                {
                    Assert.IsTrue(specificFieldsToEnable.Contains(fld));
                }

                hbeat.SimulateSend();

                var sentHeartBeat = hbeat.sentMessages.First();
                Assert.IsNotNull(sentHeartBeat);

                foreach (var kvp in sentHeartBeat.Properties)
                {
                    Assert.IsTrue(specificFieldsToEnable.Contains(kvp.Key), string.Format(CultureInfo.CurrentCulture, "Dissallowed field '{0}' found in payload", kvp.Key));
                }
            }
        }

        [TestMethod]
        [Ignore("I don't know how to modify the config file during tests yet")]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFieldsSpecifiedInConfig()
        {
            // FROM: HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFields below...

            //string specificFieldsToEnable = string.Concat(HealthHeartbeatDefaultPayload.FieldRuntimeFrameworkVer, ",", HealthHeartbeatDefaultPayload.FieldAppInsightsSdkVer);

            //using (var hbeat = new HealthHeartbeatProviderMock())
            //{
            //    hbeat.Initialize(configuration: null, delayMs: null, allowedPayloadFields: specificFieldsToEnable);
            //    Assert.AreEqual(0, String.CompareOrdinal(hbeat.EnabledPayloadFields, specificFieldsToEnable));

            //    hbeat.SimulateSend();

            //    var sentHeartBeat = hbeat.sentMessages.First();
            //    Assert.IsNotNull(sentHeartBeat);

            //    foreach (var kvp in sentHeartBeat.Properties)
            //    {
            //        Assert.IsTrue(specificFieldsToEnable.IndexOf(kvp.Key, 0, StringComparison.OrdinalIgnoreCase) >= 0, "Dissallowed field found in payload");
            //    }
            //}
        }

        [TestMethod]
        public void HeartbeatMetricIsZeroForNoFailureConditionPresent()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                hbeat.SimulateSend();
                Assert.IsFalse(hbeat.sentMessages.Any(a => a.Sum > 0.0));
            }
        }

        [TestMethod]
        public void HeartbeatMetricIsNonZeroWhenFailureConditionPresent()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                string testerKey = "tester123";
                hbeat.SetHealthProperty(new HealthHeartbeatProperty(testerKey, "test", false));
                hbeat.SimulateSend();
                Assert.IsTrue(hbeat.sentMessages.Any(a => a.Sum >= 1.0));
            }
        }

        [TestMethod]
        [Ignore("Not a stable unit test to say the least. Perhaps this test can be made functional?")]
        public void HeartbeatSentAtProperIntervals()
        {
            DataContracts.MetricTelemetry[] messages = null;
            long testStartTicks = DateTimeOffset.Now.Ticks;
            
            using (var hbeat = new HealthHeartbeatProviderMock(disableBaseHeartbeatTimer: false))
            {
                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: TimeSpan.FromMilliseconds(100));
                Thread.Sleep(150);
                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: TimeSpan.FromMilliseconds(100000));
                Thread.Sleep(150);
                messages = hbeat.sentMessages.ToArray();
            }

            if (messages != null)
            {
                long tolerance = 10;
                var avg = messages.Average(a => a.Timestamp.Ticks);
                var max = messages.Max(a => a.Timestamp.Ticks);
                var min = messages.Min(a => a.Timestamp.Ticks);

                // tolerance for messages received makes this test slightly more robust

                Assert.IsTrue(Math.Abs(avg - min) < tolerance);
                Assert.IsTrue(Math.Abs(avg - max) < tolerance);

            }
        }

        [TestMethod]
        [Ignore("No test yet, I don't know how to setup multiple ikey's to send to yet.")]
        public void HeartbeatSentToMultipleConfiguredComponents()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore("I don't know how to alter the config file during unit tests yet.")]
        public void HealthHeartbeatDisabledInConfig()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        public void HeartbeatMetricCountAccountsForAllFailures()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                hbeat.SimulateSend();
                Assert.IsTrue(hbeat.sentMessages.First()?.Sum == 0.0);
                hbeat.sentMessages.Clear();

                hbeat.SetHealthProperty(new HealthHeartbeatProperty("tester01", "test failure 1", false));
                hbeat.SetHealthProperty(new HealthHeartbeatProperty("tester02", "test failure 2", false));
                hbeat.SimulateSend();

                Assert.IsTrue(hbeat.sentMessages.First()?.Sum == 2.0);
            }
        }

        [TestMethod]
        public void SentHeartbeatContainsExpectedDefaultFields()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                hbeat.SimulateSend();
                MetricTelemetry sentMsg = hbeat.sentMessages.First();
                Assert.IsNotNull(sentMsg);

                foreach (string field in HealthHeartbeatDefaultPayload.DefaultFields)
                {
                    try
                    {
                        sentMsg.Properties.Single(a => string.Compare(a.Key, field) == 0);
                    }
                    catch (Exception)
                    {
                        Assert.Fail(string.Format(CultureInfo.CurrentCulture, "The default field '{0}' is not present exactly once in a sent message.", field));
                    }
                }
            }
        }

        [TestMethod]
        [Ignore("Not ready yet")]
        public void PayloadExtensionHandlesSingleFieldNameCollision()
        {
            string fieldName1 = "osType";

            using (var hbeat = new HealthHeartbeatProvider())
            {

            }
            throw new NotImplementedException();
        }

        [TestMethod]
        [Ignore("Not ready yet")]
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
        public void EnsureAllTargetFrameworksRepresented()
        {
            var defaultHeartbeatPayload = new HealthHeartbeatDefaultPayload();
            var props = defaultHeartbeatPayload.GetPayloadProperties();
            foreach (var kvp in props)
            {
                if (kvp.Key.Equals("targetFramework", StringComparison.Ordinal))
                {
                    Assert.IsFalse(kvp.Value.PayloadValue.Equals("undefined", StringComparison.OrdinalIgnoreCase));
                }
            }
        }
    }
}
