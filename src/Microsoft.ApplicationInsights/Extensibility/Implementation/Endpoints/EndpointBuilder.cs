namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Endpoints
{
    using System;

    internal class EndpointBuilder // : UriBuilder
    {
        public string Location { get; set; } 
        public string Prefix { get; set; }
        public string Host { get; set; }

        public Uri ToUri()
        {
            //TODO: TEST FOR DUPLICATE "."



            // <location>.<prefix>.<suffix>
            // https:// westus2.dc.applicationinsights.azure.cn/
            return null;
        }
    }
}
