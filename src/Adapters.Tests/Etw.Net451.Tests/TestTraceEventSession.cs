namespace Microsoft.ApplicationInsights
{
    using System;
    using Microsoft.ApplicationInsights.EventSourceListener.EtwCollector;
    using Diagnostics.Tracing;
    using Diagnostics.Tracing.Session;
    using System.Collections.Generic;

    internal class TestTraceEventSession : ITraceEventSession
    {
        private bool? isElevated;

        public List<string> EnabledProviderNames { get; private set; }
        public List<Guid> EnabledProviderGuids { get; private set; }


        public TestTraceEventSession()
            : this(true)
        {
        }

        public TestTraceEventSession(bool? fakeElevatedStatus)
        {
            this.EnabledProviderNames = new List<string>();
            this.EnabledProviderGuids = new List<Guid>();
            this.isElevated = fakeElevatedStatus;
        }

        public ETWTraceEventSource Source { get; private set; }

        public void DisableProvider(Guid providerGuid)
        {
            EnabledProviderGuids.Remove(providerGuid);
        }

        public void DisableProvider(string providerName)
        {
            EnabledProviderNames.Remove(providerName);
        }

        public void Dispose()
        {
        }

        public bool EnableProvider(Guid providerGuid, TraceEventLevel providerLevel = TraceEventLevel.Verbose, ulong matchAnyKeywords = ulong.MaxValue, TraceEventProviderOptions options = null)
        {
            this.EnabledProviderGuids.Add(providerGuid);
            return true;
        }

        public bool EnableProvider(string providerName, TraceEventLevel providerLevel = TraceEventLevel.Verbose, ulong matchAnyKeywords = ulong.MaxValue, TraceEventProviderOptions options = null)
        {
            this.EnabledProviderNames.Add(providerName);
            return true;
        }


        public bool? IsElevated()
        {
            return this.isElevated;
        }

        public bool Stop(bool noThrow = false)
        {
            throw new NotImplementedException();
        }
    }
}
