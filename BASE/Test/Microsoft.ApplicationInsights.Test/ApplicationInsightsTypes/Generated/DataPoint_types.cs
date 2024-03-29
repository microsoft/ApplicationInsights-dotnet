
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : DataPoint_types.cs
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

    // [global::Bond.Attribute("Description", "Metric data single measurement.")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class DataPoint
    {
        // [global::Bond.Attribute("Description", "Namespace of the metric.")]
        // [global::Bond.Attribute("MaxStringLength", "256")]
        // [global::Bond.Id(5)]
        public string ns { get; set; }

        // [global::Bond.Attribute("Description", "Name of the metric.")]
        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Id(10), global::Bond.Required]
        public string name { get; set; }

        // [global::Bond.Attribute("Description", "Metric type. Single measurement or the aggregated value.")]
        // [global::Bond.Id(20)]
        public DataPointType kind { get; set; }

        // [global::Bond.Attribute("Description", "Single value for measurement. Sum of individual measurements for the aggregation.")]
        // [global::Bond.Id(30), global::Bond.Required]
        public double value { get; set; }

        // [global::Bond.Attribute("Description", "Metric weight of the aggregated metric. Should not be set for a measurement.")]
        // [global::Bond.Id(40), global::Bond.Type(typeof(global::Bond.Tag.nullable<int>))]
        public int? count { get; set; }

        // [global::Bond.Attribute("Description", "Minimum value of the aggregated metric. Should not be set for a measurement.")]
        // [global::Bond.Id(50), global::Bond.Type(typeof(global::Bond.Tag.nullable<double>))]
        public double? min { get; set; }

        // [global::Bond.Attribute("Description", "Maximum value of the aggregated metric. Should not be set for a measurement.")]
        // [global::Bond.Id(60), global::Bond.Type(typeof(global::Bond.Tag.nullable<double>))]
        public double? max { get; set; }

        // [global::Bond.Attribute("Description", "Standard deviation of the aggregated metric. Should not be set for a measurement.")]
        // [global::Bond.Id(70), global::Bond.Type(typeof(global::Bond.Tag.nullable<double>))]
        public double? stdDev { get; set; }

        public DataPoint()
            : this("AI.DataPoint", "DataPoint")
        {}

        protected DataPoint(string fullName, string name)
        {
            ns = "";
            this.name = "";
            kind = DataPointType.Measurement;
        }
    }
} // AI
