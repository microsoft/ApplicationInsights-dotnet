//-----------------------------------------------------------------------
// <copyright file="TelemetryLoggerTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using Xunit;
[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace Microsoft.ApplicationInsights.AspNet.Tests.Extensibility.Implementation.Tracing
{
    using System;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Tests for the TelemetryLogger class.
    /// </summary>
    public class TelemetryLoggerTests
    {
        /// <summary>Assembly reference for reuse.</summary>
        private Assembly asm;

        /// <summary>
        /// Initializes a new instance of the TelemetryLoggerTests class.
        /// </summary>
        public TelemetryLoggerTests()
        {
            this.asm = Assembly.Load(new AssemblyName("Microsoft.ApplicationInsights.AspNet"));
        }

        /// <summary>
        /// Tests that calls to each of the logging level methods make it all the way through the underlying event source to a listener.
        /// </summary>
        [Fact]
        public void TestThatAllTelemetryLogCallsMakeItThroughTheEventSourceToAListener()
        {
            using (var listener = new TestEventListener())
            {
                EventSource eventSource = this.GetInternalEventSource("Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing.AspNetEventSource");
                listener.EnableEvents((EventSource)eventSource, EventLevel.Verbose);
                ILogger logger = this.ConfigureTelemetryLogger(LogLevel.Debug);

                listener.ClearMessages();
                LoggerExtensions.LogDebug(logger, $"Debug message test.");
                LoggerExtensions.LogVerbose(logger, $"Verbose message test.");
                LoggerExtensions.LogInformation(logger, $"Information message test.");
                LoggerExtensions.LogWarning(logger, $"Warning message test.");
                LoggerExtensions.LogError(logger, $"Error message test.");
                LoggerExtensions.LogCritical(logger, $"Critical message test.");

                Assert.NotNull(listener.Messages);
                Assert.Equal<int>(6, listener.Messages.Count());
            }
        }

        /// <summary>
        /// Tests that setting the logging level causes log calls for a lower level to be ignored.
        /// </summary>
        [Fact]
        public void TestThatTelemetryLogCallsHonorLevel()
        {
            using (var listener = new TestEventListener())
            {
                EventSource eventSource = this.GetInternalEventSource("Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing.AspNetEventSource");
                listener.EnableEvents((EventSource)eventSource, EventLevel.Verbose);
                ILogger logger = this.ConfigureTelemetryLogger(LogLevel.Error);

                // Warning should be ignored but Error should be logged.
                listener.ClearMessages();
                LoggerExtensions.LogWarning(logger, $"Warning message test.");
                LoggerExtensions.LogError(logger, $"Error message test.");

                Assert.Equal<int>(1, listener.Messages.Count());
            }
        }

        /// <summary>
        /// Gets an event source declared as internal by its type name.
        /// </summary>
        /// <param name="eventSourceTypeName">The name of the event source type.</param>
        /// <returns>The event source if it exists.</returns>
        private EventSource GetInternalEventSource(string eventSourceTypeName)
        {
            Type eventSourceType = this.asm.GetType(eventSourceTypeName);
            return (EventSource)eventSourceType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        /// <summary>
        /// Gets the logger and sets the logging level.
        /// </summary>
        /// <param name="level">The level to set as active on the logger.</param>
        /// <returns>The logger instance.</returns>
        private ILogger ConfigureTelemetryLogger(LogLevel level)
        {
            Type loggerType = this.asm.GetType("Microsoft.ApplicationInsights.AspNet.Extensibility.Implementation.Tracing.TelemetryLogger");
            object logger = loggerType.GetField("instance", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            PropertyInfo levelProperty = loggerType.GetProperty("Level", BindingFlags.Instance | BindingFlags.Public);
            levelProperty.SetValue(logger, level);
            return (ILogger)logger;
        }
    }
}
