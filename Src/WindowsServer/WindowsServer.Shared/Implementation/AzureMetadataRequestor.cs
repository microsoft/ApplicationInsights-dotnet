namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Runtime.Serialization.Json;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation.DataContracts;

    internal class AzureMetadataRequestor : IAzureMetadataRequestor
    {
        /// <summary>
        /// Azure Instance Metadata Service exists on a single non-routable IP on machines configured
        /// by the Azure Resource Manager. See <a href="https://go.microsoft.com/fwlink/?linkid=864683">to learn more.</a>
        /// </summary>
        internal const string AzureImsApiVersion = "api-version=2017-08-01"; // this version has the format=text capability
        internal const string AzureImsJsonFormat = "format=json";
        internal const int AzureImsMaxResponseBufferSize = 512;

        /// <summary>
        /// Default timeout for the web requests made to obtain Azure IMS data. Internal to expose to tests.
        /// </summary>
        internal TimeSpan AzureImsRequestTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Private function for mocking out the actual call to IMS in unit tests. Available to internal only.
        /// </summary>
        /// parameter sent to the func is a string representing the Uri to request Azure IMS data from.
        /// <returns>An instance of AzureInstanceComputeMetadata or null.</returns>
        private Func<string, Task<AzureInstanceComputeMetadata>> azureIMSRequestor = null;

        internal AzureMetadataRequestor(Func<string, Task<AzureInstanceComputeMetadata>> makeAzureIMSRequestor = null)
        {
            this.azureIMSRequestor = makeAzureIMSRequestor;
        }

        /// <summary>
        /// Gets or sets the base URI for the Azure Instance Metadata service. Internal to allow overriding in test.
        /// </summary>
        internal string BaseAimsUri { get; set; } = "http://169.254.169.254/metadata/instance/compute";

        public Task<AzureInstanceComputeMetadata> GetAzureComputeMetadataAsync()
        {
            string metadataRequestUrl = string.Format(
                CultureInfo.InvariantCulture,
                "{0}?{1}&{2}",
                this.BaseAimsUri,
                AzureMetadataRequestor.AzureImsJsonFormat,
                AzureMetadataRequestor.AzureImsApiVersion);

            return this.MakeAzureMetadataRequestAsync(metadataRequestUrl);
        }

        private async Task<AzureInstanceComputeMetadata> MakeAzureMetadataRequestAsync(string metadataRequestUrl)
        {
            AzureInstanceComputeMetadata requestResult = null;

            SdkInternalOperationsMonitor.Enter();
            try
            {
                if (this.azureIMSRequestor != null)
                {
                    requestResult = await this.azureIMSRequestor(metadataRequestUrl).ConfigureAwait(false);
                }
                else
                {
                    requestResult = await this.MakeWebRequestAsync(metadataRequestUrl).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                WindowsServerEventSource.Log.AzureInstanceMetadataRequestFailure(
                    metadataRequestUrl, ex.Message, ex.InnerException != null ? ex.InnerException.Message : string.Empty);
            }
            finally
            {
                SdkInternalOperationsMonitor.Exit();
            }

            return requestResult;
        }
        
        private async Task<AzureInstanceComputeMetadata> MakeWebRequestAsync(string requestUrl)
        {
            AzureInstanceComputeMetadata azureIms = null;
            DataContractJsonSerializer deserializer = new DataContractJsonSerializer(typeof(AzureInstanceComputeMetadata));

            using (var azureImsClient = new HttpClient())
            {
                azureImsClient.MaxResponseContentBufferSize = AzureMetadataRequestor.AzureImsMaxResponseBufferSize;
                azureImsClient.DefaultRequestHeaders.Add("Metadata", "True");
                azureImsClient.Timeout = this.AzureImsRequestTimeout;

                Stream content = await azureImsClient.GetStreamAsync(requestUrl).ConfigureAwait(false);
                azureIms = (AzureInstanceComputeMetadata)deserializer.ReadObject(content);
                content.Dispose();

                if (azureIms == null)
                {
                    WindowsServerEventSource.Log.CannotObtainAzureInstanceMetadata();
                }
            }

            return azureIms;
        }
    }
}
