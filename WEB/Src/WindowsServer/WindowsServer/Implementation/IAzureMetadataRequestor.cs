namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.WindowsServer.Implementation.DataContracts;

    internal interface IAzureMetadataRequestor : IDisposable
    {
        Task<AzureInstanceComputeMetadata> GetAzureComputeMetadataAsync();
    }
}
