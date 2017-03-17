namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    internal static class MetricTerms
    {
        private const string MetricPropertiesNamePrefix = "Microsoft.ApplicationInsights.Metrics";

        public static class Aggregation
        {
            public static class Interval
            {
                public static class Moniker
                {
                    public const string Key = MetricPropertiesNamePrefix + ".Aggregation.IntervalMs";
                }
            }
        }

        public static class Extraction
        {
            public static class ProcessedByExtractors
            {
                public static class Moniker
                {
                    public const string Key = MetricPropertiesNamePrefix + ".Extraction.ProcessedByExtractors";
                    public const string ExtractorInfoTemplate = "(Name:{0}, Ver:{1})";      // $"(Name:{ExtractorName}, Ver:{ExtractorVersion})"
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
                public static class Request
                {
                    public const string Duration = "Server response time";
                }

                public static class DependencyCall
                {
                    public const string Duration = "Dependency duration";
                }
            }

            public static class Request
            {
                public static class PropertyNames
                {
                    public const string Success = "Request.Success";
                }
            }

            public static class DependencyCall
            {
                public static class PropertyNames
                {
                    public const string Success = "Dependency.Success";
                    public const string TypeName = "Dependency.Type";
                }

                public static class TypeNames
                {
                    public const string Other = "Other";
                    public const string Unknown = "Unknown";
                }
            }
        }
    }
}
