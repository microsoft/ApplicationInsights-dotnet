namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    internal struct MonitoringDataPoint
    {
        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public string InstrumentationKey { get; set; }

        [DataMember]
        public string Instance { get; set; }

        [DataMember]
        public string StreamId { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public MetricPoint[] Metrics { get; set; }
    }
}