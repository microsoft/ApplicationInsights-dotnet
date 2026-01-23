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
            AssertKey("product", "appinsights", tags);
            AssertKey("version", "b", tags);
        }

        private void AssertKey<V, T>(string key, T value, IDictionary<string, V> dictionary) where T: V
        {
            Assert.Contains(key, dictionary);
            Assert.Equal(value, dictionary[key]);
        }
    }
}