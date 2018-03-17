using System;
using System.ComponentModel;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary>
    /// Provides discoverable access to constants used by metric aggregates.
    /// Do not use directly. Instead, use: <c>MetricConfigurations.Common.Xxxx().Constants()</c>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MetricConfigurationExtensions
    {
        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForAccumulator"/>. See also <c>MetricConfigurations.Common.Accumulator()</c>./>
        /// </summary>
        /// <param name="accumulatorConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForAccumulator.AggregateKindConstants Constants(this MetricSeriesConfigurationForAccumulator accumulatorConfig)
        {
            return MetricSeriesConfigurationForAccumulator.AggregateKindConstants.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricConfigurationForAccumulator"/>. See also <c>MetricConfigurations.Common.Accumulator()</c>./>
        /// </summary>
        /// <param name="accumulatorConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForAccumulator.AggregateKindConstants Constants(this MetricConfigurationForAccumulator accumulatorConfig)
        {
            return MetricSeriesConfigurationForAccumulator.AggregateKindConstants.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForGauge"/>. See also <c>MetricConfigurations.Common.Gauge()</c>./>
        /// </summary>
        /// <param name="gaugeConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForGauge.AggregateKindConstants Constants(this MetricSeriesConfigurationForGauge gaugeConfig)
        {
            return MetricSeriesConfigurationForGauge.AggregateKindConstants.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricConfigurationForGauge"/>. See also <c>MetricConfigurations.Common.Gauge()</c>./>
        /// </summary>
        /// <param name="gaugeConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForGauge.AggregateKindConstants Constants(this MetricConfigurationForGauge gaugeConfig)
        {
            return MetricSeriesConfigurationForGauge.AggregateKindConstants.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForNaiveDistinctCount"/>. See also <c>MetricConfigurations.Common.NaiveDistinctCount()</c>./>
        /// </summary>
        /// <param name="naiveDistinctCountConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForNaiveDistinctCount.AggregateKindConstants Constants(
                                                            this MetricSeriesConfigurationForNaiveDistinctCount naiveDistinctCountConfig)
        {
            return MetricSeriesConfigurationForNaiveDistinctCount.AggregateKindConstants.Instance;
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricConfigurationForNaiveDistinctCount"/>. See also <c>MetricConfigurations.Common.NaiveDistinctCount()</c>./>
        /// </summary>
        /// <param name="naiveDistinctCountConfig"></param>
        /// <returns></returns>
        public static MetricSeriesConfigurationForNaiveDistinctCount.AggregateKindConstants Constants(
                                                            this MetricConfigurationForNaiveDistinctCount naiveDistinctCountConfig)
        {
            return MetricSeriesConfigurationForNaiveDistinctCount.AggregateKindConstants.Instance;
        }
    }
}
