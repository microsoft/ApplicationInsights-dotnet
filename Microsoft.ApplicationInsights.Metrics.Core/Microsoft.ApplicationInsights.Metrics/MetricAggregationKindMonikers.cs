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
    }
}
