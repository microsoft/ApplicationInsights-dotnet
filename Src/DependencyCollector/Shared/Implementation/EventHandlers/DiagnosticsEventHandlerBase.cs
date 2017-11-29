namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation.EventHandlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Base implementation of diagnostic event handler.
    /// </summary>
    internal abstract class DiagnosticsEventHandlerBase : IDiagnosticEventHandler
    {
        protected const string StatusPropertyName = "Status";

        protected readonly TelemetryClient TelemetryClient;

        // Every fetcher is unique for event payload and particular property into this payload
        // when we first receive an event and require particular property, we cache fetcher for it
        // There are just a few (~10) events we can receive and each will a have a few payload fetchers
        // I.e. this dictionary is quite small, does not grow up after service warm-up and does not require clean up
        private readonly ConcurrentDictionary<Property, PropertyFetcher> propertyFetchers = new ConcurrentDictionary<Property, PropertyFetcher>();

        protected DiagnosticsEventHandlerBase(TelemetryConfiguration configuration)
        {
            this.TelemetryClient = new TelemetryClient(configuration);
        }

        public virtual bool IsEventEnabled(string evnt, object arg1, object arg2)
        {
            return !evnt.EndsWith(TelemetryDiagnosticSourceListener.ActivityStartNameSuffix, StringComparison.Ordinal);
        }

        public abstract void OnEvent(KeyValuePair<string, object> evnt, DiagnosticListener ignored);

        protected void SetCommonProperties(string eventName, object eventPayload, Activity activity, OperationTelemetry telemetry)
        {
            telemetry.Name = this.GetOperationName(eventName, eventPayload, activity);
            telemetry.Duration = activity.Duration;
            telemetry.Timestamp = activity.StartTimeUtc;
            telemetry.Id = activity.Id;
            telemetry.Context.Operation.Id = activity.RootId;
            telemetry.Context.Operation.ParentId = activity.ParentId;

            foreach (var item in activity.Baggage)
            {
                if (!telemetry.Context.Properties.ContainsKey(item.Key))
                {
                    telemetry.Context.Properties[item.Key] = item.Value;
                }
            }

            foreach (var item in activity.Tags)
            {
                if (!telemetry.Context.Properties.ContainsKey(item.Key))
                {
                    telemetry.Context.Properties[item.Key] = item.Value;
                }
            }

            telemetry.Success = this.IsOperationSuccessful(eventName, eventPayload, activity);
        }

        protected virtual string GetOperationName(string eventName, object eventPayload, Activity activity)
        {
            // activity name looks like 'Microsoft.Azure.<...>.<Name>'
            // as namespace is too verbose, we'll just take the last node from the activity name as telemetry name
            string activityName = activity.OperationName;
            int lastDotIndex = activityName.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                return activityName.Substring(lastDotIndex + 1);
            }

            return activityName;
        }

        protected virtual bool IsOperationSuccessful(string eventName, object eventPayload, Activity activity)
        {
            return this.FetchPayloadProperty<TaskStatus>(eventName, StatusPropertyName, eventPayload) == TaskStatus.RanToCompletion;
        }

        protected T FetchPayloadProperty<T>(string eventName, string propertyName, object payload)
        {
            Property property = new Property(eventName, propertyName);
            var fetcher = this.propertyFetchers.GetOrAdd(property, prop => new PropertyFetcher(prop.PropertyName));
            return (T)fetcher.Fetch(payload);
        }

        private struct Property : IEquatable<Property>
        {
            public readonly string PropertyName;
            private readonly string eventName;

            public Property(string eventName, string propertyName)
            {
                this.eventName = eventName;
                this.PropertyName = propertyName;
            }

            public bool Equals(Property other)
            {
                return this.eventName == other.eventName && this.PropertyName == other.PropertyName;
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                {
                    return false;
                }

                return obj is Property && this.Equals((Property)obj);
            }

            public override int GetHashCode()
            {
                return this.eventName.GetHashCode() ^ this.PropertyName.GetHashCode();
            }
        }
    }
}
