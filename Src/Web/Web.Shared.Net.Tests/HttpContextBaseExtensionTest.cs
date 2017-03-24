namespace Microsoft.ApplicationInsights.Web
{
    using System.Web;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ApplicationInsights.Web.Implementation;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpContextBaseExtensionTest
    {
        [TestMethod]
        public void GetRequestTelemetryReturnsNullForNullContextBase()
        {
            Assert.IsNull(HttpContextBaseExtension.GetRequestTelemetry(null));
        }

        [TestMethod]
        public void GetRequestTelemetryReturnsNullIfRequestNotAvailable()
        {
            Assert.IsNull(HttpModuleHelper.GetFakeHttpContextBase().GetRequestTelemetry());
        }

        [TestMethod]
        public void GetRequestTelemetryReturnsRequestTelemetryFromItems()
        {
            var expected = new RequestTelemetry();

            var context = HttpModuleHelper.GetFakeHttpContextBase();
            context.ApplicationInstance = HttpModuleHelper.GetFakeHttpApplication();
            context.ApplicationInstance.Context.Items.Add(RequestTrackingConstants.RequestTelemetryItemName, expected);
            
            var actual = context.GetRequestTelemetry();

            Assert.AreSame(expected, actual);
        }
    }
}
