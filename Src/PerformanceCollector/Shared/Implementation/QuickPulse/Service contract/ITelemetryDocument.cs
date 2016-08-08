namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    internal interface ITelemetryDocument
    {
        string Version { get; }

        string DocumentType { get; }
    }
}
