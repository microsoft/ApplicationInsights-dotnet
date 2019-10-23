namespace E2ETestApp
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;

    internal class DiagnosticsEventListener : EventListener
    {
        private const long AllKeyword = -1;
        private readonly EventLevel logLevel;

        public DiagnosticsEventListener(EventLevel logLevel)
        {
            this.logLevel = logLevel;

            string MyDirectoryPath = "c:\\mylogs";
            string filename = "logs.txt";
            if (!Directory.Exists(MyDirectoryPath))
            {
                Directory.CreateDirectory(MyDirectoryPath);
            }
            var target = Path.Combine(MyDirectoryPath, filename);
            File.AppendAllText(target, "Starting..." + DateTime.UtcNow.ToLongTimeString());
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventSourceEvent)
        {
            if (eventSourceEvent == null)
            {
                return;
            }

            string message = (eventSourceEvent.Payload != null && eventSourceEvent.Message != null) ?
                string.Format(CultureInfo.CurrentCulture, eventSourceEvent.Message, eventSourceEvent.Payload.ToArray()) :
                eventSourceEvent.Message;

            WriteToFile(DateTime.UtcNow.ToLongTimeString() + " " + message+ "\n");            
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void WriteToFile(string data)
        {
            string MyDirectoryPath = "c:\\mylogs";
            string filename = "logs.txt";
            if (!Directory.Exists(MyDirectoryPath))
            {
                Directory.CreateDirectory(MyDirectoryPath);
            }

            var target = Path.Combine(MyDirectoryPath, filename);

            File.AppendAllText(target, data);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.StartsWith("Microsoft-ApplicationInsights-Extensibility-Dependency", StringComparison.Ordinal))
            {
                this.EnableEvents(eventSource, this.logLevel, (EventKeywords)AllKeyword);
            }

            base.OnEventSourceCreated(eventSource);
        }
    }
}