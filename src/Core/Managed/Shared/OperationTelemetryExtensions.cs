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
            Start(telemetry, Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// An extension to telemetry item that initializes the timer for the the respective telemetry
        /// using a timestamp from a high-resolution <see cref="Stopwatch"/>.
        /// </summary>
        /// <param name="telemetry">Telemetry item object that calls this extension method.</param>
        /// <param name="timestamp">A high-resolution timestamp from <see cref="Stopwatch"/>.</param>
        public static void Start(this OperationTelemetry telemetry, long timestamp)
        {
            telemetry.Timestamp = DateTimeOffset.UtcNow;

            // Begin time is used internally for calculating duration of operation at the end call,
            // and hence is stored using higher precision Clock.
            // Stopwatch.GetTimestamp() is used (instead of ElapsedTicks) as it is thread-safe.
            telemetry.BeginTimeInTicks = timestamp;
        }

        /// <summary>
        /// An extension method to telemetry item that stops the timer and computes the duration of the request or dependency.
        /// </summary>
        /// <param name="telemetry">Telemetry item object that calls this extension method.</param>
        public static void Stop(this OperationTelemetry telemetry)
        {
            if (telemetry.BeginTimeInTicks != 0L)
            {
                StopImpl(telemetry, timestamp: Stopwatch.GetTimestamp());
            }
            else
            {
                StopImpl(telemetry, duration: TimeSpan.Zero);
            }
        }

        /// <summary>
        /// An extension method to telemetry item that stops the timer and computes the duration of the request or dependency.
        /// </summary>
        /// <param name="telemetry">Telemetry item object that calls this extension method.</param>
        /// <param name="timestamp">A high-resolution timestamp from <see cref="Stopwatch"/>.</param>
        public static void Stop(this OperationTelemetry telemetry, long timestamp)
        {
            if (telemetry.BeginTimeInTicks != 0L)
            {
                StopImpl(telemetry, timestamp);
            }
            else
            {
                StopImpl(telemetry, duration: TimeSpan.Zero);
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

        /// <summary>
        /// Set the duration given a timestamp from a high-resolution <see cref="Stopwatch"/>.
        /// </summary>
        /// <param name="telemetry">Telemetry item object to update.</param>
        /// <param name="timestamp">The high resolution timestamp.</param>
        private static void StopImpl(OperationTelemetry telemetry, long timestamp)
        {
            long stopWatchTicksDiff = timestamp - telemetry.BeginTimeInTicks;
            double durationInMillisecs = stopWatchTicksDiff * 1000 / (double)Stopwatch.Frequency;
            StopImpl(telemetry, TimeSpan.FromMilliseconds(durationInMillisecs));
        }

        /// <summary>
        /// Record the duration and, optionally, set the timestamp to the current time.
        /// </summary>
        /// <param name="telemetry">Telemetry item object to update.</param>
        /// <param name="duration">The duration of the operation.</param>
        private static void StopImpl(OperationTelemetry telemetry, TimeSpan duration)
        {
            telemetry.Duration = duration;

            if (telemetry.Timestamp == DateTimeOffset.MinValue)
            {
                telemetry.Timestamp = DateTimeOffset.UtcNow;
            }
        }
    }
}
