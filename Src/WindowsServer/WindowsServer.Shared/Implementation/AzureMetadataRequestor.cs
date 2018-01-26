namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
#if NETSTANDARD1_3
    using System.Net.Http;
#endif
    using Extensibility;

    internal class AzureMetadataRequestor : IAzureMetadataRequestor
    {
        /// <summary>
        /// Azure Instance Metadata Service exists on a single non-routable IP on machines configured
        /// by the Azure Resource Manager. See <a href="https://go.microsoft.com/fwlink/?linkid=864683">to learn more.</a>
        /// </summary>
        private static string baseImdsUrl = $"http://169.254.169.254/metadata/instance/compute";
        private static string imdsApiVersion = $"api-version=2017-08-01"; // this version has the format=text capability
        private static string imdsTextFormat = "format=text";
        private static int MAX_IMS_RESPONSE_BUFFER_SIZE = 256;

        /// <summary>
        /// Gets the value of a specific field from the IMS link asynchronously, and returns it.
        /// </summary>
        /// <param name="fieldName">Specific IMS field to retrieve a value for.</param>
        /// <returns>An array of field names available, or null.</returns>
        public async Task<string> GetAzureComputeMetadata(string fieldName)
        {
            string metadataRequestUrl = $"{baseImdsUrl}/{fieldName}?{imdsTextFormat}&{imdsApiVersion}";

            string requestResult = await this.MakeAzureMetadataRequest(metadataRequestUrl);

            return requestResult;
        }

        /// <summary>
        /// Gets all the available fields from the IMS link asynchronously, and returns them as an IEnumerable.
        /// </summary>
        /// <returns>An array of field names available, or null.</returns>
        public async Task<IEnumerable<string>> GetAzureInstanceMetadataComputeFields()
        {
            string allFieldsResponse = string.Empty;
            string metadataRequestUrl = $"{baseImdsUrl}?{imdsTextFormat}&{imdsApiVersion}";

            allFieldsResponse = await this.MakeAzureMetadataRequest(metadataRequestUrl);

            string[] fields = allFieldsResponse?.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            if (fields == null || fields.Length <= 0)
            {
                WindowsServerEventSource.Log.CannotObtainAzureInstanceMetadata();
            }

            return fields;
        }

        private async Task<string> MakeAzureMetadataRequest(string metadataRequestUrl)
        {
            string requestResult = string.Empty;

            SdkInternalOperationsMonitor.Enter();
            try
            {
#if NETSTANDARD1_3

                using (var getFieldValueClient = new HttpClient(new HttpClientHandler() { MaxRequestContentBufferSize = MAX_IMS_RESPONSE_BUFFER_SIZE }))
                {
                    getFieldValueClient.DefaultRequestHeaders.Add("Metadata", "True");
                    requestResult = await getFieldValueClient.GetStringAsync(metadataRequestUrl).ConfigureAwait(false);
                }

#elif NET45 || NET46

                WebRequest request = WebRequest.Create(metadataRequestUrl);
                request.Method = "GET";
                request.Headers.Add("Metadata", "true");
                using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
                {
                    if (response is HttpWebResponse)
                    {
                        var httpResponse = (HttpWebResponse)response;
                        if (httpResponse.StatusCode == HttpStatusCode.OK)
                        {
                            char[] buffer = new char[MAX_IMS_RESPONSE_BUFFER_SIZE];
                            StreamReader content = new StreamReader(httpResponse.GetResponseStream());
                            {
                                int bufferIndex = await content.ReadAsync(buffer, 0, buffer.Length);
                                // this will probably never exceed 1024 bytes returned, scrap anything else that is left
                                if (bufferIndex < buffer.Length - 1)
                                {
                                    requestResult = buffer.ToString();
                                }
                                else
                                {
                                    WindowsServerEventSource.Log.AzureInstanceMetadataRequestFailure(metadataRequestUrl, "Content received from Azure Metadata Instance service exceeds expected size, failing the call to avoid potential attack", string.Empty);
                                }
                            }
                        }
                    }
                }
#else
#error Unknown framework
#endif
            }
            catch (Exception ex)
            {
                WindowsServerEventSource.Log.AzureInstanceMetadataRequestFailure(metadataRequestUrl, ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
            }
            finally
            {
                SdkInternalOperationsMonitor.Exit();
            }

            return requestResult;
        }
    }
}
