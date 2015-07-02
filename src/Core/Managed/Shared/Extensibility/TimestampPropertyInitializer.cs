namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// An <see cref="ITelemetryInitializer"/> that sets <see cref="ITelemetry.Timestamp"/> to <see cref="DateTimeOffset.Now"/>.
    /// </summary>
    public sealed class TimestampPropertyInitializer : ITelemetryInitializer
    {
        /// <summary>
        /// Sets <see cref="ITelemetry.Timestamp"/> to <see cref="DateTimeOffset.Now"/>.
        /// </summary>
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry.Timestamp == default(DateTimeOffset))
            {
                telemetry.Timestamp = Clock.Instance.Time;
            }
        }
    }
}
