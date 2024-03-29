
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : Envelope_types.cs
//
// Changes to this file may cause incorrect behavior and will be lost when
// the code is regenerated.
// <auto-generated />
//------------------------------------------------------------------------------


// suppress "Missing XML comment for publicly visible type or member"
#pragma warning disable 1591


#region ReSharper warnings
// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable RedundantUsingDirective
#endregion

namespace AI
{
    using System.Collections.Generic;

    // [global::Bond.Attribute("Description", "System variables for a telemetry item.")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class Envelope
    {
        // [global::Bond.Attribute("Description", "Envelope version. For internal use only. By assigning this the default, it will not be serialized within the payload unless changed to a value other than #1.")]
        // [global::Bond.Attribute("Name", "SchemaVersion")]
        // [global::Bond.Id(10)]
        public int ver { get; set; }

        // [global::Bond.Attribute("Description", "Type name of telemetry data item.")]
        // [global::Bond.Attribute("Name", "DataTypeName")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(20), global::Bond.Required]
        public string name { get; set; }

        // [global::Bond.Attribute("Description", "Event date time when telemetry item was created. This is the wall clock time on the client when the event was generated. There is no guarantee that the client's time is accurate. This field must be formatted in UTC ISO 8601 format, with a trailing 'Z' character, as described publicly on https://en.wikipedia.org/wiki/ISO_8601#UTC. Note: the number of decimal seconds digits provided are variable (and unspecified). Consumers should handle this, i.e. managed code consumers should not use format 'O' for parsing as it specifies a fixed length. Example: 2009-06-15T13:45:30.0000000Z.")]
        // [global::Bond.Attribute("Name", "DateTime")]
        // [global::Bond.Attribute("CSType", "DateTimeOffset")]
        // [global::Bond.Attribute("JSType", "Date")]
        // [global::Bond.Attribute("HockeyAppMinDateOffsetFromNow", "2592000000")]
        // [global::Bond.Attribute("MinDateOffsetFromNow", "172800000")]
        // [global::Bond.Attribute("MaxDateOffsetFromNow", "7200000")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(30), global::Bond.Required]
        public string time { get; set; }

        // [global::Bond.Attribute("Name", "SamplingRate")]
        // [global::Bond.Attribute("Description", "Sampling rate used in application. This telemetry item represents 1 / sampleRate actual telemetry items.")]
        // [global::Bond.Id(40)]
        public double sampleRate { get; set; }

        // [global::Bond.Attribute("Description", "Sequence field used to track absolute order of uploaded events.")]
        // [global::Bond.Attribute("Name", "SequenceNumber")]
        // [global::Bond.Attribute("MaxStringLength", "64")]
        // [global::Bond.Id(50)]
        public string seq { get; set; }

        // [global::Bond.Attribute("Description", "The application's instrumentation key. The key is typically represented as a GUID, but there are cases when it is not a guid. No code should rely on iKey being a GUID. Instrumentation key is case insensitive.")]
        // [global::Bond.Attribute("Name", "InstrumentationKey")]
        // [global::Bond.Attribute("MaxStringLength", "40")]
        // [global::Bond.Id(60)]
        public string iKey { get; set; }

        // [global::Bond.Attribute("Description", "A collection of values bit-packed to represent how the event was processed. Currently represents whether IP address needs to be stripped out from event (set 0x200000) or should be preserved.")]
        // [global::Bond.Attribute("Name", "TelemetryProperties")]
        // [global::Bond.Id(70)]
        public long flags { get; set; }

        // [global::Bond.Attribute("Name", "Tags")]
        // [global::Bond.Attribute("TypeAlias", "ContextTagKeys")]
        // [global::Bond.Attribute("Description", "Key/value collection of context properties. See ContextTagKeys for information on available properties.")]
        // [global::Bond.Id(500), global::Bond.Type(typeof(Dictionary<string, string>))]
        public IDictionary<string, string> tags { get; set; }

        // [global::Bond.Attribute("Name", "TelemetryData")]
        // [global::Bond.Attribute("Description", "Telemetry data item.")]
        // [global::Bond.Id(999)]
        public Base data { get; set; }

        public Envelope()
            : this("AI.Envelope", "Envelope")
        {}

        protected Envelope(string fullName, string name)
        {
            ver = 1;
            this.name = "";
            time = "";
            sampleRate = 100.0;
            seq = "";
            iKey = "";
            tags = new Dictionary<string, string>();
            data = new Base();
        }
    }
} // AI
