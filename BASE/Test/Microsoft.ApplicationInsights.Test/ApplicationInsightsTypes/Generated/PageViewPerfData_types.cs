
//------------------------------------------------------------------------------
// This code was generated by a tool.
//
//   Tool : Bond Compiler 0.10.1.0
//   File : PageViewPerfData_types.cs
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

    // [global::Bond.Attribute("Description", "An instance of PageViewPerf represents: a page view with no performance data, a page view with performance data, or just the performance data of an earlier page request.")]
    // [global::Bond.Attribute("Alias", "PageViewPerformanceData;PageviewPerformanceData")]
    // [global::Bond.Schema]
    [System.CodeDom.Compiler.GeneratedCode("gbc", "0.10.1.0")]
    public partial class PageViewPerfData
        : PageViewData
    {
        // [global::Bond.Attribute("Description", "Performance total in TimeSpan 'G' (general long) format: d:hh:mm:ss.fffffff")]
        // [global::Bond.Attribute("CSType", "TimeSpan")]
        // [global::Bond.Id(10)]
        public string perfTotal { get; set; }

        // [global::Bond.Attribute("Description", "Network connection time in TimeSpan 'G' (general long) format: d:hh:mm:ss.fffffff")]
        // [global::Bond.Attribute("CSType", "TimeSpan")]
        // [global::Bond.Id(20)]
        public string networkConnect { get; set; }

        // [global::Bond.Attribute("Description", "Sent request time in TimeSpan 'G' (general long) format: d:hh:mm:ss.fffffff")]
        // [global::Bond.Attribute("CSType", "TimeSpan")]
        // [global::Bond.Id(30)]
        public string sentRequest { get; set; }

        // [global::Bond.Attribute("Description", "Received response time in TimeSpan 'G' (general long) format: d:hh:mm:ss.fffffff")]
        // [global::Bond.Attribute("CSType", "TimeSpan")]
        // [global::Bond.Id(40)]
        public string receivedResponse { get; set; }

        // [global::Bond.Attribute("Description", "DOM processing time in TimeSpan 'G' (general long) format: d:hh:mm:ss.fffffff")]
        // [global::Bond.Attribute("CSType", "TimeSpan")]
        // [global::Bond.Id(50)]
        public string domProcessing { get; set; }

        public PageViewPerfData()
            : this("AI.PageViewPerfData", "PageViewPerfData")
        {}

        protected PageViewPerfData(string fullName, string name)
        {
            perfTotal = "";
            networkConnect = "";
            sentRequest = "";
            receivedResponse = "";
            domProcessing = "";
        }
    }
} // AI
