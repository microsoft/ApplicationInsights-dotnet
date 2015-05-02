#if fakeportable
using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights
{
    public class TelemetryClient
    {
        public TelemetryClient()
        {
        }

        public TelemetryClient(TelemetryConfiguration telemetryConfiguration)
        {
        }

        public TelemetryContext Context { get; }
        public void TrackEvent(string eventName)
        {
        }
        public void TrackEvent(EventTelemetry telemetry)
        {
        }
        public void TrackMetric(string name, double value)
        {
        }
        public void TrackMetric(string name, double average, int sampleCount, double min, double max, IDictionary<string, string> properties)
        {
        }
        public void TrackRequest(RequestTelemetry request)
        {
        }
        public void TrackException(Exception exception)
        {
        }
        public void TrackTrace(string message)
        {
        }
        public void TrackTrace(string message, SeverityLevel? severityLevel)
        {
        }
        public void TrackTrace(string message, IDictionary<string, string> properties)
        {
        }
        public void TrackTrace(string message, SeverityLevel? severityLevel, IDictionary<string, string> properties)
        {
        }
        public void Track(ITelemetry telemetry)
        {
        }
    }
}
namespace Microsoft.ApplicationInsights.Channel
{
    public interface ITelemetry
    {
        TelemetryContext Context { get; }
    }
    public interface ITelemetryChannel : IDisposable
    {
        bool DeveloperMode { get; set; }
    }
}

namespace Microsoft.ApplicationInsights.DataContracts
{
    public class TelemetryContext
    {
        public string InstrumentationKey { get; set; }
        public OperationContext Operation { get; }
        public LocationContext Location { get; }
        public DeviceContext Device { get; }
        public SessionContext Session { get; }
        public UserContext User { get; }
    }

    public class OperationContext
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class LocationContext
    {
        public string Ip { get; set; }
    }

    public class DeviceContext
    {
        public string RoleInstance { get; set; }
    }

    public class SessionContext
    {
        public string Id { get; set; }
    }

    public class UserContext
    {
        public string Id { get; set; }
        public string UserAgent { get; set; }
        public string AccountId { get; set; }
        public DateTimeOffset? AcquisitionDate { get; set; }
    }

    public class RequestTelemetry : ITelemetry
    {
        public ITelemetryChannel TelemetryChannel { get; set; }
        public TelemetryContext Context { get; }
        public TimeSpan Duration { get; set; }
        public string HttpMethod { get; set; }
        public string Id { get; set; }
        public IDictionary<string, double> Metrics { get; }
        public string Name { get; set; }
        public IDictionary<string, string> Properties { get; }
        public string ResponseCode { get; set; }
        public string Sequence { get; set; }
        public bool Success { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Uri Url { get; set; }
    }

    public class EventTelemetry : ITelemetry
    {
        private TelemetryContext context = new TelemetryContext();
        public DateTimeOffset Timestamp { get; set; }
        public string Sequence { get; set; }

        public TelemetryContext Context
        {
            get
            {
                return this.context;
            }
        }
    }

    public enum ExceptionHandledAt
    {
        Unhandled,
        UserCode,
        Platform
    }

    public class ExceptionTelemetry : ITelemetry
    {
        public ExceptionTelemetry() {}
        public ExceptionTelemetry(Exception exception) {}
        public DateTimeOffset Timestamp { get; set; }
        public string Sequence { get; set; }
        public TelemetryContext Context { get; set; }
        public ExceptionHandledAt HandledAt { get; set; }
        public Exception Exception { get; set; }
        public IDictionary<string, double> Metrics { get; set; }
        public IDictionary<string, string> Properties { get; set; }
        public SeverityLevel? SeverityLevel { get; set; }
    }

    public class MetricTelemetry : ITelemetry
    {
        private TelemetryContext context = new TelemetryContext();
        public DateTimeOffset Timestamp { get; set; }
        public string Sequence { get; set; }
        public TelemetryContext Context
        {
            get
            {
                return this.context;
            }
        }
    }

    public class TraceTelemetry : ITelemetry
    {
        private TelemetryContext context = new TelemetryContext();
        public DateTimeOffset Timestamp { get; set; }
        public string Sequence { get; set; }
        public TelemetryContext Context
        {
            get
            {
                return this.context;
            }
        }
    }

    public enum SeverityLevel
    {
        Verbose,
        Information,
        Warning,
        Error,
        Critical
    }
}

namespace Microsoft.ApplicationInsights.Extensibility
{
    public class TelemetryConfiguration
    {
        public ITelemetryChannel TelemetryChannel { get; set; }
        public string InstrumentationKey { get; set; }
        public IList<IContextInitializer> ContextInitializers { get; }
        public IList<ITelemetryInitializer> TelemetryInitializers { get; }
        public static TelemetryConfiguration CreateDefault()
        {
            return new TelemetryConfiguration();
        }
    }

    public class TimestampPropertyInitializer
    {
    }

    public interface IContextInitializer
    {
    }

    public interface ITelemetryInitializer
    {
    }
}

namespace System.Net.NetworkInformation
{
    public class IPGlobalProperties
    {
        public string DomainName { get; }
        public static IPGlobalProperties GetIPGlobalProperties()
        {
            return new IPGlobalProperties();
        }
    }
}

namespace Microsoft.ApplicationInsights.Channel
{
    public sealed class InProcessTelemetryChannel : ITelemetryChannel
    {
        public string EndpointAddress { get; set; }
        public bool DeveloperMode { get; set; }
        public void Dispose()
        {
        }
        public void Initialize(TelemetryConfiguration configuration)
        {
        }
        public void Send(ITelemetry item)
        {
        }
    }
}

#endif

