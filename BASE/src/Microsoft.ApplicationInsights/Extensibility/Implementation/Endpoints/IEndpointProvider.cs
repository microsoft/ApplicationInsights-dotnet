namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    internal interface IEndpointProvider
    {
        string ConnectionString { get; set; }

        string GetAADAudience();

        Uri GetEndpoint(EndpointName endpointName);

        string GetInstrumentationKey();
    }
}
