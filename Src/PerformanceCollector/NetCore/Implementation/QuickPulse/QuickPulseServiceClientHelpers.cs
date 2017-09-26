namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System.Collections.Generic;
    using System.Net.Http.Headers;

    internal static class QuickPulseServiceClientHelpers
    {
        private static readonly string[] emptyResult = new string[0];

        public static IEnumerable<string> GetValuesSafe(this HttpResponseHeaders headers, string name)
        {
            IEnumerable<string> result = (headers?.Contains(name) ?? false) ? headers.GetValues(name) : emptyResult;

            return result;
        }
    }
}
