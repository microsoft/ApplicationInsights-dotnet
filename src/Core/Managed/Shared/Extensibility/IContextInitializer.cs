namespace Microsoft.ApplicationInsights.Extensibility
{
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;

    /// <summary>
    /// Represents an object that implements supporting logic for <see cref="TelemetryContext"/>.
    /// </summary>
    /// <remarks>
    /// One type of objects that support <see cref="TelemetryContext"/> is a telemetry source.
    /// A telemetry source can supply initial property values for a <see cref="TelemetryContext"/> object 
    /// during its construction or generate <see cref="ITelemetry"/> objects during its lifetime.
    /// </remarks>
    //// TODO: Decide whether ISupportContext serves multiple TelemetryContext instances or only one and who controls its lifetime.
    public interface IContextInitializer
    {
        /// <summary>
        /// Initializes the given <see cref="TelemetryContext"/>.
        /// </summary>
        void Initialize(TelemetryContext context);
    }
}
