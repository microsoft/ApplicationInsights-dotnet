namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Gauge that sums up the values of different gauges.
    /// </summary>
    internal class SumUpGauge : ICounterValue
    {
        /// <summary>
        /// List of gauges whose values will be added.
        /// </summary>
        private readonly List<ICounterValue> gaugesToSum;

        /// <summary>
        /// Name of the counter.
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="SumUpGauge"/> class.
        /// </summary>
        /// <param name="name"> Name of the SumUpGauge.</param>
        /// <param name="gauges"> Gauges to sum.</param>
        public SumUpGauge(string name, params ICounterValue[] gauges)
        {
            this.name = name;
            this.gaugesToSum = new List<ICounterValue>(gauges);
        }

        /// <summary>
        /// Returns the current value of the sum of all different gauges attached to this one and resets their values.
        /// </summary>
        /// <returns> MetricTelemetry object</returns>
        public float GetValueAndReset()
        {
            return this.gaugesToSum.Sum((g) => { return g.GetValueAndReset(); });
            //metric.Context.GetInternalContext().SdkVersion = SdkVersionAzureWebApp.sdkVersionAzureWebApp;
        }
    }
}
