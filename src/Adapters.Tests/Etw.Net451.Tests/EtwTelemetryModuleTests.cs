//-----------------------------------------------------------------------
// <copyright file="EtwTelemetryModuleTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.EtwTelemetryCollector.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.EtwCollector;
    using Microsoft.ApplicationInsights.Extensibility;
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
        private static readonly TimeSpan channelDelay = TimeSpan.FromMilliseconds(2500);

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

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public async Task ReportSingleEvent()
        {
            using (EtwTelemetryModule module = new EtwTelemetryModule())
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = TestProvider.ProviderName
                });
                module.Initialize(GetTestTelemetryConfiguration());
                TestProvider.Log.Info("Hello!");

                // There's going to be a delay around 2000ms before the events reaches the channel.
                await Task.Delay(EtwTelemetryModuleTests.channelDelay);

                // The very 1st event is for the manifest.
                Assert.AreEqual(2, this.adapterHelper.Channel.SentItems.Length);
                TraceTelemetry telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems[1];
                Assert.AreEqual("Hello!", telemetry.Message);
            }
        }


        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public async Task ReportMultipleEvents()
        {
            using (EtwTelemetryModule module = new EtwTelemetryModule())
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = TestProvider.ProviderName
                });
                module.Initialize(GetTestTelemetryConfiguration());
                TestProvider.Log.Info("Hello!");
                TestProvider.Log.Info("World!");


                // There's going to be a delay around 2000ms before the events reaches the channel.
                await Task.Delay(EtwTelemetryModuleTests.channelDelay);

                // The very 1st event is for the manifest.
                Assert.AreEqual(3, this.adapterHelper.Channel.SentItems.Length);
                TraceTelemetry hello = (TraceTelemetry)this.adapterHelper.Channel.SentItems[1];
                TraceTelemetry world = (TraceTelemetry)this.adapterHelper.Channel.SentItems[2];
                Assert.AreEqual("Hello!", hello.Message);
                Assert.AreEqual("World!", world.Message);
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public async Task ReportsAllProperties()
        {
            using (var module = new EtwTelemetryModule())
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = TestProvider.ProviderName
                });
                module.Initialize(GetTestTelemetryConfiguration());

                Guid eventId = new Guid("30ba9220-89a4-41e4-987c-9e27ade44b74");
                Guid activityId = new Guid("0724a028-27d7-40a9-a299-acf79ff0db94");
                EventSource.SetCurrentThreadActivityId(activityId);
                TestProvider.Log.Complex(eventId);

                // There's going to be a delay around 2000ms before the events reaches the channel.
                await Task.Delay(EtwTelemetryModuleTests.channelDelay);

                Assert.AreEqual(2, this.adapterHelper.Channel.SentItems.Length);
                TraceTelemetry actual = (TraceTelemetry)this.adapterHelper.Channel.SentItems[1];
                TraceTelemetry expected = new TraceTelemetry("Blah blah", SeverityLevel.Verbose);

                Assert.AreEqual("Blah blah", actual.Message);
                Assert.AreEqual(SeverityLevel.Verbose, actual.SeverityLevel);
                Assert.AreEqual(eventId.ToString(), actual.Properties["uniqueId"]);
                Assert.AreEqual(TestProvider.ComplexEventId.ToString(), actual.Properties["EventId"]);
                Assert.AreEqual(nameof(TestProvider.Complex) + "/Extension", actual.Properties["EventName"]);
                Assert.AreEqual(activityId.ToString(), actual.Properties["ActivityID"]);
                Assert.AreEqual("0x8000F00000000001", actual.Properties["Keywords"]);
                Assert.AreEqual(((int)EventChannel.Debug).ToString(), actual.Properties["Channel"]);
                Assert.AreEqual("Extension", actual.Properties["Opcode"]);
                Assert.AreEqual("0x00000020", actual.Properties["Task"]);
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public async Task ReportSeverityLevel()
        {
            //bool isCalled = false;
            // using (TestProvider provider = new TestProvider())
            using (EtwTelemetryModule module = new EtwTelemetryModule())
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = TestProvider.ProviderName
                });
                module.Initialize(GetTestTelemetryConfiguration());
                TestProvider.Log.Info("Hello!");
                TestProvider.Log.Warning(1, 2);

                // There's going to be a delay around 2000ms before the events reaches the channel.
                await Task.Delay(EtwTelemetryModuleTests.channelDelay);

                // The very 1st event is for the manifest.
                Assert.AreEqual(3, this.adapterHelper.Channel.SentItems.Length);
                Assert.AreEqual(SeverityLevel.Information, ((TraceTelemetry)this.adapterHelper.Channel.SentItems[1]).SeverityLevel);
                Assert.AreEqual(SeverityLevel.Warning, ((TraceTelemetry)this.adapterHelper.Channel.SentItems[2]).SeverityLevel);
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public async Task HandlesDuplicatePropertyNames()
        {
            using (var module = new EtwTelemetryModule())
            {
                module.Sources.Add(new EtwListeningRequest()
                {
                    ProviderName = TestProvider.ProviderName
                });
                module.Initialize(GetTestTelemetryConfiguration());

                TestProvider.Log.Tricky(7, "TrickyEvent", "Actual message");

                // There's going to be a delay around 2000ms before the events reaches the channel.
                await Task.Delay(EtwTelemetryModuleTests.channelDelay);

                Assert.AreEqual(2, this.adapterHelper.Channel.SentItems.Length);
                TraceTelemetry telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems[1];

                Assert.AreEqual("Manifest message", telemetry.Message);
                Assert.AreEqual(SeverityLevel.Information, telemetry.SeverityLevel);
                Assert.AreEqual("Actual message", telemetry.Properties["Message"]);
                Assert.AreEqual("7", telemetry.Properties["EventId"]);
                Assert.AreEqual("Tricky", telemetry.Properties["EventName"]);
                Assert.AreEqual("7", telemetry.Properties[telemetry.Properties.Keys.First(key => key.StartsWith("EventId") && key != "EventId")]);
                Assert.AreEqual("TrickyEvent", telemetry.Properties[telemetry.Properties.Keys.First(key => key.StartsWith("EventName") && key != "EventName")]);
            }
        }

        [TestMethod]
        [TestCategory("EtwTelemetryModule")]
        public async Task ReactsToConfigurationChanges()
        {
            using (var module = new EtwTelemetryModule())
            {
                var listeningRequest = new EtwListeningRequest();
                listeningRequest.ProviderName = TestProvider.ProviderName;
                module.Sources.Add(listeningRequest);

                module.Initialize(GetTestTelemetryConfiguration());

                TestProvider.Log.Info("Hey!");
                TestProvider.Log.Warning(1, 2);

                // Now request reporting events only with certain keywords
                listeningRequest.Keywords = (ulong)TestProvider.Keywords.NonRoutine;
                module.Initialize(GetTestTelemetryConfiguration(resetChannel: false));

                TestProvider.Log.Info("Hey again!");
                TestProvider.Log.Warning(3, 4);

                // There's going to be a delay around 2000ms before the events reaches the channel.
                await Task.Delay(EtwTelemetryModuleTests.channelDelay);

                List<TraceTelemetry> expectedTelemetry = new List<TraceTelemetry>();
                TraceTelemetry traceTelemetry = new TraceTelemetry("Hey!", SeverityLevel.Information);
                traceTelemetry.Properties["information"] = "Hey!";
                expectedTelemetry.Add(traceTelemetry);
                traceTelemetry = new TraceTelemetry("Warning!", SeverityLevel.Warning);
                traceTelemetry.Properties["i1"] = 1.ToString();
                traceTelemetry.Properties["i2"] = 2.ToString();
                expectedTelemetry.Add(traceTelemetry);
                // Note that second informational event is not expected
                traceTelemetry = new TraceTelemetry("Warning!", SeverityLevel.Warning);
                traceTelemetry.Properties["i1"] = 3.ToString();
                traceTelemetry.Properties["i2"] = 4.ToString();
                expectedTelemetry.Add(traceTelemetry);

                CollectionAssert.AreEqual(
                    expectedTelemetry,
                    this.adapterHelper.Channel.SentItems.Where(item => !((TraceTelemetry)item).Properties["EventId"].Equals("65534")).ToList(),
                    new TraceTelemetryComparer(),
                    "Reported events are not what was expected");
            }
        }
    }
}
