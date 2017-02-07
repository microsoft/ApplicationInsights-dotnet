namespace FunctionalTests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;

    using Functional.Helpers;
    using Functional.IisExpress;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using PerfCounterCollector.FunctionalTests;

    [TestClass]
    public class Test45 : SingleWebHostTestBase
    {
        private const int TimeoutInMs = 15000;
        
        private const string TestWebApplicaionSourcePath = @"TestApps\TestApp45\App";
        private const string TestWebApplicaionDestPath = @"TestsPerformanceCollector45";

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                TestWebApplicaionDestPath);

            Trace.WriteLine("Application directory:" + applicationDirectory);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 5678,
                    })
                {
                    TelemetryListenerPort = 4554,
                    QuickPulseListenerPort = 4555,
                    // AttachDebugger = Debugger.IsAttached,
                    IKey = "fafa4b10-03d3-4bb0-98f4-364f0bdf5df8",
                });

            base.LaunchAndVerifyApplication();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.StopWebAppHost();
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void DefaultCounterCollection()
        {
            CommonTests.DefaultCounterCollection(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void CustomCounterCollection()
        {
            CommonTests.CustomCounterCollection(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [Description("Tests that non existent counters are not collected and wont affect other counters")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]        
        public void NonExistentCounter()
        {
            CommonTests.NonExistentCounter(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [Description("Tests that non existent counters which use placeholders are not collected and wont affect other counters")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void NonExistentCounterWhichUsesPlaceHolder()
        {
            CommonTests.NonExistentCounterWhichUsesPlaceHolder(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void NonParsableCounter()
        {
            CommonTests.NonParsableCounter(this.Listener);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void QuickPulseAggregates()
        {
            CommonTests.QuickPulseAggregates(this.QuickPulseListener, this.HttpClient);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void QuickPulseDocuments()
        {
            CommonTests.QuickPulseDocuments(this.QuickPulseListener, this);
        }

        [TestMethod]
        [Owner("alkaplan")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void QuickPulseTopCpuProcesses()
        {
            CommonTests.QuickPulseTopCpuProcesses(this.QuickPulseListener, this);
        }
    }
}