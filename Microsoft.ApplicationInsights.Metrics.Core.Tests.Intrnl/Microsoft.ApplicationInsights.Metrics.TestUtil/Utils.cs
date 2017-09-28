using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Microsoft.ApplicationInsights.Metrics.TestUtil
{
    internal static class Utils
    {
        public const string AggregationIntervalMonikerPropertyKey = "_MS.AggregationIntervalMs";
        public const double MaxAllowedPrecisionError = 0.00001;
    }
}
