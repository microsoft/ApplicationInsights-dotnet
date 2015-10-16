namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Extension methods for <see cref="TelemetryProcessorChainBuilder"/>.
    /// Adds shorthand for adding well-known processors.
    /// </summary>
    public static class TelemetryProcessorChainBuilderExtensions
    {
        /// <summary>
        /// Adds <see cref="SamplingTelemetryProcessor"/> to the given<see cref="TelemetryProcessorChainBuilder" />
        /// </summary>
        /// <param name="builder">Instance of <see cref="TelemetryProcessorChainBuilder"/></param>
        /// <param name="samplingPercentage">Sampling Percentage to configure.</param>        
        public static void UseSampling(this TelemetryProcessorChainBuilder builder, double samplingPercentage)
        {
            builder.Use((next) => new SamplingTelemetryProcessor(next) { SamplingPercentage = samplingPercentage });
        }
    }
}
