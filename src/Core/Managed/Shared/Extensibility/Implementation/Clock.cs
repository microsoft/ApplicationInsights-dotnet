namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    /// A highly-accurate, precise and testable clock.
    /// </summary>
    internal class Clock : IClock
    {
        // DateTimeOffset.Now is accurate to ~16ms. We want accurate and precise timestamps,
        // so use Stopwatch which is based on perf counters and is highly accurate and precise.
        private static readonly DateTimeOffset InitialTimeStamp = DateTimeOffset.Now;
        private static readonly Stopwatch OffsetStopwatch = Stopwatch.StartNew();
        private static IClock instance = new Clock();

        // Because we don't want to instantiate Clock in product code
        protected Clock()
        {
        }

        public static IClock Instance
        {
            get { return instance; }
            protected set { instance = value ?? new Clock(); }
        }

        public DateTimeOffset Time
        {
            get { return InitialTimeStamp + OffsetStopwatch.Elapsed; }
        }
    }
}
