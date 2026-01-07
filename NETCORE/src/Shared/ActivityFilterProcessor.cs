#if AI_ASPNETCORE_WEB
namespace Microsoft.ApplicationInsights.AspNetCore.Extensions
#elif AI_CLASSIC_WEB
namespace Microsoft.ApplicationInsights.Web.Implementation
#else
namespace Microsoft.ApplicationInsights.WorkerService
#endif
{
    using System.Diagnostics;
    using Microsoft.Extensions.Options;
    using OpenTelemetry;

    /// <summary>
    /// An OpenTelemetry processor that filters activities based on ApplicationInsightsServiceOptions settings.
    /// When EnableDependencyTrackingTelemetryModule is false, filters out Client, Internal, and Producer activities.
    /// When EnableRequestTrackingTelemetryModule is false, filters out Server and Consumer activities.
    /// Filtering is done by setting Activity.IsAllDataRequested to false, which prevents export while maintaining context propagation.
    /// </summary>
    internal sealed class ActivityFilterProcessor : BaseProcessor<Activity>
    {
        private readonly bool enableDependencyTracking;
#if AI_ASPNETCORE_WEB || AI_CLASSIC_WEB
        private readonly bool enableRequestTracking;
#endif

#if AI_CLASSIC_WEB
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityFilterProcessor"/> class for classic ASP.NET.
        /// </summary>
        /// <param name="enableDependencyTracking">Whether dependency tracking is enabled.</param>
        /// <param name="enableRequestTracking">Whether request tracking is enabled.</param>
        public ActivityFilterProcessor(bool enableDependencyTracking, bool enableRequestTracking)
        {
            this.enableDependencyTracking = enableDependencyTracking;
            this.enableRequestTracking = enableRequestTracking;
        }
#else
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityFilterProcessor"/> class.
        /// </summary>
        /// <param name="options">The Application Insights service options containing filter settings.</param>
        public ActivityFilterProcessor(IOptions<ApplicationInsightsServiceOptions> options)
        {
            var serviceOptions = options.Value;
            this.enableDependencyTracking = serviceOptions.EnableDependencyTrackingTelemetryModule;
#if AI_ASPNETCORE_WEB
            this.enableRequestTracking = serviceOptions.EnableRequestTrackingTelemetryModule;
#endif
        }
#endif

        /// <summary>
        /// Called when an activity is started. Applies filtering logic based on activity kind and service options.
        /// </summary>
        /// <param name="activity">The activity being started.</param>
        public override void OnStart(Activity activity)
        {
            if (activity == null)
            {
                return;
            }

            // Filter dependency activities (outbound calls) when EnableDependencyTrackingTelemetryModule is false
            if (!this.enableDependencyTracking)
            {
                if (activity.Kind == ActivityKind.Client ||
                    activity.Kind == ActivityKind.Internal ||
                    activity.Kind == ActivityKind.Producer)
                {
                    // Suppress export while maintaining context propagation
                    activity.IsAllDataRequested = false;
                    return;
                }
            }

#if AI_ASPNETCORE_WEB || AI_CLASSIC_WEB
            // Filter request activities (inbound calls) when EnableRequestTrackingTelemetryModule is false
            if (!this.enableRequestTracking)
            {
                if (activity.Kind == ActivityKind.Server ||
                    activity.Kind == ActivityKind.Consumer)
                {
                    // Suppress export while maintaining context propagation
                    activity.IsAllDataRequested = false;
                    return;
                }
            }
#endif
        }
    }
}
