namespace WebAppFW45.Wcf
{
    using System.ServiceModel;
    using System.ServiceModel.Web;

    [ServiceContract(Namespace = "http://WcfEndpoint")]
    public interface IWcfEndpoint
    {
        [OperationContract(Name = "PostMethod")]
        [WebInvoke(Method = "POST", UriTemplate = "PostMethod")]
        string PostMethod(bool needThrow);

        [OperationContract(Name = "GetMethod")]
        [WebGet(UriTemplate = "GetMethod/fail/{needThrow}")]
        string GetMethod(string needThrow);

        [OperationContract(Name = "GetMethodTrue")]
        [WebGet(UriTemplate = "GetMethodTrue")]
        string GetMethodTrue();

        [OperationContract(IsOneWay = true)]
        void OneWayMethod();
    }
}