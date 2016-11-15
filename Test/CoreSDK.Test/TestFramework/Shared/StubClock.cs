namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class StubClock : IClock
    {
        public TimeSpan Time { get; set; }

        TimeSpan IClock.Time
        {
            get 
            { 
                if (this.Time == default(TimeSpan))
                {
                    return TimeSpan.Zero;
                }

                return this.Time; 
            }
        }
    }
}
