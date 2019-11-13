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
                    var telemetry = operationHolder.Get(currentActivity);

                    TelemetryClient.TrackDependency(telemetry.Item1);
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

        protected override bool IsOperationSuccessful(string eventName, object eventPayload, Activity activity)
        {
            return true;
        }
    }
}
