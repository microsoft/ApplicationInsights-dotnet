//-----------------------------------------------------------------------
// <copyright file="TraceEventSessionMock.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EtwTelemetryCollector.Tests
{
    using System;
    using System.Collections.Generic;
    using Diagnostics.Tracing;
    using Diagnostics.Tracing.Session;
    using Microsoft.ApplicationInsights.EtwCollector;

    internal class TraceEventSessionMock : ITraceEventSession
    {
        private bool? isElevated;

        public List<string> EnabledProviderNames { get; private set; }
        public List<Guid> EnabledProviderGuids { get; private set; }


        public TraceEventSessionMock()
            : this(true)
        {
        }

        public TraceEventSessionMock(bool? fakeElevatedStatus)
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
