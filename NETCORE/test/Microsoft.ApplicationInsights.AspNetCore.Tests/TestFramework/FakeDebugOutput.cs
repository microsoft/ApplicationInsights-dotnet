using Microsoft.ApplicationInsights.Extensibility;

namespace Microsoft.ApplicationInsights.AspNetCore.Tests.TestFramework
{
    internal class FakeDebugOutput : IDebugOutput
    {
        public void WriteLine(string message)
        {
        }

        public bool IsLogging() => false;

        public bool IsAttached() => false;
    }
}
