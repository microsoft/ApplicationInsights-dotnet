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
            using (var hbeat = new HealthHeartbeatProviderMock())
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
                Assert.AreEqual(nonDefaultInterval, hbeat.HeartbeatInterval);
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
                hbeat.Initialize(configuration: new TelemetryConfiguration());

                try
                {
                    Assert.IsTrue(hbeat.AddHealthProperty(new HealthHeartbeatProperty("test01", "this is a value", true)));
                    Assert.IsTrue(hbeat.AddHealthProperty(new HealthHeartbeatProperty("test02", DateTime.Now, true)));
                    Assert.IsTrue(hbeat.AddHealthProperty(new HealthHeartbeatProperty("test03", 245.678, true)));
                    Assert.IsTrue(hbeat.AddHealthProperty(new HealthHeartbeatProperty("test04", new List<string>() { "one", "two", "three" }, true)));
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
        }

        [TestMethod]
        public void HeartbeatPayloadContainsUserSpecifiedData()
        {
            using (var hbeat = new HealthHeartbeatProviderMock())
            {
                hbeat.Initialize(configuration: null);
                string testerKey = "tester123";
                hbeat.AddHealthProperty(new HealthHeartbeatProperty(testerKey, "test", true));
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
            for (int i = 0; i < HealthHeartbeatDefaultPayload.DefaultFields.Length; ++i)
            {
                if (i % 2 == 0)
                {
                    disableHbProps.Add(HealthHeartbeatDefaultPayload.DefaultFields[i]);
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
        [Ignore("I don't know how to modify the config file during tests yet")]
        public void HeartbeatPayloadContainsFieldsSpecifiedInConfig()
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
                hbeat.AddHealthProperty(new HealthHeartbeatProperty(testerKey, "test", false));
                hbeat.SimulateSend();
                Assert.IsTrue(hbeat.sentMessages.Any(a => a.Sum >= 1.0));
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

                hbeat.AddHealthProperty(new HealthHeartbeatProperty("tester01", "test failure 1", false));
                hbeat.AddHealthProperty(new HealthHeartbeatProperty("tester02", "test failure 2", false));
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
                        var fieldPayload = sentMsg.Properties.Single(a => string.Compare(a.Key, field) == 0);
                        Assert.IsNotNull(fieldPayload);
                        if (field.Equals(HealthHeartbeatDefaultPayload.UpdatedFieldsPropertyKey, StringComparison.OrdinalIgnoreCase))
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
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsTrue(hbeat.AddHealthProperty(new HealthHeartbeatProperty("test01", "some test value", true)));
                Assert.IsFalse(hbeat.AddHealthProperty(new HealthHeartbeatProperty("test01", "some other test value", true)));
            }
        }

        [TestMethod]
        public void CannotSetPayloadExtensionWithoutAddingItFirst()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsFalse(hbeat.SetHealthProperty(new HealthHeartbeatProperty("test01", "some other test value", true)));
                Assert.IsTrue(hbeat.AddHealthProperty(new HealthHeartbeatProperty("test01", "some test value", true)));
                Assert.IsTrue(hbeat.SetHealthProperty(new HealthHeartbeatProperty("test01", "some other test value", true)));
            }
        }

        [TestMethod]
        public void CannotSetValueOfDefaultPayloadProperties()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                foreach (string key in HealthHeartbeatDefaultPayload.DefaultFields)
                {
                    var prop = new HealthHeartbeatProperty(key, "test", true);
                    Assert.IsFalse(hbeat.SetHealthProperty(prop));
                }
            }
        }

        [TestMethod]
        public void CannotAddPayloadItemNamedOfDefaultPayloadProperties()
        {
            using (var hbeat = new HealthHeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                foreach (string key in HealthHeartbeatDefaultPayload.DefaultFields)
                {
                    var prop = new HealthHeartbeatProperty(key, "test", true);
                    Assert.IsFalse(hbeat.AddHealthProperty(prop));
                }
            }
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
