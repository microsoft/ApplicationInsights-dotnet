namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HealthHeartbeatTests
    {
        [TestMethod]
        public void InitializeHealthHeartbeatDoesntThrow()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatTwiceDoesntFail()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                hbeat.Initialize(configuration: null);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatDefaultsAreSetProperly()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                Assert.IsNull(hbeat.DisabledHeartbeatProperties);
                Assert.AreEqual(HeartbeatProvider.DefaultHeartbeatIntervalMs, hbeat.Interval.TotalMilliseconds);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultInterval()
        {
            TimeSpan nonDefaultInterval = TimeSpan.FromMilliseconds(10000);

            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: nonDefaultInterval);
                Assert.AreEqual(nonDefaultInterval, hbeat.Interval);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNullFieldsFails()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                bool initResult = hbeat.Initialize(configuration: null, timeBetweenHeartbeats: TimeSpan.FromMilliseconds(10000), disabledDefaultFields: null);
                Assert.IsTrue(initResult, "Initialization without allowed dissallowed fields should be fine.");
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithZeroIntervalFails()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                bool initResult = hbeat.Initialize(configuration: null, timeBetweenHeartbeats: TimeSpan.FromMilliseconds(0), disabledDefaultFields: null);
                Assert.IsFalse(initResult, "Initialization without a valid delay value (0) should fail.");
            }
        }

        [TestMethod]
        public void CanExtendHeartbeatPayload()
        {

            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: new TelemetryConfiguration());

                try
                {
                    Assert.IsTrue(hbeat.AddHealthProperty("test01", "this is a value", true));
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
                Assert.AreNotEqual(userSetInterval, hbeat.Interval.TotalMilliseconds);

                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: userSetInterval);
                Assert.AreEqual(userSetInterval, hbeat.Interval);
            }
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
        public void HeartbeatSequenceCounterOverflow()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);

                hbeat.SequenceCounter = UInt64.MaxValue;

                try
                {
                    hbeat.SimulateSend();
                }
                catch (Exception e)
                {
                    Assert.Fail("Overflow of HeartbeatProvider's sequence counter not handled. Exception: " + e.ToInvariantString());
                }
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
        }

        [TestMethod]
        public void HeartbeatPayloadContainsUserSpecifiedData()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                string testerKey = "tester123";
                Assert.IsTrue(hbeat.AddHealthProperty(testerKey, "test", true));
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
            List<string> disableHbProps = new List<string>();
            for (int i = 0; i < HeartbeatDefaultPayload.DefaultFields.Length; ++i)
            {
                if (i % 2 == 0)
                {
                    disableHbProps.Add(HeartbeatDefaultPayload.DefaultFields[i]);
                }
            }

            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null, timeBetweenHeartbeats: null, disabledFields: disableHbProps);
                Assert.AreEqual(hbeat.DisabledHeartbeatProperties.Count(), disableHbProps.Count);
                foreach (string fld in hbeat.DisabledHeartbeatProperties)
                {
                    Assert.IsTrue(disableHbProps.Contains(fld));
                }

                hbeat.SimulateSend();

                var sentHeartBeat = hbeat.sentMessages.First();
                Assert.IsNotNull(sentHeartBeat);

                foreach (var kvp in sentHeartBeat.Properties)
                {
                    Assert.IsFalse(disableHbProps.Contains(kvp.Key), string.Format(CultureInfo.CurrentCulture, "Dissallowed field '{0}' found in payload", kvp.Key));
                }
            }
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
                hbeat.AddHealthProperty(testerKey, "test", false);
                hbeat.SimulateSend();
                Assert.IsTrue(hbeat.sentMessages.Any(a => a.Sum >= 1.0));
            }
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

                hbeat.AddHealthProperty("tester01", "test failure 1", false);
                hbeat.AddHealthProperty("tester02", "test failure 2", false);
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

                foreach (string field in HeartbeatDefaultPayload.DefaultFields)
                {
                    try
                    {
                        var fieldPayload = sentMsg.Properties.Single(a => string.Compare(a.Key, field) == 0);
                        Assert.IsNotNull(fieldPayload);
                        if (field.Equals(HeartbeatDefaultPayload.UpdatedFieldsPropertyKey, StringComparison.OrdinalIgnoreCase))
                        {
                            Assert.IsTrue(string.IsNullOrEmpty(fieldPayload.Value)); // updated fields should be empty for sdk default fields
                        }
                        else
                        {
                            Assert.IsFalse(string.IsNullOrEmpty(fieldPayload.Value)); // updated fields should not be empty
                        }

                    }
                    catch (Exception)
                    {
                        Assert.Fail(string.Format(CultureInfo.CurrentCulture, "The default field '{0}' is not present exactly once in a sent message.", field));
                    }
                }
            }
        }

        [TestMethod]
        public void PayloadExtensionHandlesExtensionPayloadNameCollision()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsTrue(hbeat.AddHealthProperty("test01", "some test value", true));
                Assert.IsFalse(hbeat.AddHealthProperty("test01", "some other test value", true));
            }
        }

        [TestMethod]
        public void CannotSetPayloadExtensionWithoutAddingItFirst()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsFalse(hbeat.SetHealthProperty("test01", "some other test value", true));
                Assert.IsTrue(hbeat.AddHealthProperty("test01", "some test value", true));
                Assert.IsTrue(hbeat.SetHealthProperty("test01", "some other test value", true));
            }
        }

        [TestMethod]
        public void CannotSetValueOfDefaultPayloadProperties()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                foreach (string key in HeartbeatDefaultPayload.DefaultFields)
                {
                    Assert.IsFalse(hbeat.SetHealthProperty(key, "test", true));
                }
            }
        }

        [TestMethod]
        public void CannotAddPayloadItemNamedOfDefaultPayloadProperties()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                foreach (string key in HeartbeatDefaultPayload.DefaultFields)
                {
                    Assert.IsFalse(hbeat.AddHealthProperty(key, "test", true));
                }
            }
        }

        [TestMethod]
        public void EnsureAllTargetFrameworksRepresented()
        {
            var defaultHeartbeatPayload = new HeartbeatDefaultPayload();
            var props = defaultHeartbeatPayload.GetPayloadProperties();
            foreach (var kvp in props)
            {
                if (kvp.Key.Equals("baseSdkTargetFramework", StringComparison.Ordinal))
                {
                    Assert.IsFalse(kvp.Value.PayloadValue.Equals("undefined", StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        [TestMethod]
        public void CanSetHealthHeartbeatPayloadValueWithoutHealthyFlag()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                string key = "setValueTest";

                Assert.IsTrue(hbeat.AddHealthProperty(key, "value01", true));
                Assert.IsTrue(hbeat.SetHealthProperty(key, "value02"));
                hbeat.SimulateSend();
                var messages = hbeat.sentMessages.First();
                Assert.IsNotNull(messages);
                Assert.IsTrue(messages.Properties.ContainsKey(key));
                Assert.IsTrue(messages.Properties[key].Equals("value02", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void CanSetHealthHeartbeatPayloadHealthIndicatorWithoutSettingValue()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                string key = "healthSettingTest";

                Assert.IsTrue(hbeat.AddHealthProperty(key, "value01", true));
                Assert.IsTrue(hbeat.SetHealthProperty(key, null, false));
                hbeat.SimulateSend();
                var messages = hbeat.sentMessages.First();
                Assert.IsNotNull(messages);
                Assert.IsTrue(messages.Properties.ContainsKey(key));
                Assert.IsTrue(messages.Properties[key].Equals("value01", StringComparison.Ordinal));
                Assert.IsTrue(messages.Sum == 1.0); // one false message in payload only
            }
        }

        [TestMethod]
        public void CanRemoveHeartbeatPayloadProperty()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                string key = "removePayloadItemTest";

                Assert.IsTrue(hbeat.AddHealthProperty(key, "value01", true));
                
                hbeat.SimulateSend();

                // ensure it is there the first time
                var msg = hbeat.sentMessages.First();
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.Properties.ContainsKey(key));

                // remove it
                hbeat.sentMessages.Clear();
                Assert.IsTrue(hbeat.RemoveHealthProperty(key));
                hbeat.SimulateSend();

                // ensure it is no longer there
                msg = hbeat.sentMessages.First();
                Assert.IsNotNull(msg);
                Assert.IsFalse(msg.Properties.ContainsKey(key));
            }
        }
    }
}
