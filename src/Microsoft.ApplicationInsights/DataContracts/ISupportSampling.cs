namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;

    /// <summary>
    /// Possible item types for sampling evaluation
    /// </summary>
    [Flags]
    public enum SamplingTelemetryItemTypes
    {
#pragma warning disable SA1602
#pragma warning disable CS1591
        None = 0,
        Event = 1,
        Exception = 2,
        Message = 4,
        Metric = 8,
        PageView = 16,
        PageViewPerformance = 32,
        PerformanceCounter = 64,
        RemoteDependency = 128,
        Request = 256,
        SessionState = 512,
        Availability = 1024,
#pragma warning restore CS1591
#pragma warning restore SA1602
    }

    /// <summary>
    /// Represent objects that support data sampling.
    /// </summary>
    public interface ISupportSampling
    {
        /// <summary>
        /// Gets or sets data sampling percentage (between 0 and 100).
        /// </summary>
        double? SamplingPercentage { get; set; }

        /// <summary>
        /// Gets os sets the flag indentifying item's telemetry type to consider in sampling evaluation
        /// </summary>
        SamplingTelemetryItemTypes ItemTypeFlag { get; }
    }
}