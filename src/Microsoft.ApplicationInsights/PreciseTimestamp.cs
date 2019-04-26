namespace Microsoft.ApplicationInsights
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
#if NET45 || NET46
    using System.Threading;
#endif

    internal class PreciseTimestamp
    {
        /// <summary>
        /// Multiplier to convert Stopwatch ticks to TimeSpan ticks.
        /// </summary>
        internal static readonly double StopwatchTicksToTimeSpanTicks = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        private static readonly object Lck = new object();
        private static PreciseTimestamp instance = null;

#if NET45 || NET46
        private static TimeSync timeSync = new TimeSync();
        private readonly Timer syncTimeUpdater;

        private PreciseTimestamp()
        {
            this.syncTimeUpdater = InitializeSyncTimer();
        }
#endif

        public static PreciseTimestamp Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (Lck)
                    {
                        if (instance == null)
                        {
                            instance = new PreciseTimestamp();
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Returns high resolution (1 DateTime tick) current UTC DateTime. 
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Enforcing instance fields initialization.")]
        public DateTimeOffset GetUtcNow()
        {
#if NET45 || NET46
            // DateTime.UtcNow accuracy on .NET Framework is ~16ms, this method 
            // uses combination of Stopwatch and DateTime to calculate accurate UtcNow.

            var tmp = timeSync;

            // Timer ticks need to be converted to DateTime ticks
            long dateTimeTicksDiff = (long)((Stopwatch.GetTimestamp() - tmp.SyncStopwatchTicks) * StopwatchTicksToTimeSpanTicks);

            // DateTime.AddSeconds (or Milliseconds) rounds value to 1 ms, use AddTicks to prevent it
            return tmp.SyncUtcNow.AddTicks(dateTimeTicksDiff);
#else
            return DateTimeOffset.UtcNow;
#endif
        }

#if NET45 || NET46
        private static void Sync()
        {
            // wait for DateTime.UtcNow update to the next granular value
            Thread.Sleep(1);
            timeSync = new TimeSync();
        }

        private static Timer InitializeSyncTimer()
        {
            Timer timer;
            // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                // fire timer every 2 hours, Stopwatch is not very precise over long periods of time, 
                // so we need to correct it from time to time
                // https://docs.microsoft.com/en-us/windows/desktop/SysInfo/acquiring-high-resolution-time-stamps
                timer = new Timer(s => { Sync(); }, null, 0, 7200000);
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }

            return timer;
        }

        private class TimeSync
        {
            public readonly DateTimeOffset SyncUtcNow = DateTimeOffset.UtcNow;
            public readonly long SyncStopwatchTicks = Stopwatch.GetTimestamp();
        }
#endif
    }
}
