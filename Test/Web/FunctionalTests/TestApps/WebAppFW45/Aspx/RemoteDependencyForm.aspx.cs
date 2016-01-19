namespace WebAppFW45.Aspx
{
    using System;
    using System.Threading;

    public partial class RemoteDependencyForm : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Trace.Write("RemoteDependencyForm.Page_Load started successfully");
            // We are using EventSource callbacks to monitoring RDD.
            // The callbacks for BeginGetRequest and EndGetResponse can fire in reverse order because they are on different threads.
            // if that happens we don't monitor rdd call
            // increasing the time to process the page to decrease probability of having EndGetResponse and than BeginGetRequest callback order.
            Thread.Sleep(2000);
            System.Diagnostics.Trace.Write("RemoteDependencyForm.Page_Load finished successfully");
        }
    }
}