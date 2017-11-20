using System;
using System.ComponentModel;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class MetricAggregateKinds
    {
        internal static readonly MetricAggregateKinds Instance = new MetricAggregateKinds();
        private MetricAggregateKinds()
        {
        }

        /// <summary>
        /// Groups constants used my metric aggregates produced by aggregators that are configured by metric configurations represented
        /// through instances of <see cref="MetricSeriesConfigurationForMeasurement"/>. See also <c>MetricConfigurations.Common.Measurement()</c>./>
        /// </summary>
        public sealed class Measurement
        {
            internal static readonly Measurement Instance = new Measurement();
            private Measurement()
            {
            }

            /// <summary />
            public string Moniker { get { return Constants.Measurement.Moniker; } }

            /// <summary />
            public DataKeysConstants DataKeys { get { return DataKeysConstants.Instance; } }

            /// <summary />
            public sealed class DataKeysConstants
            {
                internal static readonly DataKeysConstants Instance = new DataKeysConstants();
                private DataKeysConstants()
                {
                }

                /// <summary />
                public string Count { get { return Constants.Measurement.DataKeys.Count; } }

                /// <summary />
                public string Sum { get { return Constants.Measurement.DataKeys.Sum; } }

                /// <summary />
                public string Min { get { return Constants.Measurement.DataKeys.Min; } }

                /// <summary />
                public string Max { get { return Constants.Measurement.DataKeys.Max; } }

                /// <summary />
                public string StdDev { get { return Constants.Measurement.DataKeys.StdDev; } }
            }
        }

        private static class Constants
        {
            /// <summary />
            public static class Measurement
            {
                /// <summary />
                public const string Moniker = "Microsoft.Azure.Measurement";

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
}
