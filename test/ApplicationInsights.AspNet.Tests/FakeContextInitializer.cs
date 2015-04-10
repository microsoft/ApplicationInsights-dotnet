namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    internal class FakeContextInitializer : IContextInitializer
    {
        public void Initialize(TelemetryContext context)
        {
            throw new NotImplementedException();
        }
    }
}