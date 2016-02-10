namespace Unit.Tests
{
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// QuickPulseEventSource tests.
    /// </summary>
    [TestClass]
    public class QuickPulseEventSourceTests
    {
        [TestMethod]
        public void QuickPulseEventSourceSanityTest()
        {
            // check for FormatExceptions and ETW exceptions
            QuickPulseEventSource.Log.ModuleIsBeingInitializedEvent("Test message");
            QuickPulseEventSource.Log.CounterRegisteredEvent("counter");
            QuickPulseEventSource.Log.CounterRegistrationFailedEvent("Test exception", "counter");
            QuickPulseEventSource.Log.CounterParsingFailedEvent("Test exception", "counter");
            QuickPulseEventSource.Log.CouldNotObtainQuickPulseTelemetryProcessorEvent();
            QuickPulseEventSource.Log.CounterReadingFailedEvent("Test exception", "counter");
            QuickPulseEventSource.Log.UnknownErrorEvent("Test exception");
        }
    }
}
