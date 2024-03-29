
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : EventData_types.cs
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

    // [global::Bond.Attribute("Description", "Instances of Event represent structured event records that can be grouped and searched by their properties. Event data item also creates a metric of event count by name.")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class EventData
        : Domain
    {
        // [global::Bond.Attribute("Description", "Schema version")]
        // [global::Bond.Id(10), global::Bond.Required]
        public int ver { get; set; }

        // [global::Bond.Attribute("MaxStringLength", "512")]
        // [global::Bond.Attribute("Description", "Event name. Keep it low cardinality to allow proper grouping and useful metrics.")]
        // [global::Bond.Attribute("Question", "Why Custom Event name is shorter than Request name or dependency name?")]
        // [global::Bond.Id(20), global::Bond.Required]
        public string name { get; set; }

        // [global::Bond.Attribute("Description", "Collection of custom properties.")]
        // [global::Bond.Attribute("MaxKeyLength", "150")]
        // [global::Bond.Attribute("MaxValueLength", "8192")]
        // [global::Bond.Id(100), global::Bond.Type(typeof(Dictionary<string, string>))]
        public IDictionary<string, string> properties { get; set; }

        // [global::Bond.Attribute("Description", "Collection of custom measurements.")]
        // [global::Bond.Attribute("MaxKeyLength", "150")]
        // [global::Bond.Id(200), global::Bond.Type(typeof(Dictionary<string, double>))]
        public IDictionary<string, double> measurements { get; set; }

        public EventData()
            : this("AI.EventData", "EventData")
        {}

        protected EventData(string fullName, string name)
        {
            ver = 2;
            this.name = "";
            properties = new Dictionary<string, string>();
            measurements = new Dictionary<string, double>();
        }
    }
} // AI
