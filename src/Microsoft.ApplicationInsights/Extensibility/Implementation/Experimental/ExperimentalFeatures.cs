namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Experimental
{
    /// <summary>
    /// This class provides a means to interact with the <see cref="TelemetryConfiguration.ExperimentalFeatures" />.
    /// This performs a simple boolean evaluation; does a feature name exist in the string array?
    /// Evaluation results are cached.
    /// </summary>
    internal static class ExperimentalFeatures
    {
        // internal static bool? exampleFeature;
        // internal static bool IsExampleFeatureEnabled(TelemetryConfiguration telemetryConfiguration) => telemetryConfiguration.EvaluateExperimentalFeature(nameof(exampleFeature), ref exampleFeature);
    }
}