using Microsoft.AspNet.Http;
namespace Microsoft.ApplicationInsights.AspNet.Implementation
{
    public class HttpContextHolder
    {
        public HttpContext Context { get; set; }
    }
}