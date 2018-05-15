namespace Microsoft.ApplicationInsights.WindowsServer.Channel.Implementation
{
    using System;

    /// <summary>
    /// Container for all the settings applicable to the process of dynamically estimating 
    /// application telemetry sampling percentage.
    /// </summary>
    /// <remarks>
    /// This class exists to resolve a backwards compatibility issue introduced in 2.5.0.
    /// TODO: REMOVE THIS CLASS WHEN WE RELEASE NEW MAJOR VERSION: 3.0.0
    /// For more information see: https://github.com/Microsoft/ApplicationInsights-dotnet/issues/727
    /// </remarks>
    public class SamplingPercentageEstimatorSettings : Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation.SamplingPercentageEstimatorSettings
    {
    }
}
