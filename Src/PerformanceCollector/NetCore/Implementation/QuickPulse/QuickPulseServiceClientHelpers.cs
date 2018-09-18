namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System.Collections.Generic;
    using System.Net.Http.Headers;
    using Microsoft.ApplicationInsights.Common;

    internal static class QuickPulseServiceClientHelpers
    {
        private static readonly string[] emptyResult = ArrayExtensions.Empty<string>();

        public static IEnumerable<string> GetValuesSafe(this HttpResponseHeaders headers, string name)
        {
            IEnumerable<string> result = (headers?.Contains(name) ?? false) ? headers.GetValues(name) : emptyResult;

            return result;
        }
    }
}
