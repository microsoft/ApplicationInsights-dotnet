namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    internal interface ITelemetryDocument
    {
        string Version { get; set; }

        string DocumentType { get; }
    }
}
