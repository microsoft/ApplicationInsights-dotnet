namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System.Runtime.Serialization;

    [DataContract]
    internal struct ExceptionTelemetryDocument : ITelemetryDocument
    {
        [DataMember]
        public string Version { get; set; }
        
        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public string SeverityLevel { get; set; }

        [DataMember]
        public string HandledAt { get; set; }

        [DataMember]
        public string Exception { get; set; }

        public TelemetryDocumentType DocumentType
        {
            get
            {
                return TelemetryDocumentType.Exception;
            }
        }
    }
}
