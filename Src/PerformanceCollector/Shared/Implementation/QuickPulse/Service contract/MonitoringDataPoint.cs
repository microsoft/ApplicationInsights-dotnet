namespace Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    [KnownType(typeof(RequestTelemetryDocument))]
    [KnownType(typeof(DependencyTelemetryDocument))]
    [KnownType(typeof(ExceptionTelemetryDocument))]
    internal struct MonitoringDataPoint
    {
        /*
         * 3 - adding TopCpuProcesses
        */
        public const int CurrentInvariantVersion = 3;

        [DataMember]
        public string Version { get; set; }

        [DataMember]
        public int InvariantVersion { get; set; }

        [DataMember]
        public string InstrumentationKey { get; set; }

        [DataMember]
        public string Instance { get; set; }

        [DataMember]
        public string StreamId { get; set; }

        [DataMember]
        public string MachineName { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public bool IsWebApp { get; set; }

        [DataMember]
        public MetricPoint[] Metrics { get; set; }

        [DataMember]
        public ITelemetryDocument[] Documents { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public ProcessCpuData[] TopCpuProcesses { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public bool TopCpuDataAccessDenied { get; set; }
    }
}