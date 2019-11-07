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
                                    // with 100, 300, 500 leading to min=50, max=300, sum = 450
                                    // ValidateAllMetric() method does this validation
                                    requests.Add(CreateRequestTelemetry(
                                        TimeSpan.FromMilliseconds(100), responseCode[j], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                    requests.Add(CreateRequestTelemetry(
                                        TimeSpan.FromMilliseconds(300), responseCode[j], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                    requests.Add(CreateRequestTelemetry(
                                        TimeSpan.FromMilliseconds(50), responseCode[j], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                }
                            }
                        }
                    }
                }

                foreach(var req in requests)
                {
                    client.TrackRequest(req);
                }

                // 2 * 2 * 3 * 2 * 2 * 2 = 96
                // success * synthetic * responseCode * RoleName * RoleInstance * DurationBucket
                // 3 Track calls are made in every iteration,
                // hence 48 * 3 requests gives 144 total requests
                Assert.AreEqual(144, telemetrySentToChannel.Count);

                // The above did not include Metrics as they are sent upon dispose only.                
            } // dispose occurs here, and hence metrics get flushed out.

            // 240 = 144 requests + 96 metrics as there are 96 unique combination of dimension
            Assert.AreEqual(240, telemetrySentToChannel.Count);

            // These are pre-agg metric
            var serverResponseMetric = telemetrySentToChannel.Where(
                (tel) => "Server response time".Equals((tel as MetricTelemetry)?.Name));
            Assert.AreEqual(96, serverResponseMetric.Count());

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
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("request/performanceBucket"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("operation/synthetic"));
            }

            // Validate success dimension
            for (int i = 0; i < success.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["Request.Success"] == success[i].ToString());
                int expectedCount = 96 / success.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate synthetic dimension
            for (int i = 0; i < success.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["operation/synthetic"] == synthetic[i].ToString());
                int expectedCount = 96 / synthetic.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate RoleName dimesion
            for (int i = 0; i < cloudRoleNames.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["cloud/roleName"] == cloudRoleNames[i]);
                int expectedCount = 96 / cloudRoleNames.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate RoleInstance dimension
            for (int i = 0; i < cloudRoleInstances.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["cloud/roleInstance"] == cloudRoleInstances[i]);
                int expectedCount = 96 / cloudRoleInstances.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate ResponseCode dimension
            for (int i = 0; i < responseCode.Length; i++)
            {
                var metricCollection = serverResponseMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["request/resultCode"] == responseCode[i]);
                int expectedCount = 96 / responseCode.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate Duration Bucket dimension
            {
                var metricCollectionBelow250 = serverResponseMetric.Where(
                    (tel) => (tel as MetricTelemetry).Properties["request/performanceBucket"] == "<250ms");

                var metricCollection500mSecTo1Sec = serverResponseMetric.Where(
                    (tel) => (tel as MetricTelemetry).Properties["request/performanceBucket"] == "500ms-1sec");


                int expectedCount = 96 / 2;
                Assert.AreEqual(expectedCount, metricCollectionBelow250.Count());
                ValidateAllMetric(metricCollectionBelow250);

                Assert.AreEqual(expectedCount, metricCollection500mSecTo1Sec.Count());
                ValidateAllMetric(metricCollection500mSecTo1Sec);
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

        private DependencyTelemetry CreateDependencyTelemetry(TimeSpan duration, string target, string type,
            bool success, bool synthetic, string role, string instance)
        {            
            var dep = new DependencyTelemetry(type, target, "Dep1", "data", DateTimeOffset.Now, duration, "resultCode", success);
            dep.Context.Cloud.RoleName = role;
            dep.Context.Cloud.RoleInstance = instance;
            if (synthetic)
            {
                dep.Context.Operation.SyntheticSource = "synthetic";
            }

            return dep;
        }

        private void ValidateAllMetric(IEnumerable<ITelemetry> metricCollection)
        {
            foreach (var singleMetric in metricCollection)
            {
                var m = singleMetric as MetricTelemetry;
                Assert.AreEqual(3, m.Count);
                Assert.AreEqual(50, m.Min);
                Assert.AreEqual(300, m.Max);
                Assert.AreEqual(450, m.Sum);
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
        public void Dependency_CorrectlyExtractsMetric()
        {
            List<ITelemetry> telemetrySentToChannel = new List<ITelemetry>();
            Func<ITelemetryProcessor, AutocollectedMetricsExtractor> extractorFactory = (nextProc) => new AutocollectedMetricsExtractor(nextProc);

            // default set of dimensions with test values.
            bool[] success = new bool[] { true, false };
            bool[] synthetic = new bool[] { true, false };
            string[] targets = new string[] { "TargetA", "TargetB", "TargetC" };
            string[] types = new string[] { "TypeA", "TypeB", "TypeC" };
            string[] cloudRoleNames = new string[] { "RoleA", "RoleB" };
            string[] cloudRoleInstances = new string[] { "RoleInstanceA", "RoleInstanceB" };

            TelemetryConfiguration telemetryConfig = CreateTelemetryConfigWithExtractor(telemetrySentToChannel, extractorFactory);
            using (telemetryConfig)
            {
                TelemetryClient client = new TelemetryClient(telemetryConfig);
                List<DependencyTelemetry> dependencies = new List<DependencyTelemetry>();

                // Produces telemetry with every combination of dimension values.
                for (int i = 0; i < success.Length; i++)
                {
                    for (int j = 0; j < targets.Length; j++)
                    {
                        for (int k = 0; k < cloudRoleNames.Length; k++)
                        {
                            for (int l = 0; l < cloudRoleInstances.Length; l++)
                            {
                                for (int m = 0; m < synthetic.Length; m++)
                                {
                                    for (int n = 0; n < types.Length; n++)
                                    {
                                        // For ease of validation 3 calls are tracked.
                                        // with 10, 30, 5 leading to min=5, max=30, sum = 45
                                        // ValidateAllMetric() method does this validation
                                        dependencies.Add(CreateDependencyTelemetry(
                                        TimeSpan.FromMilliseconds(10), targets[j], types[n], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                        dependencies.Add(CreateDependencyTelemetry(
                                            TimeSpan.FromMilliseconds(30), targets[j], types[n], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                        dependencies.Add(CreateDependencyTelemetry(
                                            TimeSpan.FromMilliseconds(5), targets[j], types[n], success[i], synthetic[m], cloudRoleNames[k], cloudRoleInstances[l]));
                                    }                                    
                                }
                            }
                        }
                    }
                }

                foreach (var dep in dependencies)
                {
                    client.TrackDependency(dep);
                }

                // 2 * 2 * 3 * 3 *  2 * 2 = 144
                // success * synthetic * target * type * RoleName * RoleInstance
                // 3 Track calls are made in every iteration,
                // hence 144 * 3 dependencies gives 432 total dependencies
                Assert.AreEqual(432, telemetrySentToChannel.Count);

                // The above did not include Metrics as they are sent upon dispose only.                
            } // dispose occurs here, and hence metrics get flushed out.

            // 576 = 432 requests + 144 metrics as there are 144 unique combination of dimension
            Assert.AreEqual(576, telemetrySentToChannel.Count);

            // These are pre-agg metric
            var depDurationMetric = telemetrySentToChannel.Where(
                (tel) => "Dependency duration".Equals((tel as MetricTelemetry)?.Name));
            Assert.AreEqual(144, depDurationMetric.Count());

            foreach (var metric in depDurationMetric)
            {
                var metricTel = metric as MetricTelemetry;
                // validate standard fields
                Assert.IsTrue(metricTel.Properties.ContainsKey("_MS.AggregationIntervalMs"));
                Assert.IsTrue(metricTel.Context.GlobalProperties.ContainsKey("_MS.IsAutocollected"));
                Assert.AreEqual("True", metricTel.Context.GlobalProperties["_MS.IsAutocollected"]);
                Assert.IsTrue(metricTel.Context.GlobalProperties.ContainsKey("_MS.MetricId"));
                Assert.AreEqual("dependencies/duration", metricTel.Context.GlobalProperties["_MS.MetricId"]);

                // validate dimensions exist
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("Dependency.Success"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("cloud/roleInstance"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("cloud/roleName"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("Dependency.Type"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("dependency/target"));
                Assert.AreEqual(true, metricTel.Properties.ContainsKey("operation/synthetic"));
            }

            // Validate success dimension
            for (int i = 0; i < success.Length; i++)
            {
                var metricCollection = depDurationMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["Dependency.Success"] == success[i].ToString());
                int expectedCount = 144 / success.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate synthetic dimension
            for (int i = 0; i < success.Length; i++)
            {
                var metricCollection = depDurationMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["operation/synthetic"] == synthetic[i].ToString());
                int expectedCount = 144 / synthetic.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate RoleName dimesion
            for (int i = 0; i < cloudRoleNames.Length; i++)
            {
                var metricCollection = depDurationMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["cloud/roleName"] == cloudRoleNames[i]);
                int expectedCount = 144 / cloudRoleNames.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate RoleInstance dimension
            for (int i = 0; i < cloudRoleInstances.Length; i++)
            {
                var metricCollection = depDurationMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["cloud/roleInstance"] == cloudRoleInstances[i]);
                int expectedCount = 144 / cloudRoleInstances.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate Dep Type dimension
            for (int i = 0; i < types.Length; i++)
            {
                var metricCollection = depDurationMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["Dependency.Type"] == types[i]);
                int expectedCount = 144 / types.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
            }

            // Validate Dep Target dimension
            for (int i = 0; i < targets.Length; i++)
            {
                var metricCollection = depDurationMetric.Where(
                (tel) => (tel as MetricTelemetry).Properties["dependency/target"] == targets[i]);
                int expectedCount = 144 / types.Length;
                Assert.AreEqual(expectedCount, metricCollection.Count());
                ValidateAllMetric(metricCollection);
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
