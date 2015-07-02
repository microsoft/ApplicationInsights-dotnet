namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    
    /// <summary>
    /// EventSource implementation for Silverlight.
    /// </summary>
    internal class EventSource : IDisposable
    {
        private IDictionary<int, EventMetaData> metadataCollection;

        private DiagnosticsListener eventListener;

        public EventSource()
        {
            try
            {
                this.InitializeMetadataCollection();
            }
            catch (Exception)
            {
#if DEBUG
                throw;
#endif
            }
        }

        public void EnableEventListener(DiagnosticsListener listener)
        {
            this.eventListener = listener;
        }

        public void Dispose()
        {
            // Forced to have to emulate event source
        }

        protected void WriteEvent(int eventId, params object[] parameters)
        {
            if (this.eventListener != null)
            {
                if (this.metadataCollection != null && this.metadataCollection.ContainsKey(eventId))
                {
                    var traceEvent = new TraceEvent
                    {
                        MetaData = this.metadataCollection[eventId],
                        Payload = parameters
                    };

                    this.eventListener.WriteEvent(traceEvent);
                }
#if DEBUG
                else
                {
                    throw new InvalidDataException("EventId is not in metadata collection: " + eventId);
                }
#endif
            }
        }

        private void InitializeMetadataCollection()
        {
            var publicMethodsInfos = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            if (publicMethodsInfos.Length > 0)
            {
                this.metadataCollection = new Dictionary<int, EventMetaData>(publicMethodsInfos.Length);

                foreach (var publicMethodsInfo in publicMethodsInfos)
                {
                    var attributes = publicMethodsInfo.GetCustomAttributes(typeof(EventAttribute), false).ToArray();

                    if (attributes.Length == 1)
                    {
                        var attribute = (EventAttribute)attributes[0];

                        var metadata = new EventMetaData
                        {
                            EventId = attribute.EventId,
                            Keywords = (long)attribute.Keywords,
                            Level = attribute.Level,
                            MessageFormat = attribute.Message
                        };

                        if (!this.metadataCollection.ContainsKey(metadata.EventId))
                        {
                            this.metadataCollection.Add(metadata.EventId, metadata);
                        }
#if DEBUG
                        else
                        {
                            throw new InvalidDataException("Multiple event methods with same id: " + metadata.EventId);
                        }
#endif
                    }
                }
            }
        }
    }
}
