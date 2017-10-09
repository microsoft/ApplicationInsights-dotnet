namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Encapsulates application lifecycle events.
    /// </summary>
    public interface IApplicationLifecycle
    {
        /// <summary>
        /// Occurs when a new instance of the application is started or an existing instance is activated.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly", Justification = "EventArgs is not available on Windows Runtime")]
        event Action<object, object> Started;

        /// <summary>
        /// Occurs when the application is suspending or closing.
        /// </summary>
        event EventHandler<ApplicationStoppingEventArgs> Stopping;
    }
}
