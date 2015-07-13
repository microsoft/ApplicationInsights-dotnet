namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.Mocks
{
    using System.Collections.Generic;
    using TestFramework;

    internal class F5DiagnosticsSenderMock : F5DiagnosticsSender
    {
        public IList<string> Messages = new List<string>();

        public F5DiagnosticsSenderMock()
        {
            this.debugOutput = new StubDebugOutput
            {
                OnWriteLine = message => this.Messages.Add(message)
            };
        }
    }
}
