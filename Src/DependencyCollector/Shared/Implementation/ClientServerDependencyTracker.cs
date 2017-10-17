namespace Microsoft.ApplicationInsights.DependencyCollector.Implementation
{
    using System;
    using System.Data.SqlClient;
#if NET45
    using System.Diagnostics;
#endif
    using System.Net;
    using Microsoft.ApplicationInsights.Common;
    using Microsoft.ApplicationInsights.DataContracts;

    internal static class ClientServerDependencyTracker
    {
        private const string DependencyActivityName = "Microsoft.AppInsights.Web.Dependency";

        /// <summary>
        /// Gets or sets a value indicating whether pretending the profiler is attached or not.
        /// </summary>
        internal static bool PretendProfilerIsAttached { get; set; } 

        /// <summary>
        /// The function that needs to be called before sending a request to the server. Creates and initializes dependency telemetry item.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client object to initialize the context of the telemetry item.</param>
        internal static DependencyTelemetry BeginTracking(TelemetryClient telemetryClient)
        {
            var telemetry = new DependencyTelemetry();
            telemetry.Start();
            Activity activity;
            Activity currentActivity = Activity.Current;

            // On .NET46 without profiler, outgoing requests are instrumented with reflection hook in DiagnosticSource 
            //// see https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.DiagnosticSource/src/System/Diagnostics/HttpHandlerDiagnosticListener.cs
            // it creates an Activity and injects standard 'Request-Id' and 'Correlation-Context' headers itself, so we should not start Activity in this case
            if (currentActivity != null && currentActivity.OperationName == "System.Net.Http.Desktop.HttpRequestOut")
            {
                activity = currentActivity;

                // OperationCorrelationTelemetryInitializer will initialize telemetry as a child of current activity:
                // But we need to initialize dependency telemetry from the current Activity:
                // Activity was created for this dependency in the Http Desktop DiagnosticSource
                var context = telemetry.Context;
                context.Operation.Id = currentActivity.RootId;
                context.Operation.ParentId = currentActivity.ParentId;
                foreach (var item in currentActivity.Baggage)
                {
                    if (!context.Properties.ContainsKey(item.Key))
                    {
                        context.Properties.Add(item);
                    }
                }

                telemetryClient.Initialize(telemetry);
            }
            else
            {
                telemetryClient.Initialize(telemetry);

                // Every operation must have its own Activity
                // if dependency is tracked with profiler of event source, we need to generate a proper hierarchical Id for it
                // in case of HTTP it will be propagated into the requert header.
                // So, we will create a new Activity for the dependency, jut to generate an Id.
                activity = new Activity(DependencyActivityName);

                // This is workaround for the issue https://github.com/Microsoft/ApplicationInsights-dotnet/issues/538
                // if there is no parent Activity, ID Activity generates is not random enough to work well with 
                // ApplicationInsights sampling algorithm
                // This code should go away when Activity is fixed: https://github.com/dotnet/corefx/issues/18418
                if (currentActivity == null)
                {
                    activity.SetParentId(telemetry.Id);
                }

                //// end of workaround

                activity.Start();
                activity.Stop();
            }

            // telemetry is initialized from current Activity (root and parent Id, but not the Id)
            telemetry.Id = activity.Id;

            // set operation root Id in case there was no parent activity (e.g. HttpRequest in background thread)
            if (string.IsNullOrEmpty(telemetry.Context.Operation.Id))
            {
                telemetry.Context.Operation.Id = activity.RootId;
            }

            PretendProfilerIsAttached = false;
            return telemetry;
        }

        /// <summary>
        /// Function that needs to be invoked after the request call to the sever. Computes the duration of the request and tracks the dependency telemetry
        /// item.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client object to track the telemetry item.</param>
        /// <param name="telemetry">Telemetry item to compute the duration and track.</param>
        internal static void EndTracking(TelemetryClient telemetryClient, DependencyTelemetry telemetry)
        {
            telemetry.Stop();
            telemetryClient.Track(telemetry);
        }

        /// <summary>
        /// Stops telemetry operation. Doesn't track the telemetry item.
        /// </summary>
        /// <param name="telemetry">Telemetry item to stop.</param>
        internal static void EndOperation(DependencyTelemetry telemetry)
        {
            telemetry.Stop();
        }

        /// <summary>
        /// Gets the tuple from either conditional weak table or cache (based on the framework for the input web request).
        /// </summary>
        /// <param name="webRequest">Target web request.</param>
        /// <returns>Tuple of dependency telemetry and a boolean that tells if the tuple is custom created or not.</returns>
        internal static Tuple<DependencyTelemetry, bool> GetTupleForWebDependencies(WebRequest webRequest)
        {
            if (webRequest == null)
            {
                throw new ArgumentNullException("webRequest");
            }

            Tuple<DependencyTelemetry, bool> telemetryTuple = null;

            if (DependencyTableStore.Instance.IsProfilerActivated || PretendProfilerIsAttached)
            {
                telemetryTuple = DependencyTableStore.Instance.WebRequestConditionalHolder.Get(webRequest);
            }
            else
            {
                telemetryTuple = DependencyTableStore.Instance.WebRequestCacheHolder.Get(GetIdForRequestObject(webRequest));
            }

            return telemetryTuple;
        }

        /// <summary>
        /// Adds the tuple to either conditional weak table or cache (based on the framework for the input web request).
        /// </summary>
        /// <param name="webRequest">Target web request.</param>
        /// <param name="telemetry">Dependency telemetry item to add to the table for the corresponding web request.</param>
        /// <param name="isCustomCreated">Boolean value that tells if the current telemetry item is being added by the customer or not.</param>
        internal static void AddTupleForWebDependencies(WebRequest webRequest, DependencyTelemetry telemetry, bool isCustomCreated)
        {
            if (webRequest == null)
            {
                throw new ArgumentNullException("webRequest");
            }

            if (telemetry == null)
            {
                throw new ArgumentNullException("telemetry");
            }

            var telemetryTuple = new Tuple<DependencyTelemetry, bool>(telemetry, isCustomCreated);
            if (DependencyTableStore.Instance.IsProfilerActivated || PretendProfilerIsAttached)
            {
                DependencyTableStore.Instance.WebRequestConditionalHolder.Store(webRequest, telemetryTuple);
            }
            else
            {
                DependencyTableStore.Instance.WebRequestCacheHolder.Store(GetIdForRequestObject(webRequest), telemetryTuple);
            }
        }

        /// <summary>
        /// Gets the tuple from either conditional weak table or cache (based on the framework for the input SQL request).
        /// </summary>
        /// <param name="sqlRequest">Target SQL request.</param>
        /// <returns>Tuple of dependency telemetry and a boolean that tells if the tuple is custom created or not.</returns>
        internal static Tuple<DependencyTelemetry, bool> GetTupleForSqlDependencies(SqlCommand sqlRequest)
        {
            if (sqlRequest == null)
            {
                throw new ArgumentNullException("webRequest");
            }

            Tuple<DependencyTelemetry, bool> telemetryTuple = null;

            if (DependencyTableStore.Instance.IsProfilerActivated || PretendProfilerIsAttached)
            {
                telemetryTuple = DependencyTableStore.Instance.SqlRequestConditionalHolder.Get(sqlRequest);
            }
            else
            {
                telemetryTuple = DependencyTableStore.Instance.SqlRequestCacheHolder.Get(GetIdForRequestObject(sqlRequest));
            }

            return telemetryTuple;
        }

        /// <summary>
        /// Adds the tuple to either conditional weak table or cache (based on the framework for the input SQL request).
        /// </summary>
        /// <param name="sqlRequest">Target SQL request.</param>
        /// <param name="telemetry">Dependency telemetry item to add to the table for the corresponding SQL request.</param>
        /// <param name="isCustomCreated">Boolean value that tells if the current telemetry item is being added by the customer or not.</param>
        internal static void AddTupleForSqlDependencies(SqlCommand sqlRequest, DependencyTelemetry telemetry, bool isCustomCreated)
        {
            if (sqlRequest == null)
            {
                throw new ArgumentNullException("webRequest");
            }

            if (telemetry == null)
            {
                throw new ArgumentNullException("telemetry");
            }

            var telemetryTuple = new Tuple<DependencyTelemetry, bool>(telemetry, isCustomCreated);
            if (DependencyTableStore.Instance.IsProfilerActivated || PretendProfilerIsAttached)
            {
                DependencyTableStore.Instance.SqlRequestConditionalHolder.Store(sqlRequest, telemetryTuple);
            }
            else
            {
                DependencyTableStore.Instance.SqlRequestCacheHolder.Store(GetIdForRequestObject(sqlRequest), telemetryTuple);
            }
        }

        internal static long GetIdForRequestObject(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return (long)obj.GetHashCode() + 9223372032559808512L;
        }
    }
}