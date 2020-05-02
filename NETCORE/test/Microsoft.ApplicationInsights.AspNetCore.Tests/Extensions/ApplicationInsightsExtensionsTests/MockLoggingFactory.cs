namespace Microsoft.Extensions.DependencyInjection.Test
{
    using Logging;

#pragma warning disable CS0618 // TelemetryConfiguration.Active is obsolete. We still test with this for backwards compatibility.
    internal class MockLoggingFactory : ILoggerFactory
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return null;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
#pragma warning restore CS0618 // TelemetryConfiguration.Active is obsolete. We still test with this for backwards compatibility.
}