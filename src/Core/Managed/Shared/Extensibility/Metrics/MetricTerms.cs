using System;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    internal static class MetricTerms
    {
        public static class Extraction
        {
            public static class PipelineInfo
            {
                public const string PropertyKey = "MetricsExtraction.PipelineInfo";
            }
        }

        public static class Autocollection
        {
            public static class Moniker
            {
                public const string Key = "MetricIsAutocollected";
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
                    public const string Duration = "Dependency Calls Duration";
                    public const string Failures = "Dependency Calls Failures";
                }
            }
        }


    }
}
