namespace Microsoft.ApplicationInsights.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;

    internal static class TelemetryContextEnricher
    {
        internal static void ApplyToActivity(Activity activity, TelemetryContext context)
        {
            if (activity == null || context == null)
            {
                return;
            }

            var globalProperties = context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    SetTagIfAbsent(activity, kvp.Key, kvp.Value);
                }
            }

            SetTagIfAbsent(activity, SemanticConventions.AttributeEnduserPseudoId, context.User?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeEnduserId, context.User?.AuthenticatedUserId);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftOperationName, context.Operation?.Name);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftClientIp, context.Location?.Ip);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftSessionId, context.Session?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceId, context.Device?.Id);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceModel, context.Device?.Model);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceType, context.Device?.Type);
            SetTagIfAbsent(activity, SemanticConventions.AttributeAiDeviceOsVersion, context.Device?.OperatingSystem);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftSyntheticSource, context.Operation?.SyntheticSource);
            SetTagIfAbsent(activity, SemanticConventions.AttributeMicrosoftUserAccountId, context.User?.AccountId);
            SetTagIfAbsent(activity, SemanticConventions.AttributeUserAgentOriginal, context.User?.UserAgent);
        }

        internal static void ApplyToLogAttributes(IDictionary<string, string> attributes, TelemetryContext context)
        {
            if (attributes == null || context == null)
            {
                return;
            }

            var globalProperties = context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    AddAttributeIfAbsent(attributes, kvp.Key, kvp.Value);
                }
            }

            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeEnduserPseudoId, context.User?.Id);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeEnduserId, context.User?.AuthenticatedUserId);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeMicrosoftOperationName, context.Operation?.Name);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeMicrosoftClientIp, context.Location?.Ip);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeMicrosoftSessionId, context.Session?.Id);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeAiDeviceId, context.Device?.Id);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeAiDeviceModel, context.Device?.Model);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeAiDeviceType, context.Device?.Type);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeAiDeviceOsVersion, context.Device?.OperatingSystem);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeMicrosoftSyntheticSource, context.Operation?.SyntheticSource);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeMicrosoftUserAccountId, context.User?.AccountId);
            AddAttributeIfAbsent(attributes, SemanticConventions.AttributeUserAgentOriginal, context.User?.UserAgent);
        }

        internal static ContextSnapshot BuildSnapshot(TelemetryContext context)
        {
            if (context == null)
            {
                return new ContextSnapshot(Array.Empty<KeyValuePair<string, string>>());
            }

            var list = new List<KeyValuePair<string, string>>();

            var globalProperties = context.GlobalPropertiesValue;
            if (globalProperties != null)
            {
                foreach (var kvp in globalProperties)
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        list.Add(new KeyValuePair<string, string>(kvp.Key, kvp.Value));
                    }
                }
            }

            AddIfNotEmpty(list, SemanticConventions.AttributeEnduserPseudoId, context.User?.Id);
            AddIfNotEmpty(list, SemanticConventions.AttributeEnduserId, context.User?.AuthenticatedUserId);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftOperationName, context.Operation?.Name);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftClientIp, context.Location?.Ip);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftSessionId, context.Session?.Id);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceId, context.Device?.Id);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceModel, context.Device?.Model);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceType, context.Device?.Type);
            AddIfNotEmpty(list, SemanticConventions.AttributeAiDeviceOsVersion, context.Device?.OperatingSystem);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftSyntheticSource, context.Operation?.SyntheticSource);
            AddIfNotEmpty(list, SemanticConventions.AttributeMicrosoftUserAccountId, context.User?.AccountId);
            AddIfNotEmpty(list, SemanticConventions.AttributeUserAgentOriginal, context.User?.UserAgent);

            return new ContextSnapshot(list.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ApplySnapshotToActivity(Activity activity, ContextSnapshot snapshot)
        {
            if (activity == null || snapshot == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Tags.Length; i++)
            {
                ref readonly var kvp = ref snapshot.Tags[i];
                if (activity.GetTagItem(kvp.Key) == null)
                {
                    activity.SetTag(kvp.Key, kvp.Value);
                }
            }
        }

        internal static void ApplySnapshotToLogAttributes(IDictionary<string, string> attributes, ContextSnapshot snapshot)
        {
            if (attributes == null || snapshot == null)
            {
                return;
            }

            for (int i = 0; i < snapshot.Tags.Length; i++)
            {
                ref readonly var kvp = ref snapshot.Tags[i];
                if (!attributes.ContainsKey(kvp.Key))
                {
                    attributes.Add(kvp.Key, kvp.Value);
                }
            }
        }

        private static void AddAttributeIfAbsent(IDictionary<string, string> attributes, string key, string value)
        {
            if (!string.IsNullOrEmpty(value) && !attributes.ContainsKey(key))
            {
                attributes.Add(key, value);
            }
        }

        private static void AddIfNotEmpty(List<KeyValuePair<string, string>> list, string key, string value)
        {
            if (!string.IsNullOrEmpty(value) && !ContainsKey(list, key))
            {
                list.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        private static bool ContainsKey(List<KeyValuePair<string, string>> list, string key)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Key == key)
                {
                    return true;
                }
            }

            return false;
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
