namespace Functional
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using Functional.Helpers;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestsUnhandledExceptionsFW40
    {
        private const string TestWebApplicaionSourcePath = @"..\TestApps\ConsoleAppFW40\";
        private const string TestWebApplicaionDestPath = "TestApps_TestsUnhandledExceptionsFW40_App";

        private const int TestListenerTimeoutInMs = 30000;

        protected TelemetryHttpListenerObservable Listener { get; private set; }

        protected EtwEventSession EtwSession { get; private set; }

        private string applicationDirectory;

        [TestInitialize]
        public void TestInitialize()
        {
            this.applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                TestWebApplicaionDestPath);

            Trace.WriteLine("Application directory:" + this.applicationDirectory);

            this.Listener = new TelemetryHttpListenerObservable("http://localhost:4002/v2/track/");
            this.Listener.Start();

            this.EtwSession = new EtwEventSession();
            this.EtwSession.Start();

            Thread.Sleep(5000);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            try
            {
                this.Listener.Stop();
            }
            catch (Exception)
            {
            }
            
            if (this.EtwSession.FailureDetected)
            {
                Assert.Fail("Read test output. There are errors found in application trace.");
            }

            try
            {
                this.EtwSession.Stop();
            }
            catch (Exception)
            {
            }
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TaskSchedulerUnobservedExceptionIsTracked()
        {
            var process = this.StartProcess("unobserved");

            try
            {
                var exceptions = Listener.ReceiveItemsOfType<TelemetryItem<ExceptionData>>(1, TestListenerTimeoutInMs)[0];
                
                Assert.AreEqual("System.AggregateException", exceptions.data.baseData.exceptions[0].typeName);
            }
            finally
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(TestWebApplicaionSourcePath, TestWebApplicaionDestPath)]
        public void TaskSchedulerUnhandledExceptionIsTracked()
        {
            var process = this.StartProcess("unhandled");

            try
            {
                var exception = Listener.ReceiveItemsOfType<TelemetryItem<ExceptionData>>(1, TestListenerTimeoutInMs)[0];

                Assert.AreEqual("System.Exception", exception.data.baseData.exceptions[0].typeName);
            }
            finally
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
        }

        private Process StartProcess(string arguments)
        {
            var startInfo = new ProcessStartInfo(this.applicationDirectory + @"\ConsoleAppFW40.exe");
            startInfo.Arguments = arguments;
            startInfo.CreateNoWindow = true;
            startInfo.ErrorDialog = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;

            return Process.Start(startInfo);
        }
    }
}
