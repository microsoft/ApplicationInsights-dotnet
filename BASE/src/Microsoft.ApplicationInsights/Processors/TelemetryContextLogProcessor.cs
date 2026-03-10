namespace Microsoft.ApplicationInsights.Processors
{
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;
    using OpenTelemetry;
    using OpenTelemetry.Logs;

    /// <summary>
    /// A log processor that applies client-level <see cref="TelemetryContext"/> properties
    /// to all log records as attributes, using skip-if-present semantics.
    /// This ensures context attributes are applied universally — to Track* calls
    /// and any <see cref="Microsoft.Extensions.Logging.ILogger"/> calls from customer code.
    /// </summary>
    internal sealed class TelemetryContextLogProcessor : BaseProcessor<LogRecord>
    {
        private readonly TelemetryContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryContextLogProcessor"/> class.
        /// </summary>
        /// <param name="context">The client-level <see cref="TelemetryContext"/> to apply.</param>
        public TelemetryContextLogProcessor(TelemetryContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Called when a log record ends. Applies client-level context attributes using
        /// skip-if-present semantics: if an attribute key is already present in the log record,
        /// it is not overwritten. This matches the 2.x SDK's <c>Tags.CopyTagValue</c> precedence behavior.
        /// The Azure Monitor Exporter's <c>LogsHelper.ProcessLogRecordProperties</c> will pick up
        /// these attributes and route them into <c>LogContextInfo</c> for context tag mapping.
        /// </summary>
        /// <param name="logRecord">The log record being processed.</param>
        public override void OnEnd(LogRecord logRecord)
        {
            if (logRecord == null || this.context == null)
            {
                return;
            }

            // Collect existing attribute keys to check for presence
            var existingKeys = new HashSet<string>();
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    existingKeys.Add(attr.Key);
                }
            }

            // Build list of context attributes to add (only those not already present)
            var contextAttributes = new List<KeyValuePair<string, object>>();
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeEnduserPseudoId, this.context.User?.Id);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeEnduserId, this.context.User?.AuthenticatedUserId);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeMicrosoftOperationName, this.context.Operation?.Name);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeMicrosoftClientIp, this.context.Location?.Ip);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeMicrosoftSessionId, this.context.Session?.Id);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeAiDeviceId, this.context.Device?.Id);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeAiDeviceModel, this.context.Device?.Model);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeAiDeviceType, this.context.Device?.Type);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeAiDeviceOsVersion, this.context.Device?.OperatingSystem);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeMicrosoftSyntheticSource, this.context.Operation?.SyntheticSource);
            AddIfAbsent(contextAttributes, existingKeys, SemanticConventions.AttributeMicrosoftUserAccountId, this.context.User?.AccountId);

            if (contextAttributes.Count == 0)
            {
                base.OnEnd(logRecord);
                return;
            }

            // Merge original attributes with context attributes into a new list
            var mergedAttributes = new List<KeyValuePair<string, object>>(
                (logRecord.Attributes?.Count ?? 0) + contextAttributes.Count);

            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    mergedAttributes.Add(attr);
                }
            }

            mergedAttributes.AddRange(contextAttributes);
            logRecord.Attributes = mergedAttributes;

            base.OnEnd(logRecord);
        }

        private static void AddIfAbsent(
            List<KeyValuePair<string, object>> contextAttributes,
            HashSet<string> existingKeys,
            string key,
            string value)
        {
            if (!string.IsNullOrEmpty(value) && !existingKeys.Contains(key))
            {
                contextAttributes.Add(new KeyValuePair<string, object>(key, value));
            }
        }
    }
}
