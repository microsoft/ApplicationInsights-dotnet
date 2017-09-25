namespace Wcf45Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Web;
    using System.Text;

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class WcfEndpoint : IWcfEndpoint
    {
        public string GetMethodTrue()
        {
            return "Hello from WcfEndpoint.GetMethod";
        }
    }
}
