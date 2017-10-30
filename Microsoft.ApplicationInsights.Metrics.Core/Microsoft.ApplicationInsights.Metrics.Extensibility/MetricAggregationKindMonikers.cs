using System;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    public static class MetricAggregationKinds
    {
        /// <summary />
        public static class SimpleMeasurement
        {
            /// <summary />
            public const string Moniker = "Microsoft.ApplicationInsights.SimpleMeasurement";

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
