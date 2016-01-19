namespace Functional
{
    using Functional.Helpers;
    using Functional.IisExpress;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Diagnostics;
    using System.IO;
    using System.Net;

    [TestClass]
    public class TestsExcCollectingFw40Mvc : ExceptionTelemetryTestBase
    {
        private const int TimeoutInMs = 10000;
        private const string ApplicationDirName = "TestApps_Mvc4_MediumTrust_App";

        [TestInitialize]
        public void TestInitialize()
        {
            var applicationDirectory = Path.Combine(
                Directory.GetCurrentDirectory(),
                ApplicationDirName);

            Trace.WriteLine("Application directory:" + applicationDirectory);

            this.StartWebAppHost(
                new SingleWebHostTestConfiguration(
                    new IisExpressConfiguration
                    {
                        ApplicationPool = IisExpressAppPools.Clr4IntegratedAppPool,
                        Path = applicationDirectory,
                        Port = 44918,
                    })
                {
                    TelemetryListenerPort = 4004,
                    IKey = "5552B788-A47E-4E6D-866B-5C8BD7AC72C1",
                    AttachDebugger = System.Diagnostics.Debugger.IsAttached
                });
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.StopWebAppHost();
        }

        [TestMethod]
        [Owner("abaranch")]
        [DeploymentItem(@"..\TestApps\Mvc4_MediumTrust\App", "TestApps_Mvc4_MediumTrust_App")]
        public void Test4MediumRequestAndExceptionAreTrackedForResourceNotFoundException()
        {
            var request = (HttpWebRequest) WebRequest.Create(this.Config.ApplicationUri + "/wrongController?id=13");

            try
            {
                request.GetResponse();
                Assert.Fail("Task was supposed to fail.");
            }
            catch (WebException exp)
            {
                Trace.WriteLine(exp.Message);
            }

            var items = Listener
                .ReceiveItemsOfTypes<TelemetryItem<RequestData>, TelemetryItem<ExceptionData>>(2, TimeoutInMs);

            // One item is request, the other one is exception.
            int requestItemIndex = (items[0] is TelemetryItem<RequestData>) ? 0 : 1;
            int exceptionItemIndex = (requestItemIndex == 0) ? 1 : 0;

            var exceptionItem = (TelemetryItem<ExceptionData>) items[exceptionItemIndex];
            this.ValidateExceptionTelemetry(
                exceptionItem,
                (TelemetryItem<RequestData>) items[requestItemIndex],
                1);

            this.ValidateExceptionDetails(
                exceptionItem.Data.BaseData.Exceptions[0],
                "System.Web.HttpException",
                "The controller for path '/wrongController' was not found or does not implement IController.",
                "System.Web.Mvc.DefaultControllerFactory.GetControllerInstance",
                "System.Web.Mvc, Version=",
                8);
        }
    }
}
