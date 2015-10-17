namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
#if CORE_PCL || NET45 || WINRT || UWP || NET46
    using System.Diagnostics.Tracing;
#endif
    using System.Linq;
#if NET40 || NET35
    using Microsoft.Diagnostics.Tracing;
#endif

    /// <summary>
    /// Subscriber to ETW Event source events, which sends data to other Senders (F5 and Portal).
    /// </summary>
    internal class DiagnosticsListener : IDisposable
    {
        private readonly IList<IDiagnosticsSender> diagnosticsSenders = new List<IDiagnosticsSender>();
        private EventLevel logLevel = EventLevel.Error;
#if !Wp80
        private DiagnosticsEventListener eventListener;
#endif
        public DiagnosticsListener(IList<IDiagnosticsSender> senders)
        {
            if (senders == null || senders.Count < 1)
            {
                throw new ArgumentNullException("senders");
            }

            this.diagnosticsSenders = senders;
#if !Wp80
            this.eventListener = new DiagnosticsEventListener(this, this.LogLevel);
#endif
        }

        public EventLevel LogLevel
        {
            get
            {
                return this.logLevel;
            }

            set
            {
#if !Wp80
                if (this.LogLevel != value)
                {
                    var oldListener = this.eventListener;
                    this.eventListener = new DiagnosticsEventListener(this, value);
                    oldListener.Dispose();
                }
#endif
                this.logLevel = value;
            }
        }

        public void WriteEvent(TraceEvent eventData)
        {
            if (eventData.MetaData != null && eventData.MetaData.MessageFormat != null)
            {
                // check severity because it is not done in Silverlight EventSource implementation 
                if (eventData.MetaData.Level <= this.LogLevel)
                {
                    foreach (var sender in this.diagnosticsSenders)
                    {
                        sender.Send(eventData);
                    }
                }
            }
        }

        public void Dispose()
        {
#if !Wp80
            this.eventListener.Dispose();
#endif
        }
    }
}
