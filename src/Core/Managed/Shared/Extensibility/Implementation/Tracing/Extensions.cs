namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    /// <summary>
    /// Provides a set of extension methods for tracing.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns a culture-independent string representation of the given <paramref name="exception"/> object, 
        /// appropriate for diagnostics tracing.
        /// </summary>
        public static string ToInvariantString(this Exception exception)
        {
#if !WINRT && !CORE_PCL
            CultureInfo originalUICulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
#endif
                return exception.ToString();
#if !WINRT && !CORE_PCL
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
#endif
        }
    }
}
