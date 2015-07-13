namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal sealed class StubTelemetry : ITelemetry, ISupportProperties
    {
        public Action<IJsonWriter> OnSerialize = jsonWriter => { };

#if !Wp80 && !NET35
        public Action<object> OnSendEvent = eventSourceWriter => { };
#endif

        public StubTelemetry()
        {
            this.Context = new TelemetryContext();
            this.Properties = new Dictionary<string, string>();
        }

        public DateTimeOffset Timestamp { get; set; }

        public string Sequence { get; set; }

        public TelemetryContext Context { get; set; }

        public IDictionary<string, string> Properties { get; set; }

        public void Serialize(IJsonWriter writer)
        {
            this.OnSerialize(writer);
        }

        public void Sanitize()
        {
        }

#if !Wp80 && !NET35
        public void SendEvent(object writer)
        {
            this.OnSendEvent(writer);
        }
#endif
    }
}
