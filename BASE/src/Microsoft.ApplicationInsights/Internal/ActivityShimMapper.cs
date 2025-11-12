namespace Microsoft.ApplicationInsights.Internal
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;

    internal static class ActivityShimMapper
    {
        public static void ApplyDependencyTags(Activity activity, DependencyTelemetry dep)
        {
            if (activity == null || dep == null)
            {
                return;
            }

            // Map core AI DependencyTelemetry → OpenTelemetry semantic conventions
            if (!string.IsNullOrEmpty(dep.Type))
            {
                if (string.Equals(dep.Type, "Http", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(dep.Data) &&
                        Uri.TryCreate(dep.Data, UriKind.Absolute, out var uri))
                    {
                        activity.SetTag("url.full", uri.ToString());
                        activity.SetTag("http.method", "_OTHER");
                        activity.SetTag("server.address", uri.Host);
                        activity.SetTag("server.port", uri.Port);
                    }
                }
                else if (string.Equals(dep.Type, "SQL", StringComparison.OrdinalIgnoreCase))
                {
                    activity.SetTag("db.system", "mssql");
                    activity.SetTag("db.statement", dep.Data);
                    if (!string.IsNullOrEmpty(dep.Target))
                    {
                        activity.SetTag("server.address", dep.Target);
                    }
                }
                else if (string.Equals(dep.Type, "Queue Message", StringComparison.OrdinalIgnoreCase))
                {
                    activity.SetTag("messaging.system", "queue");
                    activity.SetTag("messaging.destination", dep.Data);

                    if (!string.IsNullOrEmpty(dep.Target) &&
                        Uri.TryCreate(dep.Target, UriKind.Absolute, out var uri))
                    {
                        activity.SetTag("server.address", uri.Host);
                    }
                }
                else
                {
                    activity.SetTag("microsoft.dependency.type", dep.Type);
                }
            }

            if (!string.IsNullOrEmpty(dep.ResultCode))
            {
                activity.SetTag("http.response.status_code", dep.ResultCode);
            }

            if (!string.IsNullOrEmpty(dep.Target))
            {
                activity.SetTag("microsoft.dependency.target", dep.Target);
            }

            activity.SetStatus(dep.Success == true ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        }
    }
}
