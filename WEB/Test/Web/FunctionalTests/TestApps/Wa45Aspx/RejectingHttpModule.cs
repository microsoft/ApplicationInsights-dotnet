namespace Wa45Aspx
{
    using System;
    using System.Net;
    using System.Web;

    public class RejectingHttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += Context_BeginRequest;
        }

        private void Context_BeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            application.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            application.Response.End();
        }

        public void Dispose()
        {
        }
    }
}