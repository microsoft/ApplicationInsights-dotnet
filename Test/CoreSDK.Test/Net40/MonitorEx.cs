using System.Threading;

namespace Microsoft.ApplicationInsights.TestFramework
{
#if NET40
    internal static class MonitorEx
    {
        public static bool IsEntered(object obj)
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(obj, ref lockTaken);
                return !lockTaken;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(obj);
                }
            }
        }
    }
#endif
}
