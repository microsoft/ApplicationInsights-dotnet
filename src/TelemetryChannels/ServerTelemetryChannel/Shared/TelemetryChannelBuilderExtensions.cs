namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;

    /// <summary>
    /// Extension methods for <see cref="TelemetryChannelBuilder"/>.
    /// Adds shorthand for adding well-known processors.
    /// </summary>
    public static class TelemetryChannelBuilderExtensions
    {
        /// <summary>
        /// Adds <see cref="SamplingTelemetryProcessor"/> to the <see cref="TelemetryChannelBuilder" />
        /// </summary>
        /// <param name="builder">Instance of <see cref="TelemetryChannelBuilder"/></param>
        /// <param name="samplingPercentage">Sampling Percentage to configure.</param>
        /// <return>Instance of <see cref="TelemetryChannelBuilder"/>.</return>
        public static TelemetryChannelBuilder UseSampling(this TelemetryChannelBuilder builder, double samplingPercentage)
        {
            return builder.Use((next) => new SamplingTelemetryProcessor(next) { SamplingPercentage = samplingPercentage });
        }

        public static TelemetryChannelBuilder UseAdaptiveSampling(this TelemetryChannelBuilder builder)
        {
            return builder.Use((next) => new AdaptiveSamplingTelemetryProcessor(next));
        }

        public static TelemetryChannelBuilder UseAdaptiveSampling(this TelemetryChannelBuilder builder, double maxTelemetryItemsPerSecond)
        {
            return builder.Use((next) => new AdaptiveSamplingTelemetryProcessor(next) { MaxTelemetryItemsPerSecond = maxTelemetryItemsPerSecond });
        }

        public static TelemetryChannelBuilder UseAdaptiveSampling(
            this TelemetryChannelBuilder builder, 
            double initialSamplingPercentage, 
            SamplingPercentageEstimatorSettings settings,
            AdaptiveSamplingPercentageEvaluatedCallback callback)
        {
            return builder.Use((next) => new AdaptiveSamplingTelemetryProcessor(settings, callback, next) { InitialSamplingPercentage = initialSamplingPercentage });
        }
    }
}
