namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.WebAppPerformanceCollector
{
    using System;
    using DataContracts;

    /// <summary>
    /// Gauge that computes the CPU percentage utilized by a process by utilizing the last computed time.
    /// </summary>
    internal class CPUPercenageGauge : ICounterValue
    {
        /// <summary>
        /// Name of the counter.
        /// </summary>
        private string name;

        private double lastCollectedValue;

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
        /// Returns the percentage of the CPU process utilization time with respect to the total duration.
        /// </summary>
        /// <returns>The value of the target metric.</returns>
        public double GetValueAndReset()
        {
            double previouslyCollectedValue = this.lastCollectedValue;
            this.lastCollectedValue = this.valueProvider.GetValueAndReset();

            var previouslyCollectedTime = this.lastCollectedTime;
            this.lastCollectedTime = DateTimeOffset.UtcNow;

            double value = 0;
            if (previouslyCollectedTime != DateTimeOffset.MinValue)
            {
                var baseValue = this.lastCollectedTime.Ticks - previouslyCollectedTime.Ticks;
                baseValue = baseValue != 0 ? baseValue : 1;

                value = (float)((this.lastCollectedValue - previouslyCollectedValue) / baseValue * 100.0);
                var client = new TelemetryClient();
                client.TrackTrace(new TraceTelemetry("Time Tracking: " + previouslyCollectedValue + "|" + this.lastCollectedValue + "|" + previouslyCollectedTime.Ticks + "|" + this.lastCollectedTime.Ticks));
            }

            return value;
        }
    }
}
