namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System.Collections.Generic;

    internal interface ITelemetryDocument
    {
        string Version { get; }

        KeyValuePair<string, string>[] Properties { get; set; }
        
        string DocumentType { get; }
    }
}
