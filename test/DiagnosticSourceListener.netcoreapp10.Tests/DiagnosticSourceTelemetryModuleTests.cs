//-----------------------------------------------------------------------
// <copyright file="DiagnosticSourceTelemetryModuleTests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.DiagnosticSourceListener.Tests
{
    using System;
    using System.Linq;
    using Microsoft.ApplicationInsights.Tests;
    using Microsoft.ApplicationInsights.Tracing.Tests;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.CommonTestShared;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("DiagnosticSourceListener")]
    public sealed class DiagnosticSourceTelemetryModuleTests : IDisposable
    {
        private readonly AdapterHelper adapterHelper = new AdapterHelper();

        public void Dispose() => this.adapterHelper.Dispose();

        [TestMethod]
        public void ThrowsWhenNullConfigurationPassedToInitialize()
        {
            using (var module = new DiagnosticSourceTelemetryModule())
            {
                ExceptionAssert.Throws<ArgumentNullException>(() =>
                {
                    module.Initialize(null);
                });
            }
        }

        [TestMethod]
        public void ReportsSingleEvent()
        {
            using (var module = new DiagnosticSourceTelemetryModule())
            {
                var testDiagnosticSource = new TestDiagnosticSource();
                var listeningRequest = new DiagnosticSourceListeningRequest(testDiagnosticSource.Name);
                module.Sources.Add(listeningRequest);

                module.Initialize(GetTestTelemetryConfiguration());

                testDiagnosticSource.Write("Hey!", new { Prop1 = 1234 });

                TraceTelemetry telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.First();
                Assert.AreEqual("Hey!", telemetry.Message);
                Assert.AreEqual(testDiagnosticSource.Name, telemetry.Properties["DiagnosticSource"]);
                Assert.AreEqual(SeverityLevel.Information, telemetry.SeverityLevel);
                Assert.AreEqual(1234.ToString(), telemetry.Properties["Prop1"]);
                string expectedVersion = SdkVersionHelper.GetExpectedSdkVersion(typeof(DiagnosticSourceTelemetryModule), prefix: "dsl:");
                Assert.AreEqual(expectedVersion, telemetry.Context.GetInternalContext().SdkVersion);
            }
        }

        [TestMethod]
        public void HandlesPropertiesWithNullValues()
        {
            using (var module = new DiagnosticSourceTelemetryModule())
            {
                var testDiagnosticSource = new TestDiagnosticSource();
                var listeningRequest = new DiagnosticSourceListeningRequest(testDiagnosticSource.Name);
                module.Sources.Add(listeningRequest);

                module.Initialize(GetTestTelemetryConfiguration());

                testDiagnosticSource.Write("Hey!", new { Prop1 = (object)null });

                TraceTelemetry telemetry = (TraceTelemetry)this.adapterHelper.Channel.SentItems.First();
                Assert.AreEqual("Hey!", telemetry.Message);
                Assert.AreEqual(string.Empty, telemetry.Properties["Prop1"]);
            }
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
    }
}
