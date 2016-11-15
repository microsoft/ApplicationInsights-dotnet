namespace Microsoft.ApplicationInsights
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Extension functions to operation telemetry that start and stop the timer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class OperationTelemetryExtensions
    {
        /// <summary>
        /// An extension to telemetry item that starts the timer for the the respective telemetry.
        /// </summary>
        /// <param name="telemetry">Telemetry item object that calls this extension method.</param>
        public static void Start(this OperationTelemetry telemetry)
        {            
            telemetry.Timestamp = DateTimeOffset.UtcNow;

            // Begin time is used internally for calculating duration of operation at the end call,
            // and hence is stored using higher precision Clock.
            // Stopwatch.GetTimestamp() is used (instead of ElapsedTicks) as it is thread-safe.
            telemetry.BeginTimeInTicks = Stopwatch.GetTimestamp();
        }

        /// <summary>
        /// An extension method to telemetry item that stops the timer and computes the duration of the request or dependency.
        /// </summary>
        /// <param name="telemetry">Telemetry item object that calls this extension method.</param>
        public static void Stop(this OperationTelemetry telemetry)
        {
            if (telemetry.BeginTimeInTicks != 0L)
            {
                long stopWatchTicksDiff = Stopwatch.GetTimestamp() - telemetry.BeginTimeInTicks;
                double durationInMillisecs = (stopWatchTicksDiff * 1000 / (double) Stopwatch.Frequency);
                telemetry.Duration = TimeSpan.FromMilliseconds(durationInMillisecs);
            }
            else
            {                
                telemetry.Duration = TimeSpan.Zero;
            }

            if(telemetry.Timestamp == DateTimeOffset.MinValue)
            {
                telemetry.Timestamp = DateTimeOffset.UtcNow;
            }            
        }

        /// <summary>
        /// Generate random operation Id and set it to OperationContext.
        /// </summary>
        /// <param name="telemetry">Telemetry to initialize Operation id for.</param>
        public static void GenerateOperationId(this OperationTelemetry telemetry)
        {
            telemetry.Id = Convert.ToBase64String(BitConverter.GetBytes(WeakConcurrentRandom.Instance.Next()));
        }
    }
}
