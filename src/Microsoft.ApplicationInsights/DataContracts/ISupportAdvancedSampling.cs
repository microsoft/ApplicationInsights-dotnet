namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;

    /// <summary>
    /// Possible item types for sampling evaluation
    /// </summary>
    [Flags]
    public enum SamplingTelemetryItemTypes
    {
        /// <summary>
        /// Unknown Telemetry Item Type
        /// </summary>
        None = 0,

        /// <summary>
        /// Event Telemetry type
        /// </summary>
        Event = 1,

        /// <summary>
        /// Exception Telemetry type
        /// </summary>
        Exception = 2,

        /// <summary>
        /// Message Telemetry type
        /// </summary>
        Message = 4,

        /// <summary>
        /// Metric Telemetry type
        /// </summary>
        Metric = 8,

        /// <summary>
        /// PageView Telemetry type
        /// </summary>
        PageView = 16,

        /// <summary>
        /// PageViewPerformance Telemetry type
        /// </summary>
        PageViewPerformance = 32,

        /// <summary>
        /// PerformanceCounter Telemetry type
        /// </summary>
        PerformanceCounter = 64,

        /// <summary>
        /// RemoteDependency Telemetry type
        /// </summary>
        RemoteDependency = 128,

        /// <summary>
        /// Request Telemetry type
        /// </summary>
        Request = 256,

        /// <summary>
        /// SessionState Telemetry type
        /// </summary>
        SessionState = 512,

        /// <summary>
        /// Availability Telemetry type
        /// </summary>
        Availability = 1024,
    }

    /// <summary>
    /// Represent objects that support  advanced sampling features.
    /// </summary>
    public interface ISupportAdvancedSampling
    {
        /// <summary>
        /// Gets os sets the flag indicating item's telemetry type to consider in sampling evaluation
        /// </summary>
        SamplingTelemetryItemTypes ItemTypeFlag { get; }

        /// <summary>
        /// Gets or sets a value indicating whether item can be sampled out proactively
        /// </summary>
        bool SupportsProactiveSampling { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether item was proactively sampled out
        /// </summary>
        bool IsProactivelySampledOut { get; set; }
    }
}