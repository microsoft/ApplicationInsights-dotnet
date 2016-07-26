namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    internal interface ITelemetryDocument
    {
        string Version { get; set; }

        TelemetryDocumentType DocumentType { get; }
    }
}
