namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    using System;

    /// <summary>@ToDo: Complete documentation before stable release. {447}</summary>
    public enum MetricAggregationCycleKind : Int32
    {
        /// <summary>
        /// The default aggregation cycle.
        /// </summary>
        Default,

        /// <summary>
        /// The aggregation cycle used by QuickPulse
        /// </summary>
        QuickPulse,

        /// <summary>
        /// The custom aggregation cycle.
        /// </summary>
        Custom
    }
}
