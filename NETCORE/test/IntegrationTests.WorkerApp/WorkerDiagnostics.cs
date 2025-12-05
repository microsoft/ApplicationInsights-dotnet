using System.Diagnostics;

namespace IntegrationTests.WorkerApp
{
    public static class WorkerDiagnostics
    {
        public const string BackgroundWorkSourceName = "Azure.IntegrationTests.Worker";

        public static readonly ActivitySource BackgroundWork = new(BackgroundWorkSourceName);
    }
}
