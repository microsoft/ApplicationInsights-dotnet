namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
#if NETSTANDARD1_3
    using System.Runtime.InteropServices;
#endif
    using System.Threading.Tasks;

    internal static class HeartbeatDefaultPayload
    {
        public static readonly string[] DefaultFields =
        {
            "runtimeFramework",
            "baseSdkTargetFramework",
            "osVersion"
        };

        public static readonly string[] DefaultOptionalFields =
        {
            "processSessionId",
            // the  following are Azure Instance Metadata fields
            "osType",
            "location",
            "name",
            "offer",
            "platformFaultDomain",
            "platformUpdateDomain",
            "publisher",
            "sku",
            "version",
            "vmId",
            "vmSize"
        };

        public static readonly string[] AllDefaultFields = DefaultFields.Union(DefaultOptionalFields).ToArray();

        /// <summary>
        /// Flags that will tell us whether or not Azure VM metadata has been attempted to be gathered or not, and
        /// if we should even attempt to look for it in the first place.
        /// </summary>
        private static bool isAzureMetadataCheckCompleted = false;

        /// <summary>
        /// A unique identifier that would help to indicate to the analytics when the current process session has
        /// restarted. 
        /// 
        /// <remarks>If a process is unstable and is being restared frequently, tracking this property
        /// in the heartbeat would help to identify this unstability.
        /// </remarks>
        /// </summary>
        private static Guid? uniqueProcessSessionId = null;

        public static void PopulateDefaultPayload(IEnumerable<string> disabledFields, HeartbeatProvider provider, IAzureMetadataRequestor metadataRequestor)
        {
            var enabledProperties = HeartbeatDefaultPayload.RemoveDisabledDefaultFields(disabledFields);

            if (metadataRequestor != null)
            {
                Task.Factory.StartNew(async () => await HeartbeatDefaultPayload.AddAzureVmDetail(provider, enabledProperties, metadataRequestor).ConfigureAwait(false));
            }

            foreach (string fieldName in enabledProperties)
            {
                // we don't need to report out any failure here, so keep this look within the Sdk Internal Operations as well
                try
                {
                    switch (fieldName)
                    {
                        case "runtimeFramework":
                            provider.AddHeartbeatProperty(fieldName, true, HeartbeatDefaultPayload.GetRuntimeFrameworkVer(), true);
                            break;
                        case "baseSdkTargetFramework":
                            provider.AddHeartbeatProperty(fieldName, true, HeartbeatDefaultPayload.GetBaseSdkTargetFramework(), true);
                            break;
                        case "osVersion":
                            provider.AddHeartbeatProperty(fieldName, true, HeartbeatDefaultPayload.GetRuntimeOsType(), true);
                            break;
                        case "processSessionId":
                            provider.AddHeartbeatProperty(fieldName, true, HeartbeatDefaultPayload.GetProcessSessionId(), true);
                            break;
                        case "osType":
                        case "location":
                        case "name":
                        case "offer":
                        case "platformFaultDomain":
                        case "platformUpdateDomain":
                        case "publisher":
                        case "sku":
                        case "version":
                        case "vmId":
                        case "vmSize":
                            // skip all Azure Instance Metadata fields
                            break;
                        default:
                            provider.AddHeartbeatProperty(fieldName, true, "UNDEFINED", true);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    CoreEventSource.Log.FailedToObtainDefaultHeartbeatProperty(fieldName, ex.ToString());
                }
            }
        }

        /// <summary>
        /// Gathers operational data from an Azure VM, if the IMDS endpoint is present. If not, and the SDK is
        /// not running on an Azure VM with the Azure Instance Metadata Service running, the corresponding
        /// fields will not get set into the heartbeat payload.
        /// </summary>
        private static async Task AddAzureVmDetail(HeartbeatProvider heartbeatManager, IEnumerable<string> enabledFields, IAzureMetadataRequestor metadataRequestor)
        {
            if (metadataRequestor != null && !HeartbeatDefaultPayload.isAzureMetadataCheckCompleted)
            {
                // only ever do this once when the SDK gets initialized
                HeartbeatDefaultPayload.isAzureMetadataCheckCompleted = true;

                var allFields = await metadataRequestor.GetAzureInstanceMetadataComputeFields()
                                .ConfigureAwait(false);

                var enabledImdsFields = enabledFields.Intersect(allFields);
                foreach (string field in enabledImdsFields)
                {
                    heartbeatManager.AddHeartbeatProperty(
                        propertyName: field,
                        propertyValue: await metadataRequestor.GetAzureComputeMetadata(field)
                            .ConfigureAwait(false),
                        isHealthy: true,
                        allowDefaultFields: true);
                }
            }
        }

        private static List<string> RemoveDisabledDefaultFields(IEnumerable<string> disabledFields)
        {
            List<string> enabledProperties = new List<string>();

            if (disabledFields == null || disabledFields.Count() <= 0)
            {
                enabledProperties = AllDefaultFields.ToList();
            }
            else
            {
                enabledProperties = new List<string>();
                foreach (string fieldName in AllDefaultFields)
                {
                    if (!disabledFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                    {
                        enabledProperties.Add(fieldName);
                    }
                }
            }

            return enabledProperties;
        }

        /// <summary>
        /// Returns the current target framework that the assembly was built against.
        /// </summary>
        /// <returns>standard string representing the target framework</returns>
        private static string GetBaseSdkTargetFramework()
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

        /// <summary>
        /// This will return the current running .NET framework version, based on the version of the assembly that owns
        /// the 'Object' type. The version number returned can be used to infer other things such as .NET Core / Standard.
        /// </summary>
        /// <returns>a string representing the version of the current .NET framework</returns>
        private static string GetRuntimeFrameworkVer()
        {
#if NET45 || NET46
            Assembly assembly = typeof(Object).GetTypeInfo().Assembly;

            AssemblyFileVersionAttribute objectAssemblyFileVer =
                        assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute))
                                .Cast<AssemblyFileVersionAttribute>()
                                .FirstOrDefault();
            return objectAssemblyFileVer != null ? objectAssemblyFileVer.Version : "undefined";
#elif NETSTANDARD1_3
            return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
#else
#error Unrecognized framework
            return "unknown";
#endif
        }

        /// <summary>
        /// Runtime information for the underlying OS, should include Linux information here as well.
        /// </summary>
        /// <returns>String representing the OS or 'unknown'</returns>
        private static string GetRuntimeOsType()
        {
            string osValue = "unknown";
#if NET45 || NET46

            osValue = Environment.OSVersion.Platform.ToString();

#elif NETSTANDARD1_3
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                osValue = OSPlatform.Linux.ToString();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                osValue = OSPlatform.OSX.ToString();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                osValue = OSPlatform.Windows.ToString();
            }
            else
            {
                osValue = RuntimeInformation.OSDescription ?? "unknown";
            }

#else
#error Unrecognized framework
#endif
            return osValue;
        }

        /// <summary>
        /// Return a unique process session identifier that will only be set once in the lifetime of a 
        /// single executable session.
        /// </summary>
        /// <returns>string representation of a unique id</returns>
        private static string GetProcessSessionId()
        {
            if (HeartbeatDefaultPayload.uniqueProcessSessionId == null)
            {
                HeartbeatDefaultPayload.uniqueProcessSessionId = Guid.NewGuid();
            }

            return HeartbeatDefaultPayload.uniqueProcessSessionId.ToString();
        }
    }
}
