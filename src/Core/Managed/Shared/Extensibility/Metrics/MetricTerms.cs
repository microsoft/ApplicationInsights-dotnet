using System;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics
{
    internal static class MetricTerms
    {
        private const string MetricPropertiesNamePrefix = "Microsoft.ApplicationInsights.Metrics";
        public static class Extraction
        {
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
                public static class PropertyName
                {
                    public const string Success = "Success";
                }
            }

            public static class DependencyCall
            {
                public static class PropertyName
                {
                    public const string Success = "Success";
                    public const string TypeName = "Type";
                }

                public static class TypeName
                {
                    public const string Other = "Other";
                }
            }
        }
    }
}
