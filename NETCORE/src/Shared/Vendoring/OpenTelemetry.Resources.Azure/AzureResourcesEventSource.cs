// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0
#nullable enable

namespace Microsoft.ApplicationInsights.Shared.Vendoring.OpenTelemetry.Resources.Azure
{
    using System;
    using System.Diagnostics.Tracing;

    // Deviation from upstream: renamed from "OpenTelemetry-Resources-Azure", with a distinct name per package, to avoid
    // an EventSource name collision between the Application Insights packages, other vendored copies, or the
    // OpenTelemetry.Resources.Azure package loaded in the same process.
    [EventSource(Name = EventSourceName)]
    internal sealed class AzureResourcesEventSource : EventSource
    {
        public static AzureResourcesEventSource Log = new();

#if AI_ASPNETCORE_WEB
        internal const string EventSourceName = "Microsoft-ApplicationInsights-AspNetCore-Resources-Azure";
#elif AI_ASPNETCORE_WORKER
        internal const string EventSourceName = "Microsoft-ApplicationInsights-WorkerService-Resources-Azure";
#elif AI_CLASSIC_WEB
        internal const string EventSourceName = "Microsoft-ApplicationInsights-Web-Resources-Azure";
#else
        internal const string EventSourceName = "Microsoft-ApplicationInsights-Resources-Azure";
#endif

        private const int EventIdFailedToDetectAppServiceResources = 1;
        private const int EventIdFailedToDetectAzureContainerAppResources = 2;
        private const int EventIdFailedToDetectAzureVMResources = 3;
        private const int EventIdResourceDetectorSkipped = 4;

        [NonEvent]
        public void FailedToDetectAppServiceResources(Exception ex)
        {
            if (this.IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                this.FailedToDetectAppServiceResources(ex.ToInvariantString());
            }
        }

        [NonEvent]
        public void FailedToDetectAzureContainerAppResources(Exception ex)
        {
            if (this.IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                this.FailedToDetectAzureContainerAppResources(ex.ToInvariantString());
            }
        }

        [NonEvent]
        public void FailedToDetectAzureVMResources(Exception ex)
        {
            if (this.IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                this.FailedToDetectAzureVMResources(ex.ToInvariantString());
            }
        }

        [Event(EventIdFailedToDetectAppServiceResources, Message = "Failed to detect Azure App Service resources. Exception: {0}", Level = EventLevel.Warning)]
        public void FailedToDetectAppServiceResources(string exception)
            => this.WriteEvent(EventIdFailedToDetectAppServiceResources, exception);

        [Event(EventIdFailedToDetectAzureContainerAppResources, Message = "Failed to detect Azure Container App resources. Exception: {0}", Level = EventLevel.Warning)]
        public void FailedToDetectAzureContainerAppResources(string exception)
            => this.WriteEvent(EventIdFailedToDetectAzureContainerAppResources, exception);

        [Event(EventIdFailedToDetectAzureVMResources, Message = "Failed to detect Azure VM resources. Exception: {0}", Level = EventLevel.Warning)]
        public void FailedToDetectAzureVMResources(string exception)
            => this.WriteEvent(EventIdFailedToDetectAzureVMResources, exception);

        [Event(EventIdResourceDetectorSkipped, Message = "Resource detector '{0}' did not detect any attributes because no environment variable named {1} is set.", Level = EventLevel.Verbose)]
        public void ResourceDetectorSkipped(string detector, string environmentVariable)
            => this.WriteEvent(EventIdResourceDetectorSkipped, detector, environmentVariable);
    }
}
