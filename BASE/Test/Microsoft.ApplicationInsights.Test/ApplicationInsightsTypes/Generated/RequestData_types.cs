
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : RequestData_types.cs
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

    // [global::Bond.Attribute("Description", "An instance of Request represents completion of an external request to the application to do work and contains a summary of that request execution and the results.")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class RequestData
        : Domain
    {
        // [global::Bond.Attribute("Description", "Schema version")]
        // [global::Bond.Id(10), global::Bond.Required]
        public int ver { get; set; }

        // [global::Bond.Attribute("MaxStringLength", "128")]
        // [global::Bond.Attribute("Description", "Identifier of a request call instance. Used for correlation between request and other telemetry items.")]
        // [global::Bond.Id(20), global::Bond.Required]
        public string id { get; set; }

        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Attribute("Description", "Source of the request. Examples are the instrumentation key of the caller or the ip address of the caller.")]
        // [global::Bond.Id(29)]
        public string source { get; set; }

        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Attribute("Description", "Name of the request. Represents code path taken to process request. Low cardinality value to allow better grouping of requests. For HTTP requests it represents the HTTP method and URL path template like 'GET /values/{id}'.")]
        // [global::Bond.Id(30)]
        public string name { get; set; }

        // [global::Bond.Attribute("CSType", "TimeSpan")]
        // [global::Bond.Attribute("Description", "Request duration in format: DD.HH:MM:SS.MMMMMM. Must be less than 1000 days.")]
        // [global::Bond.Id(50), global::Bond.Required]
        public string duration { get; set; }

        // [global::Bond.Attribute("MaxStringLength", "1024")]
        // [global::Bond.Attribute("Description", "Result of a request execution. HTTP status code for HTTP requests.")]
        // [global::Bond.Id(60), global::Bond.Required]
        public string responseCode { get; set; }

        // [global::Bond.Attribute("Description", "Indication of successfull or unsuccessfull call.")]
        // [global::Bond.Id(70), global::Bond.Required]
        public bool success { get; set; }

        // [global::Bond.Attribute("MaxStringLength", "2048")]
        // [global::Bond.Attribute("Description", "Request URL with all query string parameters.")]
        // [global::Bond.Id(90)]
        public string url { get; set; }

        // [global::Bond.Attribute("Description", "Collection of custom properties.")]
        // [global::Bond.Attribute("MaxKeyLength", "150")]
        // [global::Bond.Attribute("MaxValueLength", "8192")]
        // [global::Bond.Id(100), global::Bond.Type(typeof(Dictionary<string, string>))]
        public IDictionary<string, string> properties { get; set; }

        // [global::Bond.Attribute("Description", "Collection of custom measurements.")]
        // [global::Bond.Attribute("MaxKeyLength", "150")]
        // [global::Bond.Id(200), global::Bond.Type(typeof(Dictionary<string, double>))]
        public IDictionary<string, double> measurements { get; set; }

        public RequestData()
            : this("AI.RequestData", "RequestData")
        {}

        protected RequestData(string fullName, string name)
        {
            ver = 2;
            id = "";
            source = "";
            this.name = "";
            duration = "";
            responseCode = "";
            success = true;
            url = "";
            properties = new Dictionary<string, string>();
            measurements = new Dictionary<string, double>();
        }
    }
} // AI
