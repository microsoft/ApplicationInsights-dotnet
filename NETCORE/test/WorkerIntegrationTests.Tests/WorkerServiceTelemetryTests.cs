using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTests.WorkerApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Tests
{
    public static class WorkerServiceTestCollection
    {
        public const string Name = "WorkerServiceIntegrationTests";
    }

    [CollectionDefinition(WorkerServiceTestCollection.Name)]
    public sealed class WorkerServiceTestCollectionDefinition : ICollectionFixture<WorkerHostFixture>
    {
    }

    [Collection(WorkerServiceTestCollection.Name)]
    public class WorkerServiceTelemetryTests
    {
        private readonly WorkerHostFixture fixture;
        private readonly ITestOutputHelper output;

        public WorkerServiceTelemetryTests(WorkerHostFixture fixture, ITestOutputHelper output)
        {
            this.fixture = fixture;
            this.output = output;
            this.fixture.Telemetry.Clear();
        }

        [Fact]
        public async Task BackgroundWorkOperationsAreExported()
        {
            await EnqueueAndWaitAsync(this.fixture.TaskQueue, async (provider, _) =>
            {
                await Task.CompletedTask;
            });

            await this.fixture.WaitForTelemetryAsync(expectedItemCount: 1);
            this.LogTelemetrySnapshot();

            var operations = this.fixture.Telemetry.GetAllTelemetryOfType<RequestTelemetryEnvelope>();
            Assert.NotEmpty(operations);

            var backgroundOperation = Assert.Single(operations.Where(operation =>
                operation.Name.Contains("integrationtests.workitem", StringComparison.OrdinalIgnoreCase)));

            Assert.False(string.IsNullOrWhiteSpace(backgroundOperation.OperationId));
            Assert.True(backgroundOperation.Duration > TimeSpan.Zero, "Request duration should be greater than zero.");
            Assert.False(string.IsNullOrWhiteSpace(backgroundOperation.ResponseCode));
        }

        [Fact]
        public async Task HttpClientDependenciesAreTracked()
        {
            await EnqueueAndWaitAsync(this.fixture.TaskQueue, async (provider, token) =>
            {
                var factory = provider.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient();
                using var response = await client.GetAsync("https://www.bing.com/favicon.ico", token).ConfigureAwait(false);
            });

            await this.fixture.WaitForTelemetryAsync(expectedItemCount: 2);
            this.LogTelemetrySnapshot();

            var dependencies = this.fixture.Telemetry.GetAllTelemetryOfType<DependencyTelemetryEnvelope>();
            Assert.NotEmpty(dependencies);

            var httpDependency = Assert.Single(dependencies.Where(dependency =>
                dependency.Target.Contains("bing.com", StringComparison.OrdinalIgnoreCase)));

            Assert.True(string.Equals("Http", httpDependency.Type, StringComparison.OrdinalIgnoreCase));
            Assert.True(httpDependency.Success, "Dependency should be marked successful.");
            Assert.False(string.IsNullOrWhiteSpace(httpDependency.ResultCode));
            Assert.Contains("bing.com", httpDependency.Data, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TraceTelemetryIsExportedFromBackgroundWork()
        {
            var traceIdentifier = Guid.NewGuid().ToString("N");
            var expectedMessage = $"worker-trace-{traceIdentifier}";

            await EnqueueAndWaitAsync(this.fixture.TaskQueue, (provider, _) =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("IntegrationTests.Worker.Trace");

                using (logger.BeginScope(new Dictionary<string, object> { ["CategoryName"] = "IntegrationTests.Worker.Trace" }))
                {
                    logger.LogWarning(expectedMessage);
                }

                return Task.CompletedTask;
            });

            await this.fixture.WaitForTelemetryAsync(expectedItemCount: 2);
            this.LogTelemetrySnapshot();

            var traces = this.fixture.Telemetry.GetAllTelemetryOfType<TraceTelemetryEnvelope>();
            Assert.NotEmpty(traces);

            var traceTelemetry = Assert.Single(traces.Where(trace =>
                trace.Message.Contains(traceIdentifier, StringComparison.Ordinal)));

            Assert.Equal(expectedMessage, traceTelemetry.Message);
            Assert.True(traceTelemetry.SeverityLevel.HasValue, "Trace severity should be populated.");
            Assert.Equal(2, traceTelemetry.SeverityLevel.Value); // Warning

            var operations = this.fixture.Telemetry.GetAllTelemetryOfType<RequestTelemetryEnvelope>();
            var backgroundOperation = Assert.Single(operations.Where(operation =>
                operation.Name.Contains("integrationtests.workitem", StringComparison.OrdinalIgnoreCase)));

            Assert.Equal(backgroundOperation.OperationId, traceTelemetry.OperationId);
            Assert.Equal(backgroundOperation.Id, traceTelemetry.OperationParentId);
        }

        private static async Task EnqueueAndWaitAsync(
            IBackgroundTaskQueue queue,
            Func<IServiceProvider, CancellationToken, Task> workItem)
        {
            var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            await queue.QueueBackgroundWorkItemAsync(async (provider, token) =>
            {
                try
                {
                    await workItem(provider, token).ConfigureAwait(false);
                    completion.SetResult();
                }
                catch (Exception ex)
                {
                    completion.SetException(ex);
                    throw;
                }
            }).ConfigureAwait(false);

            await completion.Task.ConfigureAwait(false);
        }

        private void LogTelemetrySnapshot()
        {
            var totalItems = this.fixture.Telemetry.GetTotalItemCount();
            this.output.WriteLine($"Telemetry items observed: {totalItems}");

            var requests = this.fixture.Telemetry.GetAllTelemetryOfType<RequestTelemetryEnvelope>();
            this.output.WriteLine($"Requests: {requests.Count}");

            var dependencies = this.fixture.Telemetry.GetAllTelemetryOfType<DependencyTelemetryEnvelope>();
            this.output.WriteLine($"Dependencies: {dependencies.Count}");

            var traces = this.fixture.Telemetry.GetAllTelemetryOfType<TraceTelemetryEnvelope>();
            this.output.WriteLine($"Traces: {traces.Count}");
        }
    }
}
