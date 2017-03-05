using System;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using System.Collections.Generic;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    public class DependencyMetricExtractor : MetricExtractorTelemetryProcessorBase
    {
        private const string Version = "1.0";

        private IDictionary<string, Metric> successDurationMetricsByType = null;
        private IDictionary<string, Metric> failureDurationMetricsByType = null;

        private Metric successDurationMetricDefault = null;
        private Metric failureDurationMetricDefault = null;

        private string[] dependencyTypes = null;
        private string dependenccyTypesString = String.Empty;

        public DependencyMetricExtractor(ITelemetryProcessor nextProcessorInPipeline)
            :base(nextProcessorInPipeline, MetricTerms.Autocollection.Moniker.Key, MetricTerms.Autocollection.Moniker.Value)
        {
        }

        protected override string ExtractorVersion { get { return Version; } }

        public string DependencyTypes
        {
            get
            {
                return dependenccyTypesString;
            }

            set
            {
                // This setter is not thread safe. However, we expect it to be called rarely.
                // Worst case: queryable config and actual list of types briefly mismatch. Nothing breaks.

                if (value == null)
                {
                    this.dependenccyTypesString = String.Empty;
                    this.dependencyTypes = null;
                }
                else
                {
                    string[] depTypes = value.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < depTypes.Length; i++)
                    {
                        depTypes[i] = depTypes[i].Trim();
                    }
                    Array.Sort(depTypes);

                    this.dependenccyTypesString = String.Join("|", depTypes);
                    this.dependencyTypes = (depTypes.Length == 0) ? null : depTypes;
                }

                InitializeExtractor(unusedConfiguration: null);
            }
        }

        public override void InitializeExtractor(TelemetryConfiguration unusedConfiguration)
        {
            // This initializer is not thread safe. However, we expect it to be called rarely.
            // Worst case: metric containers briefly mismatch. Nothing breaks.

            string[] depTypes = this.dependencyTypes;

            if (depTypes == null || depTypes.Length == 0)
            {
                this.successDurationMetricsByType = null;
                this.failureDurationMetricsByType = null;

                this.successDurationMetricDefault = MetricManager.CreateMetric(
                        MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                        new Dictionary<string, string>() {
                            [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.TrueString,
                        });

                this.failureDurationMetricDefault = MetricManager.CreateMetric(
                        MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                        new Dictionary<string, string>() {
                            [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString,
                        });
            }
            else
            {
                var successDurationsByType = new Dictionary<string, Metric>();
                var failureDurationsByType = new Dictionary<string, Metric>();

                for (int i = 0; i < depTypes.Length; i++)
                {
                    successDurationsByType[depTypes[i]] = MetricManager.CreateMetric(
                            MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                            new Dictionary<string, string>() {
                                [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.TrueString,
                                [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = depTypes[i],
                            });

                    failureDurationsByType[depTypes[i]] = MetricManager.CreateMetric(
                            MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                            new Dictionary<string, string>() {
                                [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString,
                                [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = depTypes[i],
                            });
                }

                this.successDurationMetricsByType = successDurationsByType;
                this.failureDurationMetricsByType = failureDurationsByType;

                this.successDurationMetricDefault = MetricManager.CreateMetric(
                        MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                        new Dictionary<string, string>() {
                            [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.TrueString,
                            [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Other,
                        });

                this.failureDurationMetricDefault = MetricManager.CreateMetric(
                        MetricTerms.Autocollection.MetricNames.DependencyCall.Duration,
                        new Dictionary<string, string>() {
                            [MetricTerms.Autocollection.DependencyCall.PropertyName.Success] = Boolean.FalseString,
                            [MetricTerms.Autocollection.DependencyCall.PropertyName.TypeName] = MetricTerms.Autocollection.DependencyCall.TypeName.Other,
                        });
            }
        }

        public override void ExtractMetrics(ITelemetry fromItem, out bool isItemProcessed)
        {
            DependencyTelemetry dependencyCall = fromItem as DependencyTelemetry;
            if (dependencyCall == null)
            {
                isItemProcessed = false;
                return;
            }

            bool isFailed = (dependencyCall.Success != null) && (dependencyCall.Success == false);
            Metric metric = null;
            string dependencyType = dependencyCall.Type;

            IDictionary<string, Metric> durationMetricsByType = (isFailed)
                                                                    ? this.failureDurationMetricsByType
                                                                    : this.successDurationMetricsByType;

            if (durationMetricsByType == null || dependencyType == null)
            {
                metric = (isFailed ? this.failureDurationMetricDefault : this.successDurationMetricDefault);
            }
            else
            {
                bool isKnownType = durationMetricsByType.TryGetValue(dependencyType, out metric);
                if (! isKnownType)
                {
                    metric = (isFailed ? this.failureDurationMetricDefault : this.successDurationMetricDefault);
                }
            }

            if (metric != null)
            {
                isItemProcessed = true;
                metric.Track(dependencyCall.Duration.TotalMilliseconds);
            }
            else
            {
                isItemProcessed = false;
            }
        }
    }
}
