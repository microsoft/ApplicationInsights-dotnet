namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System;
    using System.Collections.Generic;

    internal interface ITelemetryDocument
    {
        Guid Id { get; }

        string Version { get; }
        
        KeyValuePair<string, string>[] Properties { get; set; }
        
        string DocumentType { get; }
    }
}
