namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal interface IAzureMetadataRequestor
    {
        Task<IEnumerable<string>> GetAzureInstanceMetadataComputeFields();

        Task<string> GetAzureComputeMetadata(string fieldName);
    }
}
