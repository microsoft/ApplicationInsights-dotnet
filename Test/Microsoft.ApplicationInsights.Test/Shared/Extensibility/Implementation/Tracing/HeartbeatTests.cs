namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HeartbeatTests
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
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                Assert.IsTrue(hbeat.ExcludedHeartbeatProperties == null || hbeat.ExcludedHeartbeatProperties.Count() == 0);
                Assert.AreEqual(hbeat.HeartbeatInterval, HeartbeatProvider.DefaultHeartbeatInterval);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartbeatWithNonDefaultInterval()
        {
            TimeSpan userSetInterval = TimeSpan.FromMilliseconds(HeartbeatProvider.MinimumHeartbeatInterval.TotalMilliseconds + 7852.0);

            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                // set the interval via the IHeartbeatPropertyManager interface
                IHeartbeatPropertyManager hbeat = initializedModule;
                Assert.AreNotEqual(userSetInterval, hbeat.HeartbeatInterval);
                hbeat.HeartbeatInterval = userSetInterval;

                // initialize the DiagnosticsTelemetryModule, and check that the interval is still intact
                initializedModule.Initialize(new TelemetryConfiguration());
                Assert.AreEqual(userSetInterval, hbeat.HeartbeatInterval);
            }
        }

        [TestMethod]
        public void InitializeHealthHeartDisablingAzureMetadata()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                // disable azure metadata lookup via the IHeartbeatPropertyManager interface
                IHeartbeatPropertyManager hbeat = initializedModule;
                Assert.IsTrue(hbeat.EnableInstanceMetadata);
                hbeat.EnableInstanceMetadata = false;

                // initialize the DiagnosticsTelemetryModule, and ensure the instance metadata is still disabled
                initializedModule.Initialize(new TelemetryConfiguration());
                Assert.IsFalse(hbeat.EnableInstanceMetadata);
            }
        }

        [TestMethod]
        public void InitializeHeartbeatWithZeroIntervalIsSetToMinimum()
        {
            using (var hbeat = new HeartbeatProvider() { HeartbeatInterval = TimeSpan.FromMilliseconds(0) })
            {
                hbeat.Initialize(configuration: null);
                Assert.AreEqual(hbeat.HeartbeatInterval, HeartbeatProvider.MinimumHeartbeatInterval);
            }
        }

        [TestMethod]
        public void SetHeartbeatWithSmallerThanMinimumIntervalIsSetToMinimum()
        {
            var tooSmallInterval = TimeSpan.FromMilliseconds(HeartbeatProvider.MinimumHeartbeatInterval.TotalMilliseconds / 2);
            using (var hbeat = new HeartbeatProvider() { HeartbeatInterval = tooSmallInterval })
            {
                hbeat.Initialize(configuration: null);
                Assert.AreEqual(hbeat.HeartbeatInterval, HeartbeatProvider.MinimumHeartbeatInterval);
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
                    Assert.IsTrue(hbeat.AddHeartbeatProperty("test01", "this is a value", true));
                }
                catch (Exception e)
                {
                    Assert.Fail(string.Format(CultureInfo.CurrentCulture, "Registration of a heartbeat payload provider throws exception '{0}", e.ToInvariantString()));
                }
            }
        }

        [TestMethod]
        public void InitializationOfTelemetryClientDoesntResetHeartbeat()
        {
            TelemetryClient client = new TelemetryClient();

            bool origIsEnabled = true;
            bool origEnableMeta = true;
            TimeSpan origInterval = TimeSpan.MaxValue;
            TimeSpan setInterval = TimeSpan.MaxValue;

            foreach (var module in TelemetryModules.Instance.Modules)
            {
                if (module is IHeartbeatPropertyManager hbeatMan)
                {
                    origIsEnabled = hbeatMan.IsHeartbeatEnabled;
                    hbeatMan.IsHeartbeatEnabled = !origIsEnabled;

                    origEnableMeta = hbeatMan.EnableInstanceMetadata;
                    hbeatMan.EnableInstanceMetadata = !origEnableMeta;

                    hbeatMan.ExcludedHeartbeatProperties.Add("Test01");

                    origInterval = hbeatMan.HeartbeatInterval;
                    setInterval = origInterval + TimeSpan.FromMinutes(2);
                    hbeatMan.HeartbeatInterval = setInterval;
                }
            }

            TelemetryClient client2 = new TelemetryClient();

            foreach (var module in TelemetryModules.Instance.Modules)
            {
                if (module is IHeartbeatPropertyManager hbeatMan)
                {
                    Assert.AreNotEqual(hbeatMan.IsHeartbeatEnabled, origIsEnabled);
                    Assert.AreNotEqual(hbeatMan.EnableInstanceMetadata, origEnableMeta);
                    Assert.IsTrue(hbeatMan.ExcludedHeartbeatProperties.Contains("Test01"));
                    Assert.AreNotEqual(hbeatMan.HeartbeatInterval, origInterval);
                    Assert.AreEqual(hbeatMan.HeartbeatInterval, setInterval);
                }
            }

        }

        [TestMethod]
        public void IsHeartbeatEnabledByDefault()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                // test that the heartbeat is enabled by default
                IHeartbeatPropertyManager hbeat = initializedModule;
                Assert.IsTrue(hbeat.IsHeartbeatEnabled);

                // initialize the DiagnosticsTelemetryModule, and check that heartbeats are still enabled
                initializedModule.Initialize(new TelemetryConfiguration());
                Assert.IsTrue(hbeat.IsHeartbeatEnabled);
            }
        }

        [TestMethod]
        public void CanDisableHeartbeatPriorToInitialize()
        {
            using (var initializedModule = new DiagnosticsTelemetryModule())
            {
                // disable the heartbeat at construction time but before initialize
                // (this simulates the flow of disabling the heartbeat via config)
                IHeartbeatPropertyManager hbeat = initializedModule;
                hbeat.IsHeartbeatEnabled = false;

                // initialize the DiagnosticsTelemetryModule, and check that heartbeats are still disabled
                initializedModule.Initialize(new TelemetryConfiguration());
                Assert.IsFalse(hbeat.IsHeartbeatEnabled);

                // dig into the heartbeat provider itself to ensure this is indeed disabled
                Assert.IsFalse(initializedModule.HeartbeatProvider.IsHeartbeatEnabled);
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
        public void HeartbeatPayloadContainsDataByDefault()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                var hbeatPayloadData = hbeat.GatherData();
                Assert.IsNotNull(hbeatPayloadData);
            }
        }

        [TestMethod]
        public void HeartbeatPayloadContainsUserSpecifiedData()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                string testerKey = "tester123";
                Assert.IsTrue(hbeat.AddHeartbeatProperty(testerKey, "test", true));
                hbeat.Initialize(configuration: null);

                MetricTelemetry payload = (MetricTelemetry)hbeat.GatherData();

                Assert.IsTrue(payload.Properties.Any(
                    a => a.Key.Equals(testerKey, StringComparison.OrdinalIgnoreCase)),
                    "Provided custom payload provider to heartbeat but never received any messages with its content");
            }
        }

        [TestMethod]
        public void HeartbeatPayloadContainsOnlyAllowedDefaultPayloadFields()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                List<string> disableHbProps = new List<string>();

                for (int i = 0; i < HeartbeatDefaultPayload.DefaultFields.Length; ++i)
                {
                    if (i % 2 == 0)
                    {
                        disableHbProps.Add(HeartbeatDefaultPayload.DefaultFields[i]);
                        hbeat.ExcludedHeartbeatProperties.Add(HeartbeatDefaultPayload.DefaultFields[i]);
                    }
                }

                hbeat.Initialize(configuration: null);

                var sentHeartBeat = (MetricTelemetry)hbeat.GatherData();

                Assert.IsNotNull(sentHeartBeat);

                foreach (var kvp in sentHeartBeat.Properties)
                {
                    Assert.IsFalse(disableHbProps.Contains(kvp.Key), 
                        string.Format(CultureInfo.CurrentCulture, "Dissallowed field '{0}' found in payload", kvp.Key));
                }
            }
        }

        [TestMethod]
        public void HeartbeatMetricIsZeroForNoFailureConditionPresent()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsFalse(msg.Sum > 0.0);
            }
        }

        [TestMethod]
        public void HeartbeatMetricIsNonZeroWhenFailureConditionPresent()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);
                string testerKey = "tester123";
                hbeat.AddHeartbeatProperty(testerKey, "test", false);

                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsTrue(msg.Sum >= 1.0);
            }
        }

        [TestMethod]
        public void HeartbeatMetricCountAccountsForAllFailures()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsTrue(msg.Sum == 0.0);

                hbeat.AddHeartbeatProperty("tester01", "test failure 1", false);
                hbeat.AddHeartbeatProperty("tester02", "test failure 2", false);
                msg = (MetricTelemetry)hbeat.GatherData();

                Assert.IsTrue(msg.Sum == 2.0);
            }
        }

        [TestMethod]
        public void SentHeartbeatContainsExpectedDefaultFields()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                var msg = (MetricTelemetry)hbeat.GatherData();
                Assert.IsNotNull(msg);

                foreach (string field in HeartbeatDefaultPayload.DefaultFields)
                {
                    try
                    {
                        var fieldPayload = msg.Properties.Single(a => string.Compare(a.Key, field) == 0);
                        Assert.IsNotNull(fieldPayload);
                        Assert.IsFalse(string.IsNullOrEmpty(fieldPayload.Value));
                    }
                    catch (Exception)
                    {
                        Assert.Fail(string.Format(CultureInfo.CurrentCulture, "The default field '{0}' is not present exactly once in a sent message.", field));
                    }
                }
            }
        }

        [TestMethod]
        public void HeartbeatProviderDoesNotAllowDuplicatePropertyName()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsTrue(hbeat.AddHeartbeatProperty("test01", "some test value", true));
                Assert.IsFalse(hbeat.AddHeartbeatProperty("test01", "some other test value", true));
            }
        }

        [TestMethod]
        public void CannotSetPayloadExtensionWithoutAddingItFirst()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                Assert.IsFalse(hbeat.SetHeartbeatProperty("test01", "some other test value", true));
                Assert.IsTrue(hbeat.AddHeartbeatProperty("test01", "some test value", true));
                Assert.IsTrue(hbeat.SetHeartbeatProperty("test01", "some other test value", true));
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
                    Assert.IsFalse(hbeat.SetHeartbeatProperty(key, "test", true));
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
                    Assert.IsFalse(hbeat.AddHeartbeatProperty(key, "test", true));
                }
            }
        }

        [TestMethod]
        public void EnsureAllTargetFrameworksRepresented()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                var msg = (MetricTelemetry)hbeat.GatherData();

                Assert.IsTrue(msg.Properties.ContainsKey("baseSdkTargetFramework"));
                Assert.IsFalse(msg.Properties["baseSdkTargetFramework"].Equals("undefined", StringComparison.OrdinalIgnoreCase));
            }
        }

        [TestMethod]
        public void CanSetHealthHeartbeatPayloadValueWithoutHealthyFlag()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                string key = "setValueTest";

                Assert.IsTrue(hbeat.AddHeartbeatProperty(key, "value01", true));
                Assert.IsTrue(hbeat.SetHeartbeatProperty(key, "value02"));
                var msg = (MetricTelemetry)hbeat.GatherData();
                
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.Properties.ContainsKey(key));
                Assert.IsTrue(msg.Properties[key].Equals("value02", StringComparison.Ordinal));
            }
        }

        [TestMethod]
        public void CanSetHealthHeartbeatPayloadHealthIndicatorWithoutSettingValue()
        {
            using (var hbeat = new HeartbeatProvider())
            {
                hbeat.Initialize(configuration: null);

                string key = "healthSettingTest";

                Assert.IsTrue(hbeat.AddHeartbeatProperty(key, "value01", true));
                Assert.IsTrue(hbeat.SetHeartbeatProperty(key, null, false));
                var msg = (MetricTelemetry)hbeat.GatherData();
                
                Assert.IsNotNull(msg);
                Assert.IsTrue(msg.Properties.ContainsKey(key));
                Assert.IsTrue(msg.Properties[key].Equals("value01", StringComparison.Ordinal));
                Assert.IsTrue(msg.Sum == 1.0); // one false message in payload only
            }
        }

        private class TestRequestorStub : IAzureMetadataRequestor
        {
            private Func<IEnumerable<string>> GetAllFieldsFunc = null;
            private Func<string, string> GetSingleFieldFunc = null;

            public TestRequestorStub(Func<IEnumerable<string>> getAllFields = null, Func<string,string> getSingleFieldFunc = null)
            {
                this.GetAllFieldsFunc = getAllFields;
                if (getAllFields == null)
                {
                    this.GetAllFieldsFunc = this.GetAllFields;
                }

                this.GetSingleFieldFunc = getSingleFieldFunc;
                if (getSingleFieldFunc == null)
                {
                    this.GetSingleFieldFunc = this.GetSingleField;
                }
            }

            public Dictionary<string, string> computeFields = new Dictionary<string, string>();

            public Task<string> GetAzureComputeMetadata(string fieldName)
            {
                return Task.FromResult(this.GetSingleFieldFunc(fieldName));
            }

            private string GetSingleField(string fieldName)
            {
                if (this.computeFields.ContainsKey(fieldName))
                {
                    return this.computeFields[fieldName];
                }

                return string.Empty;
            }

            public Task<IEnumerable<string>> GetAzureInstanceMetadataComputeFields()
            {
                return Task.FromResult(this.GetAllFieldsFunc());
            }

            private IEnumerable<string> GetAllFields()
            {
                IEnumerable<string> fields = this.computeFields.Keys.ToArray();
                return fields;
            }

        }

        private class TestHeartbeatProviderStub : IHeartbeatProvider
        {
            public Dictionary<string, HeartbeatPropertyPayload> HeartbeatProperties = new Dictionary<string, HeartbeatPropertyPayload>();
            public List<string> ExcludedPropertyFields = new List<string>();

            public string InstrumentationKey { get; set; }

            public bool IsHeartbeatEnabled { get; set; }

            public bool EnableInstanceMetadata { get; set; }

            public TimeSpan HeartbeatInterval { get; set; }

            public IList<string> ExcludedHeartbeatProperties { get => this.ExcludedPropertyFields; }

            public TestHeartbeatProviderStub()
            {
                this.InstrumentationKey = Guid.NewGuid().ToString();
                this.IsHeartbeatEnabled = true;
                this.EnableInstanceMetadata = true;
                this.HeartbeatInterval = TimeSpan.FromSeconds(31);
            }

            public bool AddHeartbeatProperty(string propertyName, bool overrideDefaultField, string propertyValue, bool isHealthy)
            {
                this.HeartbeatProperties.Add(
                    propertyName, 
                    new HeartbeatPropertyPayload()
                    {
                        IsHealthy = isHealthy,
                        IsUpdated = true,
                        PayloadValue = propertyValue
                    });

                return true;
            }

            public void Dispose()
            {
            }

            public void Initialize(TelemetryConfiguration configuration)
            {
            }

            public bool SetHeartbeatProperty(string propertyName, bool overrideDefaultField, string propertyValue = null, bool? isHealthy = null)
            {
                if (this.HeartbeatProperties.ContainsKey(propertyName))
                {
                    HeartbeatPropertyPayload pl = this.HeartbeatProperties[propertyName];
                    pl.IsHealthy = isHealthy.GetValueOrDefault(pl.IsHealthy);
                    pl.PayloadValue = propertyValue ?? pl.PayloadValue;
                    pl.IsUpdated = true;

                    return true;
                }
                return false;
            }
        }

        [TestMethod]
        public void GetAzureInstanceMetadataFieldsAsExpected()
        {
            using (var hbeat = new TestHeartbeatProviderStub())
            {
                TestRequestorStub myRequestor = new TestRequestorStub();
                int counter = 1;
                foreach (string field in HeartbeatDefaultPayload.DefaultOptionalFields)
                {
                    myRequestor.computeFields.Add(field, $"testValue{counter++}");
                }

                HeartbeatDefaultPayload.PopulateDefaultPayload(new string[] { }, hbeat, myRequestor);
                // this is an async call, and will most defintely finish after the next call. Instead
                // of just doing a timeout and allowing for uncertainty in tests, let's be a bit more
                
                foreach (string fieldName in HeartbeatDefaultPayload.DefaultOptionalFields)
                {
                    Assert.IsTrue(hbeat.HeartbeatProperties.ContainsKey(fieldName));
                }
            }
        }

        [TestMethod]
        public void FailToObtainAzureInstanceMetadataFieldsAltogether()
        {
            using (var hbeat = new TestHeartbeatProviderStub())
            {
                TestRequestorStub myRequestor = new TestRequestorStub();

                HeartbeatDefaultPayload.PopulateDefaultPayload(new string[] { }, hbeat, myRequestor);

                foreach (string fieldName in HeartbeatDefaultPayload.DefaultOptionalFields)
                {
                    Assert.IsFalse(hbeat.HeartbeatProperties.ContainsKey(fieldName));
                }
            }
        }
    }
}
