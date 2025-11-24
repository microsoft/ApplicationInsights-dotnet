using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntegrationTests.WorkerApp
{
    public sealed class QueuedHostedService : BackgroundService
    {
        private readonly IBackgroundTaskQueue taskQueue;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ILogger<QueuedHostedService> logger;

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            IServiceScopeFactory scopeFactory,
            ILogger<QueuedHostedService> logger)
        {
            this.taskQueue = taskQueue;
            this.scopeFactory = scopeFactory;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var workItem = await this.taskQueue.DequeueAsync(stoppingToken).ConfigureAwait(false);
                    using var scope = this.scopeFactory.CreateScope();
                    using var activity = WorkerDiagnostics.BackgroundWork.StartActivity(
                        "integrationtests.workitem",
                        ActivityKind.Server);

                    await workItem(scope.ServiceProvider, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error executing queued background work item.");
                }
            }
        }
    }
}
