namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;

    internal class StubClock : IClock
    {
        public DateTimeOffset Time { get; set; }

        DateTimeOffset IClock.Time
        {
            get 
            { 
                if (this.Time == default(DateTimeOffset))
                {
                    return DateTimeOffset.Now;
                }

                return this.Time; 
            }
        }
    }
}
