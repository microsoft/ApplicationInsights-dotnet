namespace Microsoft.ApplicationInsights.AspNet.Tests
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    // TODO: Remove FakeContextInitializer when we can use a dynamic test isolation framework, like NSubstitute or Moq
    internal class FakeContextInitializer : IContextInitializer
    {
        public void Initialize(TelemetryContext context)
        {
            throw new NotImplementedException();
        }
    }
}