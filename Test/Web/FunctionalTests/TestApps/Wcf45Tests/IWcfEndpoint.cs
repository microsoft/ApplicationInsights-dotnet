namespace Wcf45Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.Text;

    [ServiceContract]
    public interface IWcfEndpoint
    {
        [OperationContract(Name = "GetMethodTrue")]
        [WebGet(UriTemplate = "GetMethodTrue")]
        string GetMethodTrue();
    }
}
