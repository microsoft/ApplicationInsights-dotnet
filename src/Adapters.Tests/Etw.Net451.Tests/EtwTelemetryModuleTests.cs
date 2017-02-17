//-----------------------------------------------------------------------
// <copyright file="EtwTelemetryModuleTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EtwTelemetryCollector.Tests
{
    using System;
    using Microsoft.ApplicationInsights.EtwCollector;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Tests;
    using Microsoft.ApplicationInsights.Tracing.Tests;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EtwTelemetryModuleTests : IDisposable
    {
        private const int NoEventSourcesConfiguredEventId = 1;
        private const int FailedToEnableProvidersEventId = 2;
        private const int ModuleInitializationFailedEventId = 3;
        private const int RequiresToRunUnderPriviledgedAccountEventId = 4;

        private readonly AdapterHelper adapterHelper = new AdapterHelper();

        public void Dispose()
        {
            this.adapterHelper.Dispose();
        }

        private TelemetryConfiguration GetTestTelemetryConfiguration(bool resetChannel = true)
        {
            var configuration = new TelemetryConfiguration();
            configuration.InstrumentationKey = this.adapterHelper.InstrumentationKey;
            if (resetChannel)
            {
                configuration.TelemetryChannel = this.adapterHelper.Channel.Reset();
            }
            else
            {
                configuration.TelemetryChannel = this.adapterHelper.Channel;
            }
            return configuration;
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleDefaultConstructorExists()
        {
            using (EtwTelemetryModule module = new EtwTelemetryModule())
            {
                Assert.IsNotNull(module, "There has to be a default constructor, which has no parameter.");
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleInitializeFailedWhenConfigurationIsNull()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (EtwTelemetryModule module = new EtwTelemetryModule())
            {
                module.Initialize(null);
                Assert.AreEqual(1, listener.EventsReceived.Count);
                Assert.AreEqual(ModuleInitializationFailedEventId, listener.EventsReceived[0].EventId);
                Assert.AreEqual("Argument configuration is required. The initialization is terminated.", listener.EventsReceived[0].Payload[1].ToString());
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleInitializeFailedWhenDisposed()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            {
                EtwTelemetryModule module = new EtwTelemetryModule();
                module.Dispose();
                module.Initialize(GetTestTelemetryConfiguration());

                Assert.AreEqual(1, listener.EventsReceived.Count);
                Assert.AreEqual(ModuleInitializationFailedEventId, listener.EventsReceived[0].EventId);
                Assert.AreEqual("Can't initialize a module that is disposed. The initialization is terminated.", listener.EventsReceived[0].Payload[1].ToString());
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleInitializeFailedWhenProcessNotElevated()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(false))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                ExceptionAssert.Throws<UnauthorizedAccessException>(() =>
                {
                    module.Initialize(GetTestTelemetryConfiguration());
                    Assert.AreEqual(1, listener.EventsReceived.Count);
                    Assert.AreEqual(ModuleInitializationFailedEventId, listener.EventsReceived[0].EventId);
                    Assert.AreEqual("The process is required to be elevated to enable ETW providers. The initialization is terminated.", listener.EventsReceived[0].Payload[1].ToString());
                });
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleInitializeFailedWithEventWhenProcessNotElevated()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(false))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                try
                {
                    module.Initialize(GetTestTelemetryConfiguration());
                }
                catch (UnauthorizedAccessException)
                {
                    // Slient the expected exception to keep test running.
                }

                Assert.AreEqual(1, listener.EventsReceived.Count);
                Assert.AreEqual(RequiresToRunUnderPriviledgedAccountEventId, listener.EventsReceived[0].EventId);
                Assert.AreEqual(@"Failed to enable provider for the {0}. Run under priviledged account is required.", listener.EventsReceived[0].Message);
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleInitializeFailedWhenSourceIsNotSpecified()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                module.Initialize(GetTestTelemetryConfiguration());
                Assert.AreEqual(1, listener.EventsReceived.Count);
                Assert.AreEqual(NoEventSourcesConfiguredEventId, listener.EventsReceived[0].EventId);
                Assert.AreEqual("EtwTelemetryModule", listener.EventsReceived[0].Payload[1].ToString());
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleInitializeSucceed()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = "Test Provider",
                    Level = Diagnostics.Tracing.TraceEventLevel.Always
                });
                module.Initialize(GetTestTelemetryConfiguration());
                Assert.AreEqual(0, listener.EventsReceived.Count);
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleProviderEnabledByName()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = "Test Provider",
                    Level = Diagnostics.Tracing.TraceEventLevel.Always
                });
                module.Initialize(GetTestTelemetryConfiguration());
                Assert.AreEqual(1, traceEventSession.EnabledProviderNames.Count);
                Assert.AreEqual("Test Provider", traceEventSession.EnabledProviderNames[0]);
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleProviderEnabledByGuid()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                Guid guid = Guid.NewGuid();
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderGuid = guid,
                    Level = Diagnostics.Tracing.TraceEventLevel.Always
                });
                module.Initialize(GetTestTelemetryConfiguration());
                Assert.AreEqual(1, traceEventSession.EnabledProviderGuids.Count);
                Assert.AreEqual(guid.ToString(), traceEventSession.EnabledProviderGuids[0].ToString());
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void EtwTelemetryModuleProviderNotEnabledByEmptyGuid()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                Guid guid = Guid.Empty;
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderGuid = guid,
                    Level = Diagnostics.Tracing.TraceEventLevel.Always
                });
                module.Initialize(GetTestTelemetryConfiguration());
                Assert.AreEqual(0, traceEventSession.EnabledProviderGuids.Count);
            }
        }
    }
}
