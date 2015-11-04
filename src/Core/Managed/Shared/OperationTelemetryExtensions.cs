namespace Microsoft.ApplicationInsights
{
    using System;
    using System.ComponentModel;
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
            var startTime = OperationWatch.Timestamp(OperationWatch.ElapsedTicks);
            telemetry.StartTime = startTime;
            telemetry.Timestamp = startTime;
        }

        /// <summary>
        /// An extension method to telemetry item that stops the timer and computes the duration of the request or dependency.
        /// </summary>
        /// <param name="telemetry">Telemetry item object that calls this extension method.</param>
        public static void Stop(this OperationTelemetry telemetry)
        {
            if (telemetry.StartTime != DateTimeOffset.MinValue)
            {
                telemetry.Duration = OperationWatch.Timestamp(OperationWatch.ElapsedTicks) - telemetry.StartTime;
            }
            else
            {
                telemetry.Timestamp = OperationWatch.Timestamp(OperationWatch.ElapsedTicks);
                telemetry.StartTime = telemetry.Timestamp;
                telemetry.Duration = TimeSpan.Zero;
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
