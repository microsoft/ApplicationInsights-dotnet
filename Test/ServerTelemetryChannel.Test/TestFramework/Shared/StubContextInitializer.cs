namespace Microsoft.ApplicationInsights.TestFramework
{
    using System;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    public class StubContextInitializer : IContextInitializer
    {
        public Action<TelemetryContext> OnInitialize = item => { };

        public void Initialize(TelemetryContext context)
        {
            this.OnInitialize(context);
        }
    }
}
