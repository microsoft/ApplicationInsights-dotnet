namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal interface IAzureMetadataRequestor
    {
        Task<IEnumerable<string>> GetAzureInstanceMetadataComputeFields();

        Task<string> GetAzureComputeMetadata(string fieldName);
    }
}
