using System;

namespace Microsoft.ApplicationInsights.Extensibility.Metrics.MetricTerms
{
    internal static class Shared
    {
        public const string MetricPropertiesNamePrefix = "Microsoft.ApplicationInsights.Metrics";
    }

    namespace Extraction.ProcessedByExtractors
    {
        internal static class Moniker
        {
            public const string Key = Shared.MetricPropertiesNamePrefix + ".Extraction.ProcessedByExtractors";
        }
    }

    namespace Autocollection
    {
        internal static class Moniker
        {
            public const string Key = Shared.MetricPropertiesNamePrefix + ".MetricIsAutocollected";
            public const string Value = "True";
        }

        namespace MetricNames
        {
            internal static class Request
            {
                public const string Duration = "Server response time";
            }

            internal static class DependencyCall
            {
                public const string Duration = "Dependency duration";
            }
        }

        namespace Request
        {
            internal static class PropertyName
            {
                public const string Success = "Success";
            }
        }

        namespace DependencyCall
        {
            internal static class PropertyName
            {
                public const string Success = "Success";
                public const string TypeName = "Type";
            }

            internal static class TypeName
            {
                public const string Other = "Other";
            }
        }
    }
}


//namespace Microsoft.ApplicationInsights.Extensibility.Metrics
//{
//    internal static class MetricTerms
//    {
//        private const string MetricPropertiesNamePrefix = "Microsoft.ApplicationInsights.Metrics";
//        public static class Extraction
//        {
//            public static class ProcessedByExtractors
//            {
//                public static class Moniker
//                {
//                    public const string Key = MetricPropertiesNamePrefix + ".Extraction.ProcessedByExtractors";
//                }
//            }
//        }

//        public static class Autocollection
//        {
//            public static class Moniker
//            {
//                public const string Key = MetricPropertiesNamePrefix + ".MetricIsAutocollected";
//                public const string Value = "True";

//            }

//            public static class MetricNames
//            {
//                public static class Request
//                {
//                    public const string Duration = "Server response time";
//                }

//                public static class DependencyCall
//                {
//                    public const string Duration = "Dependency duration";
//                }
//            }

//            public static class Request
//            {
//                public static class PropertyName
//                {
//                    public const string Success = "Success";
//                }
//            }

//            public static class DependencyCall
//            {
//                public static class PropertyName
//                {
//                    public const string Success = "Success";
//                    public const string TypeName = "Type";
//                }

//                public static class TypeName
//                {
//                    public const string Other = "Other";
//                }
//            }
//        }
//    }
//}
