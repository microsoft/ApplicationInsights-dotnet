namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerfCollector
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Gauge that sums up the values of different gauges.
    /// </summary>
    internal class SumUpCountersGauge : ICounterValue
    {
        /// <summary>
        /// List of gauges whose values will be added.
        /// </summary>
        private readonly List<ICounterValue> gaugesToSum;

        /// <summary>
        /// Initializes a new instance of the <see cref="SumUpCountersGauge"/> class.
        /// </summary>
        /// <param name="gauges"> Gauges to sum.</param>
        public SumUpCountersGauge(params ICounterValue[] gauges)
        {
            this.gaugesToSum = new List<ICounterValue>(gauges);
        }

        /// <summary>
        /// Returns the current value of the sum of all different gauges attached to this one and resets their values.
        /// </summary>
        /// <returns>The value of the target metric.</returns>
        public double Collect()
        {
            return this.gaugesToSum.Sum((g) => { return g.Collect(); });
        }
    }
}
