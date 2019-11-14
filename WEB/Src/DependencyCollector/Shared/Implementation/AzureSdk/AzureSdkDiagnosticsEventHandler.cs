using Microsoft.ApplicationInsights.DataContracts;

namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.EventHandlers;
    using Microsoft.ApplicationInsights.DependencyCollector.Implementation.Operation;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;

    internal class AzureSdkDiagnosticsEventHandler : DiagnosticsEventHandlerBase
    {
        private readonly ObjectInstanceBasedOperationHolder operationHolder = new ObjectInstanceBasedOperationHolder();

        public AzureSdkDiagnosticsEventHandler(TelemetryConfiguration configuration):base(configuration)
        {
        }

        public override bool IsEventEnabled(string evnt, object arg1, object arg2)
        {
            return true;
        }

        public override void OnEvent(KeyValuePair<string, object> evnt, DiagnosticListener diagnosticListener)
        {
            try
            {
                var currentActivity = Activity.Current;
                if (evnt.Key.EndsWith(".Start"))
                {
                    var telemetry = new DependencyTelemetry();
                    
                    SetCommonProperties(evnt.Key, evnt.Value, currentActivity, telemetry);

                    telemetry.Properties["DiagnosticSource"] = diagnosticListener.Name;
                    telemetry.Properties["Activity"] = currentActivity.OperationName;

                    operationHolder.Store(currentActivity, Tuple.Create(telemetry, /* isCustomCreated: */ false));
                }
                if (evnt.Key.EndsWith(".Stop"))
                {
                    var telemetry = operationHolder.Get(currentActivity).Item1;
                    telemetry.Duration = currentActivity.Duration;
                    TelemetryClient.TrackDependency(telemetry);
                }
                else if (evnt.Key.EndsWith(".Exception"))
                {
                    Exception ex = evnt.Value as Exception;

                    var telemetry = operationHolder.Get(currentActivity);
                    telemetry.Item1.Success = false;
                    if (ex != null)
                    {
                        telemetry.Item1.Data = ex.ToInvariantString();
                    }
                }
            }
            catch (Exception ex)
            {
                DependencyCollectorEventSource.Log.TelemetryDiagnosticSourceCallbackException(evnt.Key, ex.ToInvariantString());
            }
        }

        protected override string GetOperationName(string eventName, object eventPayload, Activity activity)
        {
            // activity name looks like 'Azure.<...>.<Class>.<Name>'
            // as namespace is too verbose, we'll just take the last two nodes from the activity name as telemetry name
            string activityName = activity.OperationName;
            int methodDotIndex = activityName.LastIndexOf('.');
            if (methodDotIndex <= 0)
            {
                return activityName;
            }

            int classDotIndex = activityName.LastIndexOf('.', methodDotIndex - 1);

            if (classDotIndex == -1)
            {
                return activityName;
            }

            return activityName.Substring(classDotIndex + 1, activityName.Length - classDotIndex - 1);
        }

        protected override bool IsOperationSuccessful(string eventName, object eventPayload, Activity activity)
        {
            return true;
        }
    }
}
