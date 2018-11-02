namespace Microsoft.ApplicationInsights.Extensibility.Implementation.External
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Partial class to implement ISerializableWithWriter
    /// </summary>
    internal partial class RequestData
    {
        private IDictionary<string, double> measurementsInternal;
        private IDictionary<string, string> propertiesInternal;

#pragma warning disable SA1300 // Element must begin with upper-case letter
        public IDictionary<string, double> measurements
#pragma warning restore SA1300 // Element must begin with upper-case letter
        {
            get { return LazyInitializer.EnsureInitialized(ref this.measurementsInternal, () => new ConcurrentDictionary<string, double>()); }
            set { this.measurementsInternal = value; }
        }

        public IDictionary<string, string> properties
        {
            get { return LazyInitializer.EnsureInitialized(ref this.propertiesInternal, () => new ConcurrentDictionary<string, string>()); }
            set { this.propertiesInternal = value; }
        }
    }
}