using System;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.ApplicationInsights.Metrics.Extensibility;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace SomeCustomerNamespace
{
    /// <summary />
    [TestClass]
    public class MetricEmissionTestsVirtualTime
    {
        /// <summary />
        [TestMethod]
        public void RecordNormalMetric()
        {
            TelemetryConfiguration telemetryPipeline = TelemetryConfiguration.Active;

            MetricSeries durationMeric = telemetryPipeline.Metrics().CreateNewSeries(
                                                                        "Item Add duration",
                                                                        new SimpleMeasurementMetricSeriesConfiguration(lifetimeCounter: false, supportDoubleValues: true));

            MockContainerDataStructure dataStructure = new MockContainerDataStructure((c) => TimeSpan.FromSeconds(c));

            DateTimeOffset experimentStart = new DateTimeOffset(2017, 9, 14, 0, 0, 0, TimeSpan.Zero);

            telemetryPipeline.Metrics().StartAggregators(MetricConsumerKind.Custom, experimentStart, filter: null);

            const int ExperimentLengthSecs = 60 * 10;
            const int IntervalLengthSecs = 60;

            int totalSecs = 0;
            int intervalSecs = 0;
            Random rnd = new Random();

            while (totalSecs < ExperimentLengthSecs)
            {
                int addItemCount = rnd.Next(4);
                int removeItemCount = rnd.Next(4);

                TimeSpan duration;

                dataStructure.AddItems(addItemCount, out duration);

                int durationSecs = (int) duration.TotalSeconds;
                durationMeric.TrackValue(durationSecs);

                totalSecs += durationSecs;
                intervalSecs += durationSecs;

                dataStructure.RemoveItems(removeItemCount, out duration);

                durationSecs = (int) duration.TotalSeconds;
                durationMeric.TrackValue(durationSecs);

                totalSecs += durationSecs;
                intervalSecs += durationSecs;

                if (intervalSecs >= IntervalLengthSecs)
                {
                    intervalSecs %= IntervalLengthSecs;

                    AggregationPeriodSummary aggregatedMetrics = telemetryPipeline.Metrics().CycleAggregators(
                                                                                                MetricConsumerKind.Custom,
                                                                                                experimentStart.AddSeconds(totalSecs),
                                                                                                updatedFilter: null);
                    IReadOnlyList<ITelemetry> aggregates = aggregatedMetrics.NonpersistentAggregates;
                    MetricTelemetry aggregate = (MetricTelemetry) aggregates[0];
                    
                }

            }

        }

        #region class MockContainerDataStructure
        private class MockContainerDataStructure
        {
            private readonly Random _rnd;
            private readonly Func<int, TimeSpan> _customOperationDurationCalculator;

            private int _itemCount;

            public MockContainerDataStructure()
                : this(customOperationDurationCalculator : null)
            {
            }

            public MockContainerDataStructure(Func<int, TimeSpan> customOperationDurationCalculator)
            {
                if (customOperationDurationCalculator == null)
                {
                    _rnd = new Random();
                    _customOperationDurationCalculator = null;
                }
                else
                {
                    _rnd = null;
                    _customOperationDurationCalculator = customOperationDurationCalculator;
                }
                
                _itemCount = 0;
            }

            public void AddItems(int count, out TimeSpan duration)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                duration = GetVirtualOperationDuration(count);
                Interlocked.Add(ref _itemCount, count);
            }

            public void RemoveItems(int count, out TimeSpan duration)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                duration = GetVirtualOperationDuration(count);
                Interlocked.Add(ref _itemCount, -count);
            }

            private TimeSpan GetVirtualOperationDuration(int count)
            {
                if (_customOperationDurationCalculator != null)
                {
                    return _customOperationDurationCalculator(count);
                }

                int millis = _rnd.Next(99) + 1;
                return TimeSpan.FromMilliseconds(millis);
            }
        }
        #endregion class MockContainerDataStructure
    }
}
