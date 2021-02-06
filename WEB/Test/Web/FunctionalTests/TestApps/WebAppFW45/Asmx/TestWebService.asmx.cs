namespace WebAppFW45.Asmx
{
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.ServiceModel.Web;
    using System.Web.Script.Services;
    using System.Web.Services;

    /// <summary>
    /// Summary description for TestWebService
    /// </summary>
    [WebService(Namespace = "http://TestWebService")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Allow this Web Service to be called from script, using ASP.NET AJAX: 
    [System.Web.Script.Services.ScriptService]
    public class TestWebService : WebService
    {
        [ScriptMethod(UseHttpGet = true)]
        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [WebMethod]
        public string HelloPost(bool needThrow)
        {
            if (needThrow)
            {
                throw new WebFaultException(HttpStatusCode.ServiceUnavailable);
            }

            return "HelloPost";
        }
    }
}
