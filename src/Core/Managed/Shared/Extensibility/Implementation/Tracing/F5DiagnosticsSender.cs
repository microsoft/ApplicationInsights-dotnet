namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System.Globalization;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Platform;

    /// <summary>
    /// This class is responsible for sending diagnostics information into VS debug output
    /// for F5 experience.
    /// </summary>
    internal class F5DiagnosticsSender : IDiagnosticsSender
    {
        /// <summary>
        /// VS debug output.
        /// </summary>
        protected readonly IDebugOutput debugOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="F5DiagnosticsSender"/> class. 
        /// </summary>
        public F5DiagnosticsSender()
        {
            this.debugOutput = PlatformSingleton.Current.GetDebugOutput();
        }

        public void Send(TraceEvent eventData)
        {
            if (this.debugOutput.IsLogging())
            {
                if (eventData.MetaData != null && !string.IsNullOrEmpty(eventData.MetaData.MessageFormat))
                {
                    var message = eventData.Payload != null && eventData.Payload.Length > 0 ?
                        string.Format(CultureInfo.InvariantCulture, eventData.MetaData.MessageFormat, eventData.Payload) :
                        eventData.MetaData.MessageFormat;

                    this.debugOutput.WriteLine(message);
                }
            }
        }
    }
}
