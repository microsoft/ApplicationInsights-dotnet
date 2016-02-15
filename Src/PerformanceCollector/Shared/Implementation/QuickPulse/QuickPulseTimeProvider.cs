namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;

    internal class QuickPulseTimeProvider
    {
        public virtual DateTime UtcNow
        {
            get
            {
                return DateTime.UtcNow;
            }
        }
    }
}