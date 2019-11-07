namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Metrics;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AutocollectedMetricsExtractorTest
    {
        #region General Tests

        [TestMethod]
        public void CanConstruct()
        {
            var extractor = new AutocollectedMetricsExtractor(null);
        }

        [TestMethod]
        public void DisposeIsIdempotent()
        {
            AutocollectedMetricsExtractor extractor = null;

            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory =
                    (nextProc) =>
                    {
                        extractor = new AutocollectedMetricsExtractor(nextProc);
                        return extractor;
                    };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                ;
            }

            extractor.Dispose();
            extractor.Dispose();
        }

        #endregion General Tests

        #region Request-metrics-related Tests

        [TestMethod]
        public void Request_TelemetryMarkedAsProcessedCorrectly()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc) => new AutocollectedMetricsExtractor(nextProc);

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);
                client.TrackEvent("Test Event");
                client.TrackRequest("Test Request 1", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", success: true);
                client.TrackRequest("Test Request 2", DateTimeOffset.Now, TimeSpan.FromMilliseconds(11), "200", success: true);
            }

            Assert.AreEqual(4, telemetrySentToChannel.Count);

            AssertEx.IsType<EventTelemetry>(telemetrySentToChannel[0]);
            Assert.AreEqual("Test Event", ((EventTelemetry) telemetrySentToChannel[0]).Name);
            Assert.AreEqual(false, ((EventTelemetry) telemetrySentToChannel[0]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));

            AssertEx.IsType<RequestTelemetry>(telemetrySentToChannel[1]);
            Assert.AreEqual("Test Request 1", ((RequestTelemetry) telemetrySentToChannel[1]).Name);
            Assert.AreEqual(true, ((RequestTelemetry) telemetrySentToChannel[1]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));
            Assert.AreEqual("(Name:'Requests', Ver:'1.1')",
                         ((RequestTelemetry) telemetrySentToChannel[1]).Properties["_MS.ProcessedByMetricExtractors"]);

            AssertEx.IsType<RequestTelemetry>(telemetrySentToChannel[2]);
            Assert.AreEqual("Test Request 2", ((RequestTelemetry) telemetrySentToChannel[2]).Name);
            Assert.AreEqual(true, ((RequestTelemetry) telemetrySentToChannel[2]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));
            Assert.AreEqual("(Name:'Requests', Ver:'1.1')",
                         ((RequestTelemetry) telemetrySentToChannel[2]).Properties["_MS.ProcessedByMetricExtractors"]);

            AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[3]);            
        }

        [TestMethod]
        public void Request_CorrectlyExtractsMetric()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc) => new AutocollectedMetricsExtractor(nextProc);

            // default set of dimensions with test values.
            bool[] success = new bool[] { true, false };
            bool[] synthetic = new bool[] { true, false };
            string[] responseCode = new string[] { "200", "500", "401" };
            string[] cloudRoleNames = new string[] { "RoleA", "RoleB" };
            string[] cloudRoleInstances = new string[] { "RoleInstanceA", "RoleInstanceB" };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);
                List<RequestTelemetry> requests = new List<RequestTelemetry>();

                // Produces telemetry with every combination of dimension values.
                for(int i = 0; i < success.Length; i++)
                {
                    for (int j = 0; j < responseCode.Length; j++)
                    {
                        for (int k = 0; k < cloudRoleNames.Length; k++)
                        {
                            for (int l = 0; l < cloudRoleInstances.Length; l++)
                            {
                                for (int m = 0; m < synthetic.Length; m++)
                                {
                                    // For ease of validation 3 calls are tracked.
                                    // with 10, 30, 5 leading to min=5, max=30, sum = 45
                                    // ValidateAllMetric() method does this validation
                                    requests.Add(CreateRequestTelemetry(
                                        TimeSpan.FromMilliseconds(10), responseCode[j], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                    requests.Add(CreateRequestTelemetry(
                                        TimeSpan.FromMilliseconds(30), responseCode[j], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                    requests.Add(CreateRequestTelemetry(
                                        TimeSpan.FromMilliseconds(5), responseCode[j], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                }
                            }
                        }
                    }
                }

                foreach(var req in requests)
                {
                    client.TrackRequest(req);
                }

                // 2 * 2 * 3 * 2 * 2 = 48
                // success * synthetic * responseCode * RoleName * RoleInstance
                // 3 Track calls are made in every iteration,
                // hence 48 * 3 requests gives 144 total requests
                Assert.AreEqual(144, telemetrySentToChannel.Count);

                // The above did not include Metrics as they are sent upon dispose only.                
            } // dispose occurs here, and hence metrics get flushed out.

            // 192 = 144 requests + 48 metrics as there are 48 unique combination of dimension
            Assert.AreEqual(192, telemetrySentToChannel.Count);

            // These are pre-agg metric
            var serverResponseMetric = telemetrySentToChannel.Where(
                (tel) => "Server response time".Equals((tel as MetricTelemetry)?.Name));
            Assert.AreEqual(48, serverResponseMetric.Count());

            foreach(var metric in serverResponseMetric)
            {
                var metricTel = metric as MetricTelemetry;
                // validate standard fields
                Assert.IsTrue(metricTel.Properties.ContainsKey("_MS.AggregationIntervalMs"));
                Assert.IsTrue(metricTel.Context.GlobalProperties.ContainsKey("_MS.IsAutocollected"));
                Assert.AreEqual("True", metricTel.Context.GlobalProperties["_MS.IsAutocollected"]);
                Assert.IsTrue(metricTel.Context.GlobalProperties.ContainsKey("_MS.MetricId"));
                Assert.AreEqual("requests/duration", metricTel.Context.GlobalProperties["_MS.MetricId"]);
                
                // validate dimensions exist
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("Request.Success"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("cloud/roleInstance"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("cloud/roleName"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("request/resultCode"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("operation/synthetic"));
            }

            // Validate success dimension
            for (int i = 0; i < success.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["Request.Success"] == success[i].ToString());
                int expectedCount = 48 / success.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate synthetic dimension
            for (int i = 0; i < success.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["operation/synthetic"] == synthetic[i].ToString());
                int expectedCount = 48 / synthetic.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate RoleName dimesion
            for (int i = 0; i < cloudRoleNames.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["cloud/roleName"] == cloudRoleNames[i]);
                int expectedCount = 48 / cloudRoleNames.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate RoleInstance dimension
            for (int i = 0; i < cloudRoleInstances.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["cloud/roleInstance"] == cloudRoleInstances[i]);
                int expectedCount = 48 / cloudRoleInstances.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate ResponseCode dimension
            for (int i = 0; i < responseCode.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["request/resultCode"] == responseCode[i]);
                int expectedCount = 48 / responseCode.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }
        }

        private RequestTelemetry CreateRequestTelemetry(TimeSpan duration, string resp, bool success, bool synthetic,
            string role, string instance)
        {
            var req = new RequestTelemetry("Req1", DateTimeOffset.Now, duration, resp, success);
            req.Context.Cloud.RoleName = role;
            req.Context.Cloud.RoleInstance = instance;
            if (synthetic)
            {
                req.Context.Operation.SyntheticSource = "synthetic";
            }

            return req;
        }

        private void ValidateAllMetric(IEnumerable<ITelemetry> metricCollection)
        {
            foreach (var singleMetric in metricCollection)
            {
                var m = singleMetric as MetricTelemetry;
                Assert.AreEqual(3, m.Count);
                Assert.AreEqual(5, m.Min);
                Assert.AreEqual(30, m.Max);
                Assert.AreEqual(45, m.Sum);
            }
        }

        [TestMethod]
        public void Request_CorrectlyWorksWithResponseSuccess()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc) => new AutocollectedMetricsExtractor(nextProc);

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackRequest(new RequestTelemetry()
                                    {
                                        Name = "Test Request 1",
                                        Timestamp = DateTimeOffset.Now,
                                        Duration = TimeSpan.FromMilliseconds(5),
                                        ResponseCode = "xxx",
                                        Success = true
                                    });

                client.TrackRequest(new RequestTelemetry()
                                    {
                                        Name = "Test Request 2",
                                        Timestamp = DateTimeOffset.Now,
                                        Duration = TimeSpan.FromMilliseconds(10),
                                        ResponseCode = "xxx",
                                        Success = false
                                    });

                client.TrackRequest(new RequestTelemetry()
                                    {
                                        Name = "Test Request 3",
                                        Timestamp = DateTimeOffset.Now,
                                        Duration = TimeSpan.FromMilliseconds(15),
                                        ResponseCode = "xxx",
                                        Success = null
                                    });
            }

            Assert.AreEqual(5, telemetrySentToChannel.Count);

            var t = new SortedList<string, MetricTelemetry>();

            Assert.IsNotNull(telemetrySentToChannel[3]);
            AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[3]);
            var m = (MetricTelemetry)telemetrySentToChannel[3];
            t.Add(m.Properties["Request.Success"], m);

            Assert.IsNotNull(telemetrySentToChannel[4]);
            AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[4]);
            m = (MetricTelemetry)telemetrySentToChannel[4];
            t.Add(m.Properties["Request.Success"], m);

            var metricF = t.Values[0];
            var metricT = t.Values[1];

            Assert.AreEqual("Server response time", metricT.Name);
            Assert.AreEqual(2, metricT.Count);
            Assert.AreEqual(15, metricT.Max);
            Assert.AreEqual(5, metricT.Min);
            Assert.AreEqual(20, metricT.Sum);
            Assert.AreEqual(true, metricT.Properties.ContainsKey("Request.Success"));
            Assert.AreEqual(Boolean.TrueString, metricT.Properties["Request.Success"]);

            Assert.AreEqual("Server response time", metricF.Name);
            Assert.AreEqual(1, metricF.Count);
            Assert.AreEqual(10, metricF.Max);
            Assert.AreEqual(10, metricF.Min);
            Assert.AreEqual(10, metricF.Sum);
            Assert.AreEqual(true, metricF.Properties.ContainsKey("Request.Success"));
            Assert.AreEqual(Boolean.FalseString, metricF.Properties["Request.Success"]);
        }

        #endregion Request-metrics-related Tests

        #region Dependency-metrics-related Tests

        [TestMethod]
        public void Dependency_MaxDependenctTypesToDiscoverDefaultIsAsExpected()
        {
            Assert.AreEqual(15, DependencyMetricsExtractor.MaxDependencyTypesToDiscoverDefault);
        }

        [TestMethod]
        public void Dependency_TelemetryMarkedAsProcessedCorrectly()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc) => new AutocollectedMetricsExtractor(nextProc) { MaxDependencyTypesToDiscover = 0 };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);
                
                client.TrackRequest("Test Request", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "200", success: true);
                client.TrackDependency("Type", "Target", "depName1", "data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1000), "ResultCode100", true);
                client.TrackDependency("Type", "Target", "depName2", "data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1000), "ResultCode100", true);
                client.TrackDependency("Type", "Target", "depName3", "data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1000), "ResultCode100", true);
                client.TrackEvent("Test Event");
            }

            Assert.AreEqual(7, telemetrySentToChannel.Count);
            
            AssertEx.IsType<RequestTelemetry>(telemetrySentToChannel[0]);
            Assert.AreEqual(true, ((RequestTelemetry) telemetrySentToChannel[0]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));
            Assert.AreEqual("(Name:'Requests', Ver:'1.1')",
                         ((RequestTelemetry) telemetrySentToChannel[0]).Properties["_MS.ProcessedByMetricExtractors"]);

            AssertEx.IsType<DependencyTelemetry>(telemetrySentToChannel[1]);
            Assert.AreEqual("depName1", ((DependencyTelemetry) telemetrySentToChannel[1]).Name);
            Assert.AreEqual(true, ((DependencyTelemetry) telemetrySentToChannel[1]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));
            Assert.AreEqual("(Name:'Dependencies', Ver:'1.1')",
                         ((DependencyTelemetry) telemetrySentToChannel[1]).Properties["_MS.ProcessedByMetricExtractors"]);

            AssertEx.IsType<DependencyTelemetry>(telemetrySentToChannel[2]);
            Assert.AreEqual("depName2", ((DependencyTelemetry)telemetrySentToChannel[2]).Name);
            Assert.AreEqual(true, ((DependencyTelemetry)telemetrySentToChannel[2]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));
            Assert.AreEqual("(Name:'Dependencies', Ver:'1.1')",
                         ((DependencyTelemetry)telemetrySentToChannel[2]).Properties["_MS.ProcessedByMetricExtractors"]);

            AssertEx.IsType<DependencyTelemetry>(telemetrySentToChannel[3]);
            Assert.AreEqual("depName3", ((DependencyTelemetry) telemetrySentToChannel[3]).Name);
            Assert.AreEqual(true, ((DependencyTelemetry) telemetrySentToChannel[3]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));
            Assert.AreEqual("(Name:'Dependencies', Ver:'1.1')",
                         ((DependencyTelemetry) telemetrySentToChannel[3]).Properties["_MS.ProcessedByMetricExtractors"]);

            AssertEx.IsType<EventTelemetry>(telemetrySentToChannel[4]);
            Assert.AreEqual("Test Event", ((EventTelemetry) telemetrySentToChannel[4]).Name);
            Assert.AreEqual(false, ((EventTelemetry) telemetrySentToChannel[4]).Properties.ContainsKey("_MS.ProcessedByMetricExtractors"));


            AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[5]);
            AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[6]);

            Assert.AreEqual(1, telemetrySentToChannel.Where( (t) => "Server response time".Equals((t as MetricTelemetry)?.Name) ).Count());
            Assert.AreEqual(1, telemetrySentToChannel.Where( (t) => "Dependency duration".Equals((t as MetricTelemetry)?.Name) ).Count());
        }

        [TestMethod]
        public void Dependency_CanSetMaxDependencyTypesToDiscoverBeforeInitialization()
        {
            var extractor = new AutocollectedMetricsExtractor(null);

            Assert.AreEqual(DependencyMetricsExtractor.MaxDependencyTypesToDiscoverDefault, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 1000;
            Assert.AreEqual(1000, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 5;
            Assert.AreEqual(5, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 1;
            Assert.AreEqual(1, extractor.MaxDependencyTypesToDiscover);

            extractor.MaxDependencyTypesToDiscover = 0;
            Assert.AreEqual(0, extractor.MaxDependencyTypesToDiscover);

            try
            {
                extractor.MaxDependencyTypesToDiscover = -1;
                Assert.IsTrue(false, "An ArgumentOutOfRangeException was expected");
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        [TestMethod]
        public void Dependency_CanSetMaxDependencyTypesToDiscoverAfterInitialization()
        {
            AutocollectedMetricsExtractor extractor = null;

            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc)
                                                                                                =>
                                                                                                {
                                                                                                    extractor = new AutocollectedMetricsExtractor(nextProc)
                                                                                                            {
                                                                                                                MaxDependencyTypesToDiscover = 0
                                                                                                            };
                                                                                                    return extractor;
                                                                                                };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {

                Assert.AreEqual(0, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 1000;
                Assert.AreEqual(1000, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 5;
                Assert.AreEqual(5, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 1;
                Assert.AreEqual(1, extractor.MaxDependencyTypesToDiscover);

                extractor.MaxDependencyTypesToDiscover = 0;
                Assert.AreEqual(0, extractor.MaxDependencyTypesToDiscover);

                try
                {
                    extractor.MaxDependencyTypesToDiscover = -1;
                    Assert.IsTrue(false, "An ArgumentOutOfRangeException was expected");
                }
                catch (ArgumentOutOfRangeException)
                {
                }
            }
        }

        [TestMethod]
        public void Dependency_CorrectlyExtractsMetricWhenGroupingByTypeDisabled()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc) => new AutocollectedMetricsExtractor(nextProc) 
            { MaxDependencyTypesToDiscover = 0 };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent("Test Event 1");

                client.TrackDependency("Test Dependency Type A", "Test Target 1", "Test Dependency Call 1", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(5), "201", success: true);
                client.TrackDependency("Test Dependency Type B", "Test Target 2", "Test Dependency Call 2", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "202", success: true);
                client.TrackDependency("Test Dependency Type A", "Test Target 3", "Test Dependency Call 3", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(15), "203", success: true);
                client.TrackDependency(null,                     "Test Target 4", "Test Dependency Call 4", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(20), "204", success: true);

                client.TrackDependency("Test Dependency Type A", "Test Target 5", "Test Dependency Call 5", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(50), "501", success: false);
                client.TrackDependency("Test Dependency Type B", "Test Target 6", "Test Dependency Call 6", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(100), "502", success: false);
                client.TrackDependency("", "                      Test Target 7", "Test Dependency Call 7", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(150), "504", success: false);
            }

            Assert.AreEqual(10, telemetrySentToChannel.Count);

            var t = new SortedList<string, MetricTelemetry>();

            Assert.IsNotNull(telemetrySentToChannel[8]);
            AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[8]);
            var m = (MetricTelemetry) telemetrySentToChannel[8];
            t.Add(m.Properties["Dependency.Success"], m);

            Assert.IsNotNull(telemetrySentToChannel[9]);
            AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[9]);
            m = (MetricTelemetry)telemetrySentToChannel[9];
            t.Add(m.Properties["Dependency.Success"], m);

            var metricF = t.Values[0];
            var metricT = t.Values[1];

            Assert.AreEqual("Dependency duration", metricT.Name);
            Assert.AreEqual(4, metricT.Count);
            Assert.AreEqual(20, metricT.Max);
            Assert.AreEqual(5, metricT.Min);
            Assert.AreEqual(true, Math.Abs(metricT.StandardDeviation.Value - 5.590169943749474) < 0.0000001);
            Assert.AreEqual(50, metricT.Sum);

            Assert.AreEqual(5, metricT.Properties.Count);
            Assert.IsTrue(metricT.Properties.ContainsKey("_MS.AggregationIntervalMs"));
            Assert.IsTrue(metricT.Properties.ContainsKey("_MS.IsAutocollected"));
            Assert.AreEqual("True", metricT.Properties["_MS.IsAutocollected"]);
            Assert.IsTrue(metricT.Properties.ContainsKey("_MS.MetricId"));
            Assert.AreEqual("dependencies/duration", metricT.Properties["_MS.MetricId"]);
            Assert.AreEqual(true, metricT.Properties.ContainsKey("Dependency.Success"));
            Assert.AreEqual(Boolean.TrueString, metricT.Properties["Dependency.Success"]);
            Assert.AreEqual(true, metricT.Properties.ContainsKey("Dependency.Type"));
            Assert.AreEqual("Other", metricT.Properties["Dependency.Type"]);

            Assert.AreEqual("Dependency duration", metricF.Name);
            Assert.AreEqual(3, metricF.Count);
            Assert.AreEqual(150, metricF.Max);
            Assert.AreEqual(50, metricF.Min);
            Assert.AreEqual(true, Math.Abs(metricF.StandardDeviation.Value - 40.8248290) < 0.0000001);
            Assert.AreEqual(300, metricF.Sum);

            Assert.AreEqual(5, metricF.Properties.Count);
            Assert.IsTrue(metricF.Properties.ContainsKey("_MS.AggregationIntervalMs"));
            Assert.IsTrue(metricF.Properties.ContainsKey("_MS.IsAutocollected"));
            Assert.AreEqual("True", metricF.Properties["_MS.IsAutocollected"]);
            Assert.IsTrue(metricF.Properties.ContainsKey("_MS.MetricId"));
            Assert.AreEqual("dependencies/duration", metricF.Properties["_MS.MetricId"]);
            Assert.AreEqual(true, metricF.Properties.ContainsKey("Dependency.Success"));
            Assert.AreEqual(Boolean.FalseString, metricF.Properties["Dependency.Success"]);
            Assert.AreEqual(true, metricT.Properties.ContainsKey("Dependency.Type"));
            Assert.AreEqual("Other", metricT.Properties["Dependency.Type"]);
        }

        [TestMethod]
        public void Dependency_CorrectlyExtractsMetricWhenGroupingByTypeEnabled()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc) => new AutocollectedMetricsExtractor(nextProc) { MaxDependencyTypesToDiscover = 3 };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);

                client.TrackEvent("Test Event");

                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(5), "201", success: true);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(10), "202", success: true);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(15), "203", success: true);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(20), "204", success: true);

                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(105), "201", success: true);
                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(110), "202", success: true);
                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(115), "203", success: true);
                client.TrackDependency("Test Type B", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(120), "204", success: true);

                client.TrackDependency("", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(305), "201", success: true);
                client.TrackDependency(null, "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(310), "202", success: true);
                client.TrackDependency("", "Test Dependency Call", "Test Command Name", DateTimeOffset.Now, TimeSpan.FromMilliseconds(315), success: true);

                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1070), "501", success: false);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1180), "502", success: false);

                client.TrackEvent("Another Test Event");

                client.TrackDependency("Test Type C", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(2042), "501", success: false);
                client.TrackDependency("Test Type C", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(2107), "502", success: false);
                client.TrackDependency("Test Type C", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(2158), "502", success: false);

                client.TrackDependency("Test Type D", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(565), "201", success: true);
                client.TrackDependency("Test Type D", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(573), "202", success: true);

                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1070), "501", success: false);
                client.TrackDependency("Test Type A", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(1180), "502", success: false);

                client.TrackDependency("Test Type E", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(605), "201", success: true);

                client.TrackDependency("Test Type E", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(3010), "202", success: false);

                client.TrackDependency("", "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(4062), "201", success: false);
                client.TrackDependency(null, "Test Target", "Test Dependency Call", "Test Data", DateTimeOffset.Now, TimeSpan.FromMilliseconds(4012), "202", success: false);
                client.TrackDependency("", "Test Dependency Call", "Test Command Name", DateTimeOffset.Now, TimeSpan.FromMilliseconds(4039), success: false);
            }

            //// The following metric documents are expected:
            ////   - Format: number) Type, Success: Count
            //// 
            //// 1)  A, true: 4
            //// 2)  B, true: 4
            //// 3)  Unknown, true: 3
            //// 4)  A, false: 2
            //// 5)  C, false: 3
            //// 6)  D, true ==> Other, true: 2
            //// 4)* A, false: +2 = 4
            //// 6)* E, true ==> Other, true: +1 = 3
            //// 7)  E, false ==> Other, false: 1
            //// 8)  Unknown, false: 3

            Assert.AreEqual(27 + 8, telemetrySentToChannel.Count);
            for (int i = 0; i < 35; i++)
            {
                Assert.IsNotNull(telemetrySentToChannel[i]);
                if (i == 0 || i == 14)
                {
                    AssertEx.IsType<EventTelemetry>(telemetrySentToChannel[i]);
                }
                else if (i <= 26)
                {
                    AssertEx.IsType<DependencyTelemetry>(telemetrySentToChannel[i]);
                }
                else
                {
                    AssertEx.IsType<MetricTelemetry>(telemetrySentToChannel[i]);
                    MetricTelemetry metric = (MetricTelemetry) telemetrySentToChannel[i];

                    Assert.AreEqual("Dependency duration", metric.Name);
                    Assert.IsNotNull(metric.Properties);
                    Assert.IsTrue(metric.Properties.ContainsKey("Dependency.Type"));
                    Assert.IsTrue(metric.Properties.ContainsKey("_MS.AggregationIntervalMs"));
                    Assert.IsTrue(metric.Properties.ContainsKey("_MS.IsAutocollected"));
                    Assert.AreEqual("True", metric.Properties["_MS.IsAutocollected"]);
                    Assert.IsTrue(metric.Properties.ContainsKey("_MS.MetricId"));
                    Assert.AreEqual("dependencies/duration", metric.Properties["_MS.MetricId"]);
                    Assert.IsTrue(metric.Properties.ContainsKey("Dependency.Success"));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(metric.Properties["Dependency.Success"]));
                }
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Test Type A".Equals(p["Dependency.Type"]) && "True".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(4, metric.Count);
                Assert.AreEqual(20, metric.Max);
                Assert.AreEqual(5, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 5.590169943749474) < 0.0000001);
                Assert.AreEqual(50, metric.Sum);
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Test Type B".Equals(p["Dependency.Type"]) && "True".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(4, metric.Count);
                Assert.AreEqual(120, metric.Max);
                Assert.AreEqual(105, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 5.590169943749474) < 0.0000001);
                Assert.AreEqual(450, metric.Sum);
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Unknown".Equals(p["Dependency.Type"]) && "True".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(3, metric.Count);
                Assert.AreEqual(315, metric.Max);
                Assert.AreEqual(305, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 4.082482905) < 0.0000001);
                Assert.AreEqual(930, metric.Sum);
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Test Type A".Equals(p["Dependency.Type"]) && "False".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(4, metric.Count);
                Assert.AreEqual(1180, metric.Max);
                Assert.AreEqual(1070, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 55) < 0.0000001);
                Assert.AreEqual(4500, metric.Sum);
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Test Type C".Equals(p["Dependency.Type"]) && "False".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(3, metric.Count);
                Assert.AreEqual(2158, metric.Max);
                Assert.AreEqual(2042, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 47.47162895) < 0.0000001);
                Assert.AreEqual(6307, metric.Sum);
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Other".Equals(p["Dependency.Type"]) && "True".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(3, metric.Count);
                Assert.AreEqual(605, metric.Max);
                Assert.AreEqual(565, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 17.2819752) < 0.0000001);
                Assert.AreEqual(1743, metric.Sum);
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Other".Equals(p["Dependency.Type"]) && "False".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(1, metric.Count);
                Assert.AreEqual(3010, metric.Max);
                Assert.AreEqual(3010, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 0) < 0.0000001);
                Assert.AreEqual(3010, metric.Sum);
            }

            {
                IEnumerable<ITelemetry> metrics = telemetrySentToChannel.Where(
                            (t)
                            =>
                            {
                                IDictionary<string, string> p = (t as MetricTelemetry)?.Properties;
                                return (p != null) && "Unknown".Equals(p["Dependency.Type"]) && "False".Equals(p["Dependency.Success"]);
                            });
                Assert.AreEqual(1, metrics.Count());
                MetricTelemetry metric = (MetricTelemetry) metrics.First();

                Assert.AreEqual(3, metric.Count);
                Assert.AreEqual(4062, metric.Max);
                Assert.AreEqual(4012, metric.Min);
                Assert.AreEqual(true, Math.Abs(metric.StandardDeviation.Value - 20.43417617) < 0.0000001);
                Assert.AreEqual(12113, metric.Sum);
            }
        }

        #endregion Dependency-metrics-related Tests

        #region Common Tools

        internal static TelemetryConfiguration CreateTelemetryConfigWithExtractor(IList<ITelemetry> telemetrySentToChannel,
                                                                                  Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory)
        {
            ITelemetryChannel channel = new StubTelemetryChannel 
            { 
                OnSend = (t) => telemetrySentToChannel.Add(t) 
            };
            string iKey = Guid.NewGuid().ToString("D");
            TelemetryConfiguration telemetryConfig = new TelemetryConfiguration(iKey, channel);

            var channelBuilder = new TelemetryProcessorChainBuilder(telemetryConfig);
            channelBuilder.Use(extractorFactory);
            channelBuilder.Build();

            TelemetryProcessorChain processors = telemetryConfig.TelemetryProcessorChain;
            foreach (ITelemetryProcessor processor in processors.TelemetryProcessors)
            {
                ITelemetryModule m = processor as ITelemetryModule;
                if (m != null)
                {
                    m.Initialize(telemetryConfig);
                }
            }


            return telemetryConfig;
        }

        #endregion Common Tools
    }
}
