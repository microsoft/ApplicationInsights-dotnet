//-----------------------------------------------------------------------
// <copyright file="EtwTelemetryModuleTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        private const int AccessDeniedEventId = 4;

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
        public void DefaultConstructorExists()
        {
            using (EtwTelemetryModule module = new EtwTelemetryModule())
            {
                Assert.IsNotNull(module, "There has to be a default constructor, which has no parameter.");
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void InitializeFailedWhenConfigurationIsNull()
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
        public void InitializeFailedWhenDisposed()
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
        public void InitializeFailedWhenSourceIsNotSpecified()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true, false))
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
        public void InitializeFailedWhenAccessDenied()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true, true))
            using (EtwTelemetryModule module = new EtwTelemetryModule(traceEventSession, (t, c) => { }))
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = "Test Provider",
                    Level = Diagnostics.Tracing.TraceEventLevel.Always
                });
                module.Initialize(GetTestTelemetryConfiguration());
                Assert.AreEqual(1, listener.EventsReceived.Count);
                Assert.AreEqual(AccessDeniedEventId, listener.EventsReceived[0].EventId);
                Assert.AreEqual("Access Denied.", listener.EventsReceived[0].Payload[1].ToString());
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public void InitializeSucceed()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true, false))
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
        public void ProviderEnabledByName()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true, false))
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
        public void ProviderEnabledByGuid()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true, false))
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
        public void ProviderNotEnabledByEmptyGuid()
        {
            using (EventSourceModuleDiagnosticListener listener = new EventSourceModuleDiagnosticListener())
            using (TraceEventSessionMock traceEventSession = new TraceEventSessionMock(true, false))
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
