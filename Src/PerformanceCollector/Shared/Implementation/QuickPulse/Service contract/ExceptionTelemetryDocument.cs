namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System.Runtime.Serialization;

    [DataContract]
    internal struct ExceptionTelemetryDocument : ITelemetryDocument
    {
        [DataMember(EmitDefaultValue = false)]
        public string Version { get; set; }
        
        [DataMember(EmitDefaultValue = false)]
        public string Message { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string SeverityLevel { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string HandledAt { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Exception { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string DocumentType
        {
            get
            {
                return TelemetryDocumentType.Exception.ToString();
            }

            private set
            {
            }
        }
    }
}
