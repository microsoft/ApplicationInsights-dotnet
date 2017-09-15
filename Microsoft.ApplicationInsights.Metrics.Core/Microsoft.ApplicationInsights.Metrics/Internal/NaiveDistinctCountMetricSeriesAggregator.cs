using System;
using System.Collections.Generic;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics.Extensibility;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary>
    /// This class is intended for internal verification only.
    /// It computes distinct counts in a manner that is very resource intensive and its aggregates cannot be combined across machines.
    /// </summary>
    internal class NaiveDistinctCountMetricSeriesAggregator : DataSeriesAggregatorBase, IMetricSeriesAggregator
    {
        private readonly GrowingCollection<string> _values = new GrowingCollection<string>();

        public NaiveDistinctCountMetricSeriesAggregator(NaiveDistinctCountMetricSeriesConfiguration configuration, MetricSeries dataSeries, MetricConsumerKind consumerKind)
            : base(configuration, dataSeries, consumerKind)
        {
        }

        public override bool SupportsRecycle { get { return false; } }

        public override ITelemetry CreateAggregateUnsafe(DateTimeOffset periodEnd)
        {
            // The enumerator of _values operates on a snapshot in a thread-safe manner.
            var distinctValues = new HashSet<string>(_values);

            MetricTelemetry aggregate = new MetricTelemetry(DataSeries.MetricId, distinctValues.Count, sum: 0, min: 0, max: 0, standardDeviation: 0);
            Util.CopyTelemetryContext(DataSeries.Context, aggregate.Context);

            return aggregate;
        }
        

        protected override bool RecycleUnsafe()
        {
            throw new NotSupportedException();
        }

        protected override void TrackFilteredValue(double metricValue)
        {
            TrackFilteredValue(metricValue.ToString());
        }

        protected override void TrackFilteredValue(object metricValue)
        {
            if (metricValue == null)
            {
                TrackFilteredValue((string) null);
            }
            else
            {
                string stringValue = metricValue as string;
                TrackFilteredValue(stringValue ?? metricValue.ToString());
            }
        }

        private void TrackFilteredValue(string metricValue)
        {
            _values.Add(metricValue);
        }
    }
}