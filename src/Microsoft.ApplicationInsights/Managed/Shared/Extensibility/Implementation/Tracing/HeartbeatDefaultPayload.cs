namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
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

        /// <summary>
        /// If this is an application running on an Azure IaaS VM, we have access to the local Azure Instance
        /// Metadata service. When this is true, we can get a few details that will help sort out any issues
        /// with a service based on the platform being run on. We only want to gather this information once,
        /// so upon trying this field will either be null or not, and the flag 'HasAzureVmMetadata' will be 
        /// set accordingly.
        /// </summary>
        public static string AzureVmInstanceMetadata = null;

        /// <summary>
        /// Flag that will tell us wether or not Azure VM metadata has been attempted to be gathered or not.
        /// If this is true and AzureVmInstanceMetadata is empty/null then it's very likely we aren't on an
        /// Azure IaaS VM (don't try again!).
        /// </summary>
        public static bool HasAzureVmMetadataBeenGathered = false;

        public static void GetPayloadProperties(IEnumerable<string> disabledFields, IHeartbeatProvider provider)
        {
            var enabledProperties = HeartbeatDefaultPayload.RemoveDisabledDefaultFields(disabledFields);

            var azureVmDetail = HeartbeatDefaultPayload.GatherAzureVmDetail().ConfigureAwait(false);
            
            var payload = new Dictionary<string, HeartbeatPropertyPayload>();
            foreach (string fieldName in enabledProperties)
            {
                try
                {
                    switch (fieldName)
                    {
                        case "runtimeFramework":
                            provider.AddHeartbeatProperty(fieldName, GetRuntimeFrameworkVer(), true);
                            break;
                        case "baseSdkTargetFramework":
                            provider.AddHeartbeatProperty(fieldName, GetBaseSdkTargetFramework(), true);
                            break;
                        case "osVersion":
                            provider.AddHealthProperty(fieldName, HeartbeatDefaultPayload.GetRuntimeOsType(), true);
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
        /// Gathers operational data from an Azure VM, if the endpoint is present. If not, and the SDK is
        /// not running on an Azure VM with the Azure Instance Metadata Service running, the corresponding
        /// fields will not get set into the heartbeat payload.
        /// </summary>
        private static async Task<string> GatherAzureVmDetail()
        {
            if (HeartbeatDefaultPayload.HasAzureVmMetadataBeenGathered)
            {
                return HeartbeatDefaultPayload.AzureVmInstanceMetadata;
            }

            const string api_version = "2017-04-02";
            const string imds_server = "169.254.169.254";

            string imdsUri = "http://" + imds_server + "/metadata" + "/instance" + "?api-version=" + api_version;
            string jsonResult = string.Empty;

#if NET45 || NET46
            var request = WebRequest.Create(imdsUri);
            request.Method = "POST";
            request.ContentType = JsonSerializer.ContentType;
            request.Headers[HttpRequestHeader.ContentEncoding] = JsonSerializer.ContentType;
            Stream requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false);

            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            {
                HeartbeatDefaultPayload.HasAzureVmMetadataBeenGathered = true;
                HttpWebResponseWrapper wrapper = null;

                if (response is HttpWebResponse httpResponse)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        wrapper = new HttpWebResponseWrapper
                        {
                            StatusCode = (int)httpResponse.StatusCode,
                            StatusDescription = httpResponse.StatusDescription
                        };

                        using (StreamReader content = new StreamReader(httpResponse.GetResponseStream()))
                        {
                            HeartbeatDefaultPayload.AzureVmInstanceMetadata = content.ReadToEnd();
                        }
                    }
                }

                return HeartbeatDefaultPayload.AzureVmInstanceMetadata;
            }
#else
            var request = new HttpRequestMessage(HttpMethod.Post, imdsUri);
            request.Content = new StreamContent(contentStream);
            if (!string.IsNullOrEmpty(this.ContentType))
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(this.ContentType);
            }

            if (!string.IsNullOrEmpty(this.ContentEncoding))
            {
                request.Content.Headers.Add(ContentEncodingHeader, this.ContentEncoding);
            }

            await this.client.SendAsync(request).ConfigureAwait(false);


            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Metadata", "True");

                try
                {
                    jsonResult = httpClient.GetStringAsync(imdsUri).Result;
                }
                catch (AggregateException ex)
                {
                    // handle response failures
                    Console.WriteLine("Request failed: " + ex.InnerException.Message);
                }
            }
#endif

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
    }
}
