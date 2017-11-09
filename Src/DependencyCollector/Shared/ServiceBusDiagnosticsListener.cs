namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Implements ServiceBus DiagnosticSource events handling.
    /// </summary>
    internal class ServiceBusDiagnosticsEventHandler
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
        private readonly Dictionary<string, Dictionary<string, PropertyFetcher>> propertyFetchers =
            new Dictionary<string, Dictionary<string, PropertyFetcher>>();

        internal ServiceBusDiagnosticsEventHandler(TelemetryConfiguration configuration)
        {
            this.telemetryClient = new TelemetryClient(configuration);
        }

        /// <summary>
        /// Gets custom handler <see cref="HandleDiagnosticsEvent{TContext}"/>delegate.
        /// </summary>
        internal HandleDiagnosticsEvent<HashSet<string>> EventHandler => this.HandleEvent;

        private void HandleEvent(KeyValuePair<string, object> evnt, DiagnosticListener diagnosticListener, HashSet<string> context)
        {
            // there is no guarantee IsEnabled was called in case there are multiple listeners in the system. i.e. we need to check it again
            if (evnt.Key.EndsWith("Start", StringComparison.Ordinal))
            {
                return;
            }

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

            PropertyFetcher urlFetcher = this.GetOrCreatePropertyFetcher(name, EndpointPropertyName);

            // Endpoint is URL of particular ServiceBus, e.g. sb://myservicebus.servicebus.windows.net/
            telemetry.Data = ((Uri)urlFetcher.Fetch(payload)).ToString();

            PropertyFetcher entityFetcher = this.GetOrCreatePropertyFetcher(name, EntityPropertyName);

            // Queue/Topic name, e.g. myqueue/mytopic
            telemetry.Target = (string)entityFetcher.Fetch(payload);

            this.telemetryClient.TrackDependency(telemetry);
        }

        private void OnRequest(string name, object payload, Activity activity)
        {
            RequestTelemetry telemetry = new RequestTelemetry();

            this.SetCommonProperties(name, payload, activity, telemetry);

            PropertyFetcher urlFetcher = this.GetOrCreatePropertyFetcher(name, EndpointPropertyName);

            // Endpoint is URL of particular ServiceBus, e.g. sb://myservicebus.servicebus.windows.net/
            telemetry.Url = (Uri)urlFetcher.Fetch(payload);

            PropertyFetcher entityFetcher = this.GetOrCreatePropertyFetcher(name, EntityPropertyName);
            
            // We want to make Source field extendable at the beginning as we may add
            // multi ikey support and also build some special UI for requests coming from the queues.
            telemetry.Source = "roleName:" + (string)entityFetcher.Fetch(payload);

            this.telemetryClient.TrackRequest(telemetry);
        }

        private void OnException(string name, object payload)
        {
            PropertyFetcher exceptionFetcher = this.GetOrCreatePropertyFetcher(name, ExceptionPropertyName);
            Exception ex = (Exception)exceptionFetcher.Fetch(payload);

            this.telemetryClient.TrackException(ex);
        }

        private void SetCommonProperties(string eventName, object eventPayload, Activity activity, OperationTelemetry telemetry)
        {
            // activity name looks like 'Microsoft.Azure.ServiceBus.<Name>'
            // as namespace is too verbose, we'll just take the last node from the activity name as telemetry name
            string[] activityNameSegments = activity.OperationName.Split('.');
            if (activityNameSegments.Length > 0)
            {
                telemetry.Name = activityNameSegments[activityNameSegments.Length - 1];
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

            PropertyFetcher statusFetcher = this.GetOrCreatePropertyFetcher(eventName, StatusPropertyName);
            telemetry.Success = (TaskStatus)statusFetcher.Fetch(eventPayload) == TaskStatus.RanToCompletion;
        }

        private PropertyFetcher GetOrCreatePropertyFetcher(string eventName, string propertyName)
        {
            if (!this.propertyFetchers.TryGetValue(eventName, out Dictionary<string, PropertyFetcher> fetchers))
            {
                fetchers = new Dictionary<string, PropertyFetcher>();
                this.propertyFetchers.Add(eventName, fetchers);
            }

            if (!fetchers.TryGetValue(propertyName, out PropertyFetcher fetcher))
            {
                fetcher = new PropertyFetcher(propertyName);
                fetchers.Add(propertyName, fetcher);
            }

            return fetcher;
        }
    }
}
