namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Threading.Tasks;

    internal static class HeartbeatDefaultPayload
    {
        public static readonly string[] DefaultFields =
         {
            "runtimeFramework",
            "baseSdkTargetFramework",
            "osVersion",
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
            "vmSize",
            "processId"
        };

        private static string imdsRestApiVersion = "2017-04-02";
        private static string imdsServerIp = "169.254.169.254";
        private static string baseImdsUrl = $"http://{imdsServerIp}/metadata";
        private static string imdsApiVersion = $"api-version={imdsRestApiVersion}";
        private static string imdsMethodInstance = "instance";
        private static string imdsSubmethodCompute = "compute";
        private static string imdsTextFormat = "format=text";
        private static string imdsInstanceComputeBaseUrl = $"{baseImdsUrl}/{imdsMethodInstance}/{imdsSubmethodCompute}";

        /// <summary>
        /// Flag that will tell us wether or not Azure VM metadata has been attempted to be gathered or not.
        /// If this is true and AzureVmInstanceMetadata is empty/null then it's very likely we aren't on an
        /// Azure IaaS VM (don't try again!).
        /// </summary>
        private static bool hasAzureVmMetadataBeenGathered = false;

        public static void PopulateDefaultPayload(IEnumerable<string> disabledFields, IHeartbeatPropertyManager provider)
        {
            var enabledProperties = HeartbeatDefaultPayload.RemoveDisabledDefaultFields(disabledFields);

            Task.Factory.StartNew(async () => await HeartbeatDefaultPayload.AddAzureVmDetail(provider, enabledProperties).ConfigureAwait(false));

            var payload = new Dictionary<string, HeartbeatPropertyPayload>();
            foreach (string fieldName in enabledProperties)
            {
                try
                {
                    switch (fieldName)
                    {
                        case "runtimeFramework":
                            provider.AddHeartbeatProperty(fieldName, HeartbeatDefaultPayload.GetRuntimeFrameworkVer(), true);
                            break;
                        case "baseSdkTargetFramework":
                            provider.AddHeartbeatProperty(fieldName, HeartbeatDefaultPayload.GetBaseSdkTargetFramework(), true);
                            break;
                        case "osVersion":
                            provider.AddHeartbeatProperty(fieldName, HeartbeatDefaultPayload.GetRuntimeOsType(), true);
                            break;
                        default:
                            provider.AddHeartbeatProperty(fieldName, "UNDEFINED", false);
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
        private static async Task AddAzureVmDetail(IHeartbeatPropertyManager heartbeatManager, IEnumerable<string> enabledFields)
        {
            if (!HeartbeatDefaultPayload.hasAzureVmMetadataBeenGathered)
            {
                // only ever do this once when the SDK gets initialized
                HeartbeatDefaultPayload.hasAzureVmMetadataBeenGathered = true;

                try
                {
                    var allFields = await GetAzureInstanceMetadataFields(imdsInstanceComputeBaseUrl, imdsApiVersion, imdsTextFormat)
                                    .ConfigureAwait(false);

                    var enabledImdsFields = enabledFields.Intersect(allFields);
                    foreach (string field in enabledImdsFields)
                    {
                        heartbeatManager.AddHeartbeatProperty(field, "pending", true);
                    }

                    foreach (string field in enabledImdsFields)
                    {
                        heartbeatManager.SetHealthProperty(
                            field,
                            await GetAzureInstanceMetadaValue(imdsInstanceComputeBaseUrl, field, imdsApiVersion, imdsTextFormat)
                                .ConfigureAwait(false));
                    }
                }
                catch (AggregateException)
                {
                    CoreEventSource.Log.CannotObtainAzureInstanceMetadata();
                }
            }
        }

        private static async Task<IEnumerable<string>> GetAzureInstanceMetadataFields(string baseUrl, string apiVersion, string textFormatArg)
        {
            string allComputeFields = string.Empty;
            string allComputeFieldsUrl = $"{baseUrl}?{textFormatArg}&{apiVersion}";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Metadata", "True");
                allComputeFields = await httpClient.GetStringAsync(allComputeFieldsUrl).ConfigureAwait(false);
            }

            return allComputeFields.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        }

        private static async Task<string> GetAzureInstanceMetadaValue(string baseUrl, string fieldName, string apiVersion, string textFormatArg)
        { 
            string computFieldUrl = $"{baseUrl}/{fieldName}?{textFormatArg}&{apiVersion}";
            string fieldValue = string.Empty;

            using (var getFieldValueClient = new HttpClient())
            {
                getFieldValueClient.DefaultRequestHeaders.Add("Metadata", "True");
                fieldValue = await getFieldValueClient.GetStringAsync(computFieldUrl).ConfigureAwait(false);
            }

            return fieldValue;
        }

        private static List<string> RemoveDisabledDefaultFields(IEnumerable<string> disabledFields)
        {
            List<string> enabledProperties = new List<string>();

            if (disabledFields == null || disabledFields.Count() <= 0)
            {
                enabledProperties = DefaultFields.ToList();
            }
            else
            {
                enabledProperties = new List<string>();
                foreach (string fieldName in DefaultFields)
                {
                    if (!disabledFields.Contains(fieldName, StringComparer.OrdinalIgnoreCase))
                    {
                        enabledProperties.Add(fieldName);
                    }
                }
            }

            return enabledProperties;
        }

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
        /// <returns>String representing the OS, and any version/patch information reported by that OS</returns>
        private static string GetRuntimeOsType()
        {
#if NET45 || NET46
            return Environment.OSVersion.VersionString;
#elif NETSTANDARD1_3
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
#else
#error Unrecognized framework
            return "unknown";
#endif
        }

        /// <summary>
        /// Runtime information for the process ID currently running & consuming the SDK
        /// </summary>
        /// <returns>string ID of the current process' AppDomain</returns>
        private static string GetProcessId()
        {
            var assemblyName = new AssemblyName();

            return assemblyName.Name;
        }
    }
}
