namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing
{
    using System;

    /// <summary>
    /// EventSource class attribute for Silverlight.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class EventSourceAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the ETW name of the event source.
        /// </summary>
        public string Name { get; set; }
    }
}
