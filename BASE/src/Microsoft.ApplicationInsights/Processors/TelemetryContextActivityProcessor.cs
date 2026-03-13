namespace Microsoft.ApplicationInsights.Processors
{
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;
    using OpenTelemetry;

    /// <summary>
    /// An activity processor that applies client-level <see cref="TelemetryContext"/> properties
    /// to all activities as tags, using skip-if-present semantics.
    /// This ensures context tags are applied universally — to Track* calls, Start/Stop operations,
    /// and any OpenTelemetry API activities emitted by customer code.
    /// </summary>
    internal sealed class TelemetryContextActivityProcessor : BaseProcessor<Activity>
    {
        private readonly TelemetryContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryContextActivityProcessor"/> class.
        /// </summary>
        /// <param name="context">The client-level <see cref="TelemetryContext"/> to apply.</param>
        public TelemetryContextActivityProcessor(TelemetryContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Called when an activity ends. Applies client-level context tags using
        /// skip-if-present semantics: if a tag is already set (by item-level context,
        /// Track* methods, or instrumentation), it is not overwritten.
        /// This matches the 2.x SDK's <c>Tags.CopyTagValue</c> precedence behavior.
        /// </summary>
        /// <param name="activity">The activity that ended.</param>
        public override void OnEnd(Activity activity)
        {
            if (activity == null || this.context == null)
            {
                return;
            }

            // Apply client-level GlobalProperties (lowest priority — will not overwrite existing tags)
            var globalProperties = this.context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    SetTagIfAbsent(activity, kvp.Key, kvp.Value);
                }
            }

            SetTagIfAbsent(activity, SemanticConventions.AttributeEnduserPseudoId, this.context.User?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeEnduserId, this.context.User?.AuthenticatedUserId);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftOperationName, this.context.Operation?.Name);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftClientIp, this.context.Location?.Ip);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftSessionId, this.context.Session?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceId, this.context.Device?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceModel, this.context.Device?.Model);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceType, this.context.Device?.Type);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceOsVersion, this.context.Device?.OperatingSystem);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftSyntheticSource, this.context.Operation?.SyntheticSource);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftUserAccountId, this.context.User?.AccountId);

            base.OnEnd(activity);
        }

        private static void SetTagIfAbsent(Activity activity, string key, string value)
        {
            if (!string.IsNullOrEmpty(value) && activity.GetTagItem(key) == null)
            {
                activity.SetTag(key, value);
            }
        }
    }
}
