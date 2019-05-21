namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Experimental
{
    using System;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// This class provides a means to interact with the <see cref="TelemetryConfiguration.ExperimentalFeatures" />.
    /// This performs a simple boolean evaluation; does a feature name exist in the string array?.
    /// </summary>
    /// <remarks>
    /// This allows the dev team to ship and evaluate features before adding these to the public API.
    /// We are not committing to support any features enabled through this property.
    /// Use this at your own risk.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ExperimentalFeaturesExtension
    {
        /// <summary>
        /// Evaluate the TelemetryConfiguration to determine if a feature is enabled.
        /// The cache variable is provided. This method will evaluate the cache and set the cache accordingly.
        /// </summary>
        /// <param name="telemetryConfiguration">Configuration to be evaluated.</param>
        /// <param name="featureName">Name of the feature to evaluate.</param>
        /// <returns>Returns a boolean value indicating if the feature name exists in the provided configuration.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool EvaluateExperimentalFeature(this TelemetryConfiguration telemetryConfiguration, string featureName)
        {
            return telemetryConfiguration.ExperimentalFeatures != null
                && telemetryConfiguration.ExperimentalFeatures.Any(x => x.Equals(featureName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
