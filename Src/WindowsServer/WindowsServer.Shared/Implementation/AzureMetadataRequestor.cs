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
        /// Private function for mocking out the actual call to IMS in unit tests. Available to internal only.
        /// </summary>
        private Func<string, Task<string>> azureIMSRequestor = null;

        private string baseImdsUri = $"http://169.254.169.254/metadata/instance/compute";

        /// <summary>
        /// Azure Instance Metadata Service exists on a single non-routable IP on machines configured
        /// by the Azure Resource Manager. See <a href="https://go.microsoft.com/fwlink/?linkid=864683">to learn more.</a>
        /// </summary>
        internal static string imdsApiVersion = $"api-version=2017-08-01"; // this version has the format=text capability
        internal static string imdsTextFormat = "format=text";
        internal static int maxImsResponseBufferSize = 256;
        

        public AzureMetadataRequestor()
        {
        }

        /// <summary>
        /// Base URI for the Azure Instance Metadata service. Internal to allow overriding in test.
        /// </summary>
        internal string BaseAimsUri
        {
            get => baseImdsUri;
            set => baseImdsUri = value;
        }

        internal AzureMetadataRequestor(Func<string, Task<string>> makeAzureIMSRequestor = null)
        {
            this.azureIMSRequestor = makeAzureIMSRequestor;
        }

        /// <summary>
        /// Gets the value of a specific field from the IMS link asynchronously, and returns it.
        /// </summary>
        /// <param name="fieldName">Specific IMS field to retrieve a value for.</param>
        /// <returns>An array of field names available, or null.</returns>
        public async Task<string> GetAzureComputeMetadata(string fieldName)
        {
            string metadataRequestUrl = $"{this.BaseAimsUri}/{fieldName}?{imdsTextFormat}&{imdsApiVersion}";

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
            string metadataRequestUrl = $"{this.BaseAimsUri}?{imdsTextFormat}&{imdsApiVersion}";

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
                if (this.azureIMSRequestor != null)
                {
                    requestResult = await this.azureIMSRequestor(metadataRequestUrl);
                }
                else
                {
                    requestResult = await this.MakeWebRequest(metadataRequestUrl);
                }
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

        private async Task<string> MakeWebRequest(string requestUrl)
        {
            string requestResult = string.Empty;
            int bufferLengthReceived = 0;

#if NETSTANDARD1_3

            using (var getFieldValueClient = new HttpClient())
            {
                getFieldValueClient.MaxResponseContentBufferSize = AzureMetadataRequestor.maxImsResponseBufferSize;
                getFieldValueClient.DefaultRequestHeaders.Add("Metadata", "True");
                requestResult = await getFieldValueClient.GetStringAsync(requestUrl).ConfigureAwait(false);
                bufferLengthReceived = requestResult.Length;
            }

#elif NET45 || NET46

            WebRequest request = WebRequest.Create(requestUrl);
            request.Method = "GET";
            request.Headers.Add("Metadata", "True");
            using (WebResponse response = await request.GetResponseAsync().ConfigureAwait(false))
            {
                if (response is HttpWebResponse)
                {
                    var httpResponse = (HttpWebResponse)response;
                    if (httpResponse.StatusCode == HttpStatusCode.OK)
                    {
                        char[] buffer = new char[AzureMetadataRequestor.maxImsResponseBufferSize];
                        StreamReader content = new StreamReader(httpResponse.GetResponseStream());
                        {
                            int bufferIndex = await content.ReadAsync(buffer, 0, buffer.Length);
                            bufferLengthReceived = bufferIndex;

                            // this will probably never exceed the buffer's size in bytes returned, scrap anything else that is left
                            if (bufferIndex < buffer.Length - 1)
                            {
                                requestResult = buffer.ToString();
                            }
                        }
                    }
                }
            }
#else
#error Unknown framework
#endif
            if (bufferLengthReceived > AzureMetadataRequestor.maxImsResponseBufferSize - 1)
            {
                WindowsServerEventSource.Log.AzureInstanceMetadataRequestFailure(requestUrl, "Content received from Azure Metadata Instance service exceeds expected size, failing the call to avoid potential attack", string.Empty);
                requestResult = string.Empty;
            }

            return requestResult;
        }
    }
}
