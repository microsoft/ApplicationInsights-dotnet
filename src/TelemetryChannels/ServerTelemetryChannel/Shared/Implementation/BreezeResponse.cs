namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System.Runtime.Serialization;

    [DataContract]
    internal class BreezeResponse
    {
        [DataMember(Name = "itemsReceived")]
        public int ItemsReceived { get; set; }

        [DataMember(Name = "itemsAccepted")]
        public int ItemsAccepted { get; set; }

        [DataMember(Name = "errors")]
        public Error[] Errors { get; set; }

        [DataContract]
        public class Error
        {
            [DataMember(Name = "index")]
            public int Index { get; set; }

            [DataMember(Name = "statusCode")]
            public int StatusCode { get; set; }

            [DataMember(Name = "message")]
            public string Message { get; set; }
        }
    }
}
