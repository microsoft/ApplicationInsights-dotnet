#if !NET40
namespace Microsoft.ApplicationInsights
{
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.CompilerServices;

    internal static class ActivityExtensions
    {
        private const string OperationNameTag = "OperationName";
        private static bool isInitialized = false;
        private static bool isEnabled = false;

        /// <summary>
        /// Checks that Activity API could be called (DiagnosticSource DLL is loaded).
        /// </summary>
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)] 
        public static bool IsActivityEnabled()
        {
            if (isInitialized)
            {
                return isEnabled;
            }

            try
            {
                var activity = new Activity("IsEnabled");
                isEnabled = true;
                isInitialized = true;
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                // This is a workaround that allows ApplicationInsights Core SDK run without DiagnosticSource.dll
                // so the ApplicationInsights.dll could be used alone to track telemetry, and will fall back to CallContext/AsyncLocal instead of Activity
                isInitialized = true;
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
    }
}
#endif