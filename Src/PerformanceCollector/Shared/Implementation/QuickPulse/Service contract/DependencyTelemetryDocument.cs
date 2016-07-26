namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal struct DependencyTelemetryDocument : ITelemetryDocument
    {
        [DataMember]
        public string Version { get; set; }
        
        [DataMember]
        public DateTimeOffset Timestamp { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public DateTimeOffset StartTime { get; set; }

        [DataMember]
        public bool? Success { get; set; }

        [DataMember]
        public TimeSpan Duration { get; set; }

        [DataMember]
        public string Sequence { get; set; }

        [DataMember]
        public string ResultCode { get; set; }

        [DataMember]
        public string CommandName { get; set; }

        [DataMember]
        public string DependencyTypeName { get; set; }

        [DataMember]
        public string DependencyKind { get; set; }

        public TelemetryDocumentType DocumentType
        {
            get
            {
                return TelemetryDocumentType.RemoteDependency;
            }
        }
    }
}
