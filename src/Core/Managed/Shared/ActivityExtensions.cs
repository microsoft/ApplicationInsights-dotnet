#if !NET40
namespace Microsoft.ApplicationInsights
{
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    internal static class ActivityExtensions
    {
        private const string OperationNameTag = "OperationName";
        private static bool isInitialized = false;
        private static bool isEnabled = false;

        /// <summary>
        /// Checks that Activity API could be called (DiagnosticSource DLL is loaded).
        /// </summary>
        public static bool IsActivityAvailable()
        {
            if (isInitialized)
            {
                return isEnabled;
            }

            isEnabled = Initialize();
            return isEnabled;
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool Initialize()
        {
            try
            {
                Assembly.Load(new AssemblyName("System.Diagnostics.DiagnosticSource, Version=4.0.2.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"));
                return true;
            }
            catch (System.IO.FileNotFoundException)
            {
                // This is a workaround that allows ApplicationInsights Core SDK run without DiagnosticSource.dll
                // so the ApplicationInsights.dll could be used alone to track telemetry, and will fall back to CallContext/AsyncLocal instead of Activity
                return false;
            }
            catch (System.IO.FileLoadException)
            {
                // Dll version, public token or culture is different
                return false;
            }
            finally
            {
                isInitialized = true;
            }
        }
    }
}
#endif