namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;

    internal interface IClock
    {
        DateTimeOffset Time { get; }
    }
}
