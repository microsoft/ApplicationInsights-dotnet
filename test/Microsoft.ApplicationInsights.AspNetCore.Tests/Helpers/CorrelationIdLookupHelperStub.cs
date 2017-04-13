namespace Microsoft.ApplicationInsights.AspNetCore.Tests.Helpers
{
    using DiagnosticListeners;

    public class CorrelationIdLookupHelperStub : ICorrelationIdLookupHelper
    {
        public const string AppId = "some-app-id";

        public bool TryGetXComponentCorrelationId(string instrumentationKey, out string correlationId)
        {
            correlationId = AppId;
            return true;
        }
    }
}
