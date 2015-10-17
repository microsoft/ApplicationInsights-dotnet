namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;

    /// <summary>
    /// Represents a telemetry processor for sampling telemetry at a fixed-rate before sending to Application Insights.
    /// </summary>
    public sealed class SamplingTelemetryProcessor : ITelemetryProcessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SamplingTelemetryProcessor"/> class.
        /// <param name="next">Next TelemetryProcessor in call chain.</param>
        /// </summary>
        public SamplingTelemetryProcessor(ITelemetryProcessor next)
        {
            if (next == null)
            {
                throw new ArgumentNullException("next");
            }

            this.SamplingPercentage = 100.0;
            this.Next = next;
        }
        
        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100) for all <see cref="ITelemetry"/>
        /// objects logged in this <see cref="TelemetryClient"/>.
        /// </summary>
        /// <remarks>
        /// All sampling percentage must be in a ratio of 100/N where N is a whole number (2, 3, 4, …). E.g. 50 for 1/2 or 33.33 for 1/3.
        /// Failure to follow this pattern can result in unexpected / incorrect computation of values in the portal.
        /// </remarks>
        public double SamplingPercentage { get; set; }

        /// <summary>
        /// Gets or sets the next TelemetryProcessor in call chain.
        /// </summary>
        private ITelemetryProcessor Next { get; set; }

        /// <summary>
        /// Process a collected telemetry item.
        /// </summary>
        /// <param name="item">A collected Telemetry item.</param>
        public void Process(ITelemetry item)
        {
            if (this.SamplingPercentage < 100.0 - 1.0E-12)
            {
                // set sampling percentage on telemetry item, current codebase assumes it is the only one updating SamplingPercentage.
                var samplingSupportingTelemetry = item as ISupportSampling;

                if (samplingSupportingTelemetry != null)
                {
                    samplingSupportingTelemetry.SamplingPercentage = this.SamplingPercentage;
                }

                if (!this.IsSampledIn(item))
                {
                    return;
                }
            }

            this.Next.Process(item);
        }

        private bool IsSampledIn(ITelemetry telemetry)
        {
            // check if telemetry supports sampling
            var samplingSupportingTelemetry = telemetry as ISupportSampling;

            if (samplingSupportingTelemetry == null)
            {
                return true;
            }

            // check sampling < 100% is specified
            if (samplingSupportingTelemetry.SamplingPercentage >= 100.0)
            {
                return true;
            }
            
            return SamplingScoreGenerator.GetSamplingScore(telemetry) < samplingSupportingTelemetry.SamplingPercentage;
        }
    }
}
