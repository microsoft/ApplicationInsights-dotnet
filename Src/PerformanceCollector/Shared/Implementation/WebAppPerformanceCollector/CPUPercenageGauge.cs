namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation
{
    using System;

    /// <summary>
    /// Gauge that sums up the values of different gauges.
    /// </summary>
    internal class CPUPercenageGauge : ICounterValue
    {
        /// <summary>
        /// Name of the counter.
        /// </summary>
        private string name;

        private float lastCollectedValue;

        private DateTimeOffset lastCollectedTime;

        private ICounterValue valueProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CPUPercenageGauge"/> class.
        /// </summary>
        /// <param name="name"> Name of the SumUpCountersGauge.</param>
        /// <param name="value"> Gauges to sum.</param>
        public CPUPercenageGauge(string name, ICounterValue value)
        {
            this.name = name;
            this.valueProvider = value;
        }

        /// <summary>
        /// Returns the current value of the sum of all different gauges attached to this one and resets their values.
        /// </summary>
        /// <returns> MetricTelemetry object.</returns>
        public float GetValueAndReset()
        {
            float previouslyCollectedValue = this.lastCollectedValue;
            this.lastCollectedValue = this.valueProvider.GetValueAndReset();

            var previouslyCollectedTime = this.lastCollectedTime;
            this.lastCollectedTime = DateTimeOffset.UtcNow;

            float value = 0;
            if (previouslyCollectedTime != DateTimeOffset.MinValue)
            {
                var baseValue = this.lastCollectedTime.Ticks - previouslyCollectedTime.Ticks;
                baseValue = baseValue != 0 ? baseValue : 1;

                value = (float)((this.lastCollectedValue - previouslyCollectedValue) / baseValue * 100.0);
            }

            return value;
        }
    }
}
