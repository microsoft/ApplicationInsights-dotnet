using System;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    internal static class MetricTerms
    {
        private const string MetricPropertiesNamePrefix = "Microsoft.ApplicationInsights.Metrics";
        public static class Extraction
        {
            //Microsoft.ApplicationInsights.Metrics.MetricIsAutocollected
            public static class ConsideredByProcessors
            {
                public static class Moniker
                {
                    public const string Key = MetricPropertiesNamePrefix + ".Extraction.ConsideredByProcessors";
                }
            }
        }

        public static class Autocollection
        {
            public static class Moniker
            {
                public const string Key = MetricPropertiesNamePrefix + ".MetricIsAutocollected";
                public const string Value = "True";

            }

            public static class MetricNames
            {
                private const string PreviewLabel = " (Preview)";

                public static class Requests
                {
                    public const string Count = "Requests" + PreviewLabel;
                    public const string Failures = "Request Failures" + PreviewLabel;
                    public const string ResponseTime = "Response Time" + PreviewLabel;
                }

                public static class DependencyCalls
                {
                    public const string Duration = "Dependency Calls Duration" + PreviewLabel;
                    public const string Failures = "Dependency Calls Failures" + PreviewLabel;
                }
            }
        }
    }
}
