namespace Microsoft.ApplicationInsights.Internal
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Copies an operation's telemetry item onto its Activity when the operation is stopped, using the Microsoft override
    /// attributes that the Azure Monitor exporter maps to the Application Insights fields (the same keys TrackDependency
    /// and TrackRequest use). Only values that are set are applied.
    /// </summary>
    internal static class ActivityShimMapper
    {
        public static void ApplyDependencyTags(Activity activity, DependencyTelemetry dep)
        {
            if (activity == null || dep == null)
            {
                return;
            }

            SetTagIfNotEmpty(activity, SemanticConventions.AttributeMicrosoftDependencyType, dep.Type);
            SetTagIfNotEmpty(activity, SemanticConventions.AttributeMicrosoftDependencyData, dep.Data);
            SetTagIfNotEmpty(activity, SemanticConventions.AttributeMicrosoftDependencyTarget, dep.Target);
            SetTagIfNotEmpty(activity, SemanticConventions.AttributeMicrosoftDependencyResultCode, dep.ResultCode);
            ApplyCommon(activity, dep, dep.Properties);
        }

        public static void ApplyRequestTags(Activity activity, RequestTelemetry request)
        {
            if (activity == null || request == null)
            {
                return;
            }

            SetTagIfNotEmpty(activity, SemanticConventions.AttributeMicrosoftRequestUrl, request.Url?.ToString());
            SetTagIfNotEmpty(activity, SemanticConventions.AttributeMicrosoftRequestSource, request.Source);
            SetTagIfNotEmpty(activity, SemanticConventions.AttributeMicrosoftRequestResultCode, request.ResponseCode);
            ApplyCommon(activity, request, request.Properties);
        }

        private static void ApplyCommon(Activity activity, OperationTelemetry telemetry, IDictionary<string, string> properties)
        {
            if (properties != null)
            {
                foreach (var property in properties)
                {
                    // Don't overwrite tags already on the activity: StartOperation(Activity) seeds Properties from the
                    // activity's own tags, which instrumentation may have updated since.
                    if (activity.GetTagItem(property.Key) == null)
                    {
                        activity.SetTag(property.Key, property.Value);
                    }
                }
            }

            // Leave the status untouched when Success is not set, so an unset value is not reported as a failure.
            if (telemetry.Success.HasValue)
            {
                activity.SetStatus(telemetry.Success.Value ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
            }
        }

        private static void SetTagIfNotEmpty(Activity activity, string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                activity.SetTag(key, value);
            }
        }
    }
}