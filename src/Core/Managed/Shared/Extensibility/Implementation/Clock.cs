namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// A highly-accurate, precise and testable clock.
    /// </summary>
    internal class Clock : IClock
    {
        // DateTimeOffset.Now is accurate to ~16ms. We want accurate and precise timestamps,
        // so use Stopwatch which is based on perf counters and is highly accurate and precise.
        // Making a static instance of Stopwatch shared by all operations.        
        private static readonly Stopwatch InitialStopWatch = Stopwatch.StartNew();
        private static IClock instance = new Clock();

        // to prevent instantiating clock in product code
        protected Clock()
        {
        }

        public static IClock Instance
        {
            get { return instance; }
            protected set { instance = value ?? new Clock(); }
        }

        public TimeSpan Time
        {
            get { return InitialStopWatch.Elapsed; }
        }
    }
}
