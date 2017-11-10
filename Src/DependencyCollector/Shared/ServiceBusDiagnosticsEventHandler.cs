namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Implements ServiceBus DiagnosticSource events handling.
    /// </summary>
    internal class ServiceBusDiagnosticsEventHandler : IDiagnosticEventHandler
    {
        public const string DiagnosticSourceName = "Microsoft.Azure.ServiceBus";
        private const string StatusPropertyName = "Status";
        private const string EntityPropertyName = "Entity";
        private const string EndpointPropertyName = "Endpoint";
        private const string ExceptionPropertyName = "Exception";

        // We want to reflect in the dependency type that it's one of the queue operations.
        // This way, UI could have special representation for all kinds of queues
        // Not knowing particular service
        private const string DependencyType = "Queue.ServiceBus";

        private readonly TelemetryClient telemetryClient;

        // Every fetcher is unique for event payload and particular property into this payload
        // when we first receive an event and require particular property, we cache fetcher for it
        // There are just a few (~10) events we can receive and each will a have a few payload fetchers
        // I.e. this dictionary is quite small, does not grow up after service warm-up and does not require clean up
        private readonly ConcurrentDictionary<Property, PropertyFetcher> propertyFetchers = new ConcurrentDictionary<Property, PropertyFetcher>();

        internal ServiceBusDiagnosticsEventHandler(TelemetryConfiguration configuration)
        {
            this.telemetryClient = new TelemetryClient(configuration);
        }

        public bool IsEventEnabled(string evnt, object arg1, object arg2)
        {
            return !evnt.EndsWith("Start", StringComparison.Ordinal);
        }

        public void OnEvent(KeyValuePair<string, object> evnt, DiagnosticListener ignored)
        {
            Activity currentActivity = Activity.Current;

            switch (evnt.Key)
            {
                case "Microsoft.Azure.ServiceBus.ProcessSession.Stop":
                case "Microsoft.Azure.ServiceBus.Process.Stop":
                    this.OnRequest(evnt.Key, evnt.Value, currentActivity);
                    break;
                case "Microsoft.Azure.ServiceBus.Exception":
                    this.OnException(evnt.Key, evnt.Value);
                    break;
                default:
                    this.OnDependency(evnt.Key, evnt.Value, currentActivity);
                    break;
            }
        }

        private void OnDependency(string name, object payload, Activity activity)
        {
            DependencyTelemetry telemetry = new DependencyTelemetry { Type = DependencyType };

            this.SetCommonProperties(name, payload, activity, telemetry);

            // Endpoint is URL of particular ServiceBus, e.g. sb://myservicebus.servicebus.windows.net/
            telemetry.Data = this.FetchPayloadProperty<Uri>(name, EndpointPropertyName, payload)?.ToString();

            // Queue/Topic name, e.g. myqueue/mytopic
            telemetry.Target = this.FetchPayloadProperty<string>(name, EntityPropertyName, payload);

            this.telemetryClient.TrackDependency(telemetry);
        }

        private void OnRequest(string name, object payload, Activity activity)
        {
            RequestTelemetry telemetry = new RequestTelemetry();

            this.SetCommonProperties(name, payload, activity, telemetry);

            // Endpoint is URL of particular ServiceBus, e.g. sb://myservicebus.servicebus.windows.net/
            telemetry.Url = this.FetchPayloadProperty<Uri>(name, EndpointPropertyName, payload);

            // Entity 
            // We want to make Source field extendable at the beginning as we may add
            // multi ikey support and also build some special UI for requests coming from the queues
            // so the Source looks like roleName:queueName (using the multi ikey schema).
            telemetry.Source = "roleName:" + this.FetchPayloadProperty<string>(name, EntityPropertyName, payload);

            this.telemetryClient.TrackRequest(telemetry);
        }

        private void OnException(string name, object payload)
        {
            Exception ex = this.FetchPayloadProperty<Exception>(name, ExceptionPropertyName, payload);

            this.telemetryClient.TrackException(ex);
        }

        private void SetCommonProperties(string eventName, object eventPayload, Activity activity, OperationTelemetry telemetry)
        {
            // activity name looks like 'Microsoft.Azure.ServiceBus.<Name>'
            // as namespace is too verbose, we'll just take the last node from the activity name as telemetry name
            string activityName = activity.OperationName;
            int lastDotIndex = activityName.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                telemetry.Name = activityName.Substring(lastDotIndex + 1);
            }

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

            telemetry.Success = this.FetchPayloadProperty<TaskStatus>(eventName, StatusPropertyName, eventPayload) == TaskStatus.RanToCompletion;
        }

        private T FetchPayloadProperty<T>(string eventName, string propertyName, object payload)
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
