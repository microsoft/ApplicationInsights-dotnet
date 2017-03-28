namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;

    internal static class ActivityProxy
    {
        public static bool TryRun(Func<bool> method)
        {
            Debug.Assert(method != null, "Method must not be null");
            try
            {
                return method.Invoke();
            }
            catch (System.IO.FileNotFoundException)
            {
                return false;
            }
        }
    }
}