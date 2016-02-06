namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    /// <summary>
    /// Service client for QPS service.
    /// </summary>
    internal sealed class QuickPulseServiceClient : IQuickPulseServiceClient
    {
        private Uri serviceUri;

        public QuickPulseServiceClient(Uri serviceUri)
        {
            this.serviceUri = serviceUri;
        }
        
        public bool Ping()
        {
            return false;
        }

        public bool SubmitSample(QuickPulseDataSample sample)
        {
            return false;
        }
    }
}