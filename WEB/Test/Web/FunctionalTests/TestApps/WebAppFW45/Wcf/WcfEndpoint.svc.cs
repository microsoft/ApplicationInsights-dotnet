namespace WebAppFW45.Wcf
{
    using System.Net;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WcfEndpoint : IWcfEndpoint
    {
        public string PostMethod(bool needThrow)
        {
            if (needThrow)
            {
                throw new WebFaultException(HttpStatusCode.ServiceUnavailable);
            }

            return "Hello from WcfEndpoint.PostMethod";
        }
        
        public string GetMethod(string needThrow)
        {
            if (bool.Parse(needThrow))
            {
                throw new WebFaultException(HttpStatusCode.ServiceUnavailable);
            }

            return "Hello from WcfEndpoint.GetMethod";
        }

        public string GetMethodTrue()
        {
            return "Hello from WcfEndpoint.GetMethod";
        }

        public void OneWayMethod()
        {
        }
    }
}
