namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal static class ActivityExtensions
    {
        private const string OperationNameTag = "OperationName";

        private const int ActivityAvailabilityUnknown = 0;
        private const int ActivityAvailable = 1;
        private const int ActivityNotAvailable = 2;

        private static int activityTypeAvailability = ActivityAvailabilityUnknown;

        /// <summary>
        /// Executes action if Activity is available (DiagnosticSource DLL is available).
        /// Decorate all code that works with Activity with this method.
        /// </summary>
        /// <param name="action">Action to execute.</param>
        /// <returns>True if Activity is available, false otherwise.</returns>
        public static bool TryRun(Action action)
        {
            Debug.Assert(action != null, "Action must not be null");

            if (IsActivityTypeAvailable())
            {
                action.Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static string GetOperationName(this Activity activity)
        {
            Debug.Assert(activity != null, "Activity must not be null");
            return activity.Tags.FirstOrDefault(tag => tag.Key == OperationNameTag).Value;
        }

        internal static void SetOperationName(this Activity activity, string operationName)
        {
            Debug.Assert(!string.IsNullOrEmpty(operationName), "OperationName must not be null or empty");
            Debug.Assert(activity != null, "Activity must not be null");
            activity.AddTag(OperationNameTag, operationName);
        }

        private static bool IsActivityTypeAvailable()
        {
            if (activityTypeAvailability == ActivityAvailable)
            {
                // Fast common case
                return true;
            }

            if (activityTypeAvailability == ActivityNotAvailable)
            {
                // This can happen if System.Diagnostics.DiagnosticSource cannot be loaded,
                // i.e. in some PowerShell scenarios where only Microsoft.ApplicationInsights.dll was loaded.
                return false;
            }

            // Must be activityTypeAvailability == ActivityAvailabilityUnknown

            if (CanLoadActivityType())
            {
                Interlocked.Exchange(ref activityTypeAvailability, ActivityAvailable);
                return true;
            }
            else
            {
                Interlocked.Exchange(ref activityTypeAvailability, ActivityNotAvailable);
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CanLoadActivityType()
        {
            // This is a workaround that allows ApplicationInsights Core SDK run without DiagnosticSource.dll
            // so the ApplicationInsights.dll could be used alone to track telemetry, and will fall back to CallContext/AsyncLocal instead of Activity
            try
            {
                return LoadActivityType();
            }
            catch (System.IO.IOException)
            {
                // Cannot open DLL file
                return false;
            }
            catch (System.TypeLoadException)
            {
                // Cannot resolve type
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool LoadActivityType()
        {
            // At this point the Activity type has already been loaded, or - if not - the runtime will attempt to load the assembly from which it is referenced.
            // While doing so, it will respect type redirects potentially set up by the execution environment. This is preferable to doing Assembly-Load directly.
            return typeof(System.Diagnostics.Activity) != null;
        }
    }
}
