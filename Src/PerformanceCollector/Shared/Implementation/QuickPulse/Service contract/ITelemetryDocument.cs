namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System.Runtime.Serialization;

    using Microsoft.ApplicationInsights.DataContracts;

    internal interface ITelemetryDocument
    {
        string Version { get; set; }
    }
}
