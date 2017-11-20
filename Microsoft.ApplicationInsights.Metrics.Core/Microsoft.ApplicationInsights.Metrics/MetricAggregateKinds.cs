using System;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    public static class MetricAggregateKinds
    {
        /// <summary />
        public static class SimpleStatistics
        {
            /// <summary />
            public const string Moniker = "Microsoft.Azure.SimpleStatistics";

            /// <summary />
            public static class DataKeys
            {
                /// <summary />
                public const string Count = "Count";

                /// <summary />
                public const string Sum = "Sum";

                /// <summary />
                public const string Min = "Min";

                /// <summary />
                public const string Max = "Max";

                /// <summary />
                public const string StdDev = "StdDev";
            }
        }

        /// <summary />
        public static class Gauge
        {
            /// <summary />
            public const string Moniker = "Microsoft.Azure.Gauge";

            /// <summary />
            public static class DataKeys
            {
                /// <summary />
                public const string Last = "Last";

                /// <summary />
                public const string Min = "Min";

                /// <summary />
                public const string Max = "Max";
            }
        }

        /// <summary />
        public static class Accumulator
        {
            /// <summary />
            public const string Moniker = "Microsoft.Azure.Accumulator";

            /// <summary />
            public static class DataKeys
            {
                /// <summary />
                public const string Sum = "Sum";

                /// <summary />
                public const string Min = "Min";

                /// <summary />
                public const string Max = "Max";
            }
        }

        /// <summary />
        public static class NaiveDistinctCount
        {
            /// <summary />
            public const string Moniker = "Microsoft.Azure.NaiveDistinctCount";

            /// <summary />
            public static class DataKeys
            {
                /// <summary />
                public const string TotalCount = "TotalCount";

                /// <summary />
                public const string DistinctCount = "DistinctCount";
            }
        }

    }
}
