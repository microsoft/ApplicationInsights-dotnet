namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;

    internal class HealthHeartbeatDefaultPayload
    {
        public const string UpdatedFieldsPropertyKey = "updatedFields";

        public static readonly string[] DefaultFields =
        {
            "runtimeFramework",
            "baseSdkTargetFramework",
            UpdatedFieldsPropertyKey
        };

        private List<string> enabledProperties;

        public HealthHeartbeatDefaultPayload() : this(null)
        {
        }

        public HealthHeartbeatDefaultPayload(IEnumerable<string> disableFields)
        {
            this.SetEnabledProperties(disableFields);
        }

        public IDictionary<string, HealthHeartbeatPropertyPayload> GetPayloadProperties()
        {
            var payload = new Dictionary<string, HealthHeartbeatPropertyPayload>();
            foreach (string fieldName in this.enabledProperties)
            {
                switch (fieldName)
                {
                    case "runtimeFramework":
                        payload.Add(fieldName, new HealthHeartbeatPropertyPayload()
                        {
                            IsHealthy = true,
                            PayloadValue = this.GetRuntimeFrameworkVer()
                        });
                        break;
                    case "baseSdkTargetFramework":
                        payload.Add(fieldName, new HealthHeartbeatPropertyPayload()
                        {
                            IsHealthy = true,
                            PayloadValue = this.GetTargetFrameworkVer()
                        });
                        break;
                    case UpdatedFieldsPropertyKey:
                        var updatedFieldItem = new HealthHeartbeatPropertyPayload()
                        {
                            IsHealthy = true,
                            PayloadValue = string.Empty
                        };
                        updatedFieldItem.IsUpdated = false; // always set this to false
                        payload.Add(fieldName, updatedFieldItem);
                        break;
                    default:
                        payload.Add(fieldName, new HealthHeartbeatPropertyPayload()
                        {
                            IsHealthy = false,
                            PayloadValue = "UNDEFINED"
                        });
                        break;
                }
            }

            return payload;
        }

        private void SetEnabledProperties(IEnumerable<string> disabledFields)
        {
            if (disabledFields == null || disabledFields.Count() <= 0)
            {
                this.enabledProperties = DefaultFields.ToList();
            }
            else
            {
                this.enabledProperties = new List<string>();
                foreach (string fieldName in DefaultFields)
                {
                    if (!disabledFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                    {
                        this.enabledProperties.Add(fieldName);
                    }
                }
            }
        }

        private string GetTargetFrameworkVer()
        {
#if NET45
            return "net45";
#elif NET46
            return "net46";
#elif NETSTANDARD1_3
            return "netstandard1.3";
#else
#error Unrecognized framework
            return "undefined";
#endif
        }

        private string GetRuntimeFrameworkVer()
        {
            // taken from https://github.com/Azure/azure-sdk-for-net/blob/f097add680f37908995fa2fbf6b7c73f11652ec7/src/SdkCommon/ClientRuntime/ClientRuntime/ServiceClient.cs#L214
            try
            {
                Assembly assembly = typeof(Object).GetTypeInfo().Assembly;

                AssemblyFileVersionAttribute objectAssemblyFileVer =
                            assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
                                    .Cast<AssemblyFileVersionAttribute>()
                                    .FirstOrDefault();

                return objectAssemblyFileVer != null ? objectAssemblyFileVer.Version : "undefined";
            }
            catch (Exception ex)
            {
                CoreEventSource.Log.LogError("GetRuntimeFrameworkVer did not obtain the current runtime framework version due to exception: " + ex.ToInvariantString());
            }

            return "undefined";
        }
    }
}
