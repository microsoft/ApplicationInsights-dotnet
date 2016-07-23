namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal struct RequestTelemetryDocument : ITelemetryDocument
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
        public string ResponseCode { get; set; }

        [DataMember]
        public Uri Url { get; set; }

        [DataMember]
        public string HttpMethod { get; set; }
    }
}
