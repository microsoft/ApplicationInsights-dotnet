namespace Microsoft.ApplicationInsights.Extensibility
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Represents the operation item that holds telemetry which is tracked on end request. Operation can be associated with either WEB or SQL dependencies.
    /// </summary>
    public interface IOperationHolder<T> : IDisposable where T : OperationTelemetry
    {
        /// <summary>
        /// Gets Telemetry item of interest that is created when StartOperation function of ClientExtensions is invoked.
        /// </summary>
        T Telemetry { get; }
    }
}
