namespace WebApi20.FuncTests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;

    using FunctionalTestUtils;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.DependencyCollector;
    using Xunit;
    using Xunit.Abstractions;
    using AI;
    using Microsoft.Extensions.Logging;
    using System.Reflection;

    /// <summary>
    /// These are Functional Tests validating E2E ILogger integration. Though filtering mechanism is done by the ILogger framework itself, we 
    /// are here testing that the integration is done in correct ways.
    /// Specifically,
    /// 1. By Default, Warning and above from All categories is expected to captured.
    /// 2. Any overriding done by user is respected and will override default behavior
    /// </summary>
    public class LoggerTests : TelemetryTestsBase, IDisposable
    {
        private readonly string assemblyName;
        public LoggerTests(ITestOutputHelper output) : base (output)
        {
            this.assemblyName = this.GetType().GetTypeInfo().Assembly.GetName().Name;
        }

        [Fact]
        public void TestIloggerWarningOrAboveIsCapturedByDefault()
        {

#if NETCOREAPP2_0
            using (var server = new InProcessServer(assemblyName, this.output))
            {                
                // Make request to this path, which sents one log of each severity  Error, Warning, Information, Trace
                this.ExecuteRequest(server.BaseHost + "/api/values/5");

                // By default, AI is expected to capture Warning, Error
                var actual = server.Listener.ReceiveItemsOfType<TelemetryItem<MessageData>>(2, TestListenerTimeoutInMs);
                this.DebugTelemetryItems(actual);

                // Expect 2 items.
                Assert.Equal(2, actual.Count());

                ValidateMessage(actual[0], new string[] {"error", "warning" });
                ValidateMessage(actual[1], new string[] { "error", "warning" });
            }
#endif
        }

        [Fact]
        public void TestIloggerDefaultsCanBeOverridenByUserForAllCategories()
        {

#if NETCOREAPP2_0
            void ConfigureServices(IServiceCollection services)
            {
                // ARRANGE
                // AddFilter to capture only Error. This is expected to override default behavior.
                services.AddLogging(builder => builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Error));
            }

            using (var server = new InProcessServer(assemblyName, this.output,null, ConfigureServices))
            {                
                // Make request to this path, which sents one log of each severity  Error, Warning, Information, Trace
                this.ExecuteRequest(server.BaseHost + "/api/values/5");

                // AI is now expected to capture only Error
                var actual = server.Listener.ReceiveItemsOfType<TelemetryItem<MessageData>>(1, TestListenerTimeoutInMs);
                this.DebugTelemetryItems(actual);

                // Expect 1 item1.
                Assert.Single(actual);

                ValidateMessage(actual[0], new string[] { "error"});                
            }
#endif
        }

        [Fact]
        public void TestIloggerDefaultsCanBeOverridenByUserForSpecificCategory()
        {

#if NETCOREAPP2_0
            void ConfigureServices(IServiceCollection services)
            {
                // ARRANGE
                // AddFilter to capture Trace or above for user category. This is expected to override default behavior.                
                services.AddLogging(builder => builder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("WebApi20.FunctionalTests.Controllers", LogLevel.Trace));
            }

            using (var server = new InProcessServer(assemblyName, this.output, null, ConfigureServices))
            {
                // Make request to this path, which sents one log of each severity  Error, Warning, Information, Trace
                this.ExecuteRequest(server.BaseHost + "/api/values/5");

                // AI is now expected to capture Warning or above for all categories (default behavior), except 'WebApi20.FunctionalTests.Controllers' category where Trace of above
                // is captured. (overridden behavior)
                var actual = server.Listener.ReceiveItemsOfType<TelemetryItem<MessageData>>(4, TestListenerTimeoutInMs);
                this.DebugTelemetryItems(actual);

                // Expect 4 items.
                Assert.Equal(4, actual.Count());

                ValidateMessage(actual[0], new string[] { "error" });
                ValidateMessage(actual[1], new string[] { "warning" });
                ValidateMessage(actual[2], new string[] { "information" });
                ValidateMessage(actual[3], new string[] { "trace" });
            }
#endif
        }


        private bool ValidateMessage(Envelope item, string[] expectedMessages)
        {
            if (!(item is TelemetryItem<MessageData> trace))
            {
                return false;
            }

            var actualMessage = trace.data.baseData.message;

            bool foundMessage = false;
            foreach (var msg in expectedMessages)
            {
                if(actualMessage.Contains(msg))
                {
                    foundMessage = true;
                    break;
                }
            }

            return foundMessage;
        }

        public void Dispose()
        {
            while (Activity.Current != null)
            {
                Activity.Current.Stop();
            }
        }
    }
}

