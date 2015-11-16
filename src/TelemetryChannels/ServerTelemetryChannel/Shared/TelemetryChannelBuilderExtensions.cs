namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel
{
    using System;
    using System.ComponentModel;

    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation;

    /// <summary>
    /// Extension methods for <see cref="TelemetryProcessorChainBuilder"/>.
    /// Adds shorthand for adding well-known processors.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class TelemetryProcessorChainBuilderExtensions
    {
        /// <summary>
        /// Adds <see cref="SamplingTelemetryProcessor"/> to the given<see cref="TelemetryProcessorChainBuilder" />
        /// </summary>
        /// <param name="builder">Instance of <see cref="TelemetryProcessorChainBuilder"/></param>
        /// <param name="samplingPercentage">Sampling Percentage to configure.</param>        
        /// <return>Instance of <see cref="TelemetryProcessorChainBuilder"/>.</return>
        public static TelemetryProcessorChainBuilder UseSampling(this TelemetryProcessorChainBuilder builder, double samplingPercentage)
        {
            return builder.Use((next) => new SamplingTelemetryProcessor(next) { SamplingPercentage = samplingPercentage });
        }

        /// <summary>
        /// Adds <see cref="AdaptiveSamplingTelemetryProcessor"/> to the <see cref="TelemetryProcessorChainBuilder" />
        /// </summary>
        /// <param name="builder">Instance of <see cref="TelemetryProcessorChainBuilder"/></param>
        /// <return>Instance of <see cref="TelemetryProcessorChainBuilder"/>.</return>
        public static TelemetryProcessorChainBuilder UseAdaptiveSampling(this TelemetryProcessorChainBuilder builder)
        {
            return builder.Use((next) => new AdaptiveSamplingTelemetryProcessor(next));
        }

        /// <summary>
        /// Adds <see cref="AdaptiveSamplingTelemetryProcessor"/> to the <see cref="TelemetryProcessorChainBuilder" />
        /// </summary>
        /// <param name="builder">Instance of <see cref="TelemetryProcessorChainBuilder"/></param>
        /// <param name="maxTelemetryItemsPerSecond">Maximum number of telemetry items to be generated on this application instance.</param>
        /// <return>Instance of <see cref="TelemetryProcessorChainBuilder"/>.</return>
        public static TelemetryProcessorChainBuilder UseAdaptiveSampling(this TelemetryProcessorChainBuilder builder, double maxTelemetryItemsPerSecond)
        {
            return builder.Use((next) => new AdaptiveSamplingTelemetryProcessor(next) { MaxTelemetryItemsPerSecond = maxTelemetryItemsPerSecond });
        }

        /// <summary>
        /// Adds <see cref="AdaptiveSamplingTelemetryProcessor"/> to the <see cref="TelemetryProcessorChainBuilder" />
        /// </summary>
        /// <param name="builder">Instance of <see cref="TelemetryProcessorChainBuilder"/></param>
        /// <param name="settings">Set of settings applicable to dynamic sampling percentage algorithm.</param>
        /// <param name="callback">Callback invoked every time sampling percentage evaluation occurs.</param>
        /// <return>Instance of <see cref="TelemetryProcessorChainBuilder"/>.</return>
        public static TelemetryProcessorChainBuilder UseAdaptiveSampling(
            this TelemetryProcessorChainBuilder builder, 
            SamplingPercentageEstimatorSettings settings,
            AdaptiveSamplingPercentageEvaluatedCallback callback)
        {
            return builder.Use((next) => new AdaptiveSamplingTelemetryProcessor(settings, callback, next) { InitialSamplingPercentage = 100.0 / settings.EffectiveInitialSamplingRate });
        }
    }
}
