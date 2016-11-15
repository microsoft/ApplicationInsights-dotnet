namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    internal interface IClock
    {
        TimeSpan Time { get; }
    }
}
