using System.Diagnostics;

namespace Microsoft.ApplicationInsights
{
    using Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Internal;
    using Microsoft.ApplicationInsights.Tests;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using OpenTelemetry.Trace;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using System.Linq;

    public class FeatureMetricEmissionHelperTests
    {

        [Fact]
        public void GetOrCreate_SharesInstances()
        {
            using var helper1 = FeatureMetricEmissionHelper.GetOrCreate("a", "b");
            using var helper2 = FeatureMetricEmissionHelper.GetOrCreate("a", "b");
            using var helper3 = FeatureMetricEmissionHelper.GetOrCreate("different", "b");

            Assert.Same(helper1, helper2);
            Assert.NotSame(helper1, helper3);
        }

        [Fact]
        public void ReportsFeaturesSeen()
        {
            using var helper1 = FeatureMetricEmissionHelper.GetOrCreate("a", "b");

            var metric = helper1.GetFeatureStatsbeat();

            // no features reported should not emit metric
            Assert.Equal(0, metric.Value);
            Assert.Empty(metric.Tags.ToArray());

            helper1.MarkFeatureInUse(StatsbeatFeatures.TrackEvent);
            helper1.MarkFeatureInUse(StatsbeatFeatures.TrackDependency);

            metric = helper1.GetFeatureStatsbeat();
            var tags = metric.Tags.ToArray().ToDictionary(v => v.Key, v => v.Value);

            Assert.Equal(1, metric.Value);
            AssertKey("rp", "unknown", tags);
            AssertKey("attach", "Manual", tags);
            AssertKey("cikey", "a", tags);
            AssertKey("feature", (ulong)1 + 32, tags); // == TrackEvent + TrackDependency
            AssertKey("type", 0, tags); // == feature
            Assert.Contains("os", tags);
            AssertKey("language", "dotnet", tags);
            AssertKey("version", "b", tags);
        }

        [Fact]
        public void Constructor_RejectsNullVmMetadataLookup()
        {
            Assert.Throws<ArgumentNullException>(() => new FeatureMetricEmissionHelper("a", "b", null));
        }

        [Fact]
        public void Constructor_DoesNotLookUpVmMetadata()
        {
            int calls = 0;
            using var helper = new FeatureMetricEmissionHelper("a", "b", () => { calls++; return null; });

            Assert.Equal(0, calls);

            // nothing observed => still no lookup
            helper.GetFeatureStatsbeat();
            Assert.Equal(0, calls);
        }

        [Fact]
        public void VmMetadataIsLookedUpOnceOnFirstReport_AndUsedForRpAndOs()
        {
            // These make the helper pick functions/appsvc/aks and skip the VM lookup, so clear them for this test.
            string[] azureEnvVars = { "FUNCTIONS_WORKER_RUNTIME", "WEBSITE_SITE_NAME", "AKS_ARM_NAMESPACE_ID" };
            string[] originalValues = azureEnvVars.Select(Environment.GetEnvironmentVariable).ToArray();
            try
            {
                foreach (var name in azureEnvVars)
                {
                    Environment.SetEnvironmentVariable(name, null);
                }

                int calls = 0;
                using var helper = new FeatureMetricEmissionHelper("a", "b", () =>
                {
                    calls++;
                    return new Dictionary<string, object> { ["osType"] = "Linux" };
                });
                helper.MarkFeatureInUse(StatsbeatFeatures.TrackEvent);

                var first = helper.GetFeatureStatsbeat().Tags.ToArray().ToDictionary(v => v.Key, v => v.Value);
                helper.GetFeatureStatsbeat();

                Assert.Equal(1, calls);
                AssertKey("rp", "vm", first);
                AssertKey("os", "linux", first);
            }
            finally
            {
                for (int i = 0; i < azureEnvVars.Length; i++)
                {
                    Environment.SetEnvironmentVariable(azureEnvVars[i], originalValues[i]);
                }
            }
        }

        private void AssertKey<V, T>(string key, T value, IDictionary<string, V> dictionary) where T: V
        {
            Assert.Contains(key, dictionary);
            Assert.Equal(value, dictionary[key]);
        }
    }
}