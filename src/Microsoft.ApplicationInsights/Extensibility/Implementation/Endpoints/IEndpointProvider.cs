using System;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    internal interface IEndpointProvider
    {
        string ConnectionString { get; set; }

        Uri GetEndpoint(EndpointName endpointName);
    }
}