namespace Microsoft.ApplicationInsights.Web.Helpers
{
    using System.Web;
    using Microsoft.ApplicationInsights.Web.Implementation;

    /// <summary>
    /// Extension methods for HttpContext to help with testing.
    /// </summary>
    internal static class HttpContextTestExtensions
    {
        internal static HttpContext WithAuthCookie(this HttpContext context, string cookieString)
        {
            context.AddRequestCookie(
                new HttpCookie(
                    RequestTrackingConstants.WebAuthenticatedUserCookieName,
                    HttpUtility.UrlEncode(cookieString)) 
                { 
                    HttpOnly = true,
                    Secure = true
                });
            
            return context;
        }
    }
}
