namespace FuncTest
{
    using System.Linq;
    using System.Threading;

    using FuncTest.Helpers;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    partial class RddTests
    {
        [Description("Verify ExceptionStatistics is collected in ASPX 4.5.1 application x64 app pool")]
        [Owner("abaranch")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolder)]
        [TestMethod]
        [Ignore]
        // Fails because Web Nuget does not have exception statistics dependency yet
        public void TestExceptionStatisticsRecievedFrom451ApplicationX64AppPool()
        {
            if (!DotNetVersionCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.TestExceptionStatisctics(aspx451TestWebApplication);
        }

        [TestMethod]
        [Description("Verify ExceptionStatistics is collected in ASPX 4.5.1 application x32 app pool")]
        [Owner("abaranch")]
        [TestCategory("FUNC")]
        [DeploymentItem("..\\TestApps\\ASPX451\\App\\", Aspx451AppFolderWin32)]
        [Ignore]
        // Fails because Web Nuget does not have exception statistics dependency yet
        public void TestExceptionStatisticsRecievedFrom451ApplicationX32AppPool()
        {
            if (!DotNetVersionCheck.IsNet451Installed)
            {
                Assert.Inconclusive(".Net Framework 4.5.1 is not installed");
            }

            this.TestExceptionStatisctics(aspx451TestWebApplicationWin32);
        }

        /// <summary>
        /// Verify ExceptionStatistics is collected
        /// </summary>
        private void TestExceptionStatisctics(TestWebApplication testApplication)
        {
            //testApplication.DoTest(
            //    (application, platform) =>
            //    {
            //        // Throw 1 handled and 1 unhandled exception
            //        const string queryString = "handledCount=1&isUnhandled=true";
            //        const string pageName = "PageThrowsExceptions.aspx";

            //        application.ExecuteAnonymousRequest(pageName, queryString);

            //        //// The above request would have trigged APMC into action and APMC should have collected exception statistics  
            //        //// AIC/SDK requests for this every 5 seconds and sends to the fake end point.
            //        //// Listen in the fake endpoint and see if the exception statistics telemtry is captured
            //        //// Sleep for some secs to give SDK the time to sent the events to Fake DataPlatform.
            //        Thread.Sleep(10000);

            //        var allItems = platform.GetAllReceivedDataItems();

            //        var items = allItems.Where(i => i.GetFieldValue("name").Equals("Microsoft.ApplicationInsights.Metric")).ToArray();

            //        // We should recieve 1 item for unhandled exception and 1 for handled exception
            //        Assert.AreEqual(2, items.Length, "Total Count of Exception Statistics items collected in wrong.");
            //    });
        }
    }

    
}
