namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Experimental
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// This class provides a means to interact with the <see cref="TelemetryConfiguration.ExperimentalFeatures" />.
    /// This performs a simple boolean evaluation; does a feature name exist in the string array?
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
        /// Cache the result in a nullable boolean for future queries.
        /// </summary>
        /// <param name="telemetryConfiguration">Configuration to be evaluated.</param>
        /// <param name="featureName">Name of the feature to evaluate.</param>
        /// <param name="featureValue">Nullable boolean to cache evaluation result.</param>
        /// <returns>Returns a boolean value indicating if the feature name exists in the provided configuration.</returns>
        public static bool EvaluateExperimentalFeature(this TelemetryConfiguration telemetryConfiguration, string featureName, ref bool? featureValue)
        {
            if (!featureValue.HasValue)
            {
                featureValue = DoesFeatureFlagExist(telemetryConfiguration.ExperimentalFeatures, featureName);
            }

            return featureValue.Value;
        }

        private static bool DoesFeatureFlagExist(IEnumerable<string> featureflags, string featureName)
        {
            return featureflags != null && featureflags.Any(x => x.Equals(featureName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
