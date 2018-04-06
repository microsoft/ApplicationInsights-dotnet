namespace Microsoft.ApplicationInsights.WindowsServer.Mock
{
    using System.IO;
    using System.Net;
    using System.Text;
#if NETCORE
    using Microsoft.AspNetCore.Http;
#endif

    /// <summary>
    /// Extensions to aid in testing between .NET Full Framework and .NET Core.
    /// </summary>
    internal static class NetPlatformHttpResponseTypesExtension
    {
#if NETCORE

        public static void SetContentLength(this HttpResponse resp, long len)
        {
            resp.ContentLength = len;
        }

        public static void WriteStreamToBody(this HttpResponse resp, MemoryStream content)
        {
            content.WriteTo(resp.Body);
        }

        public static void SetContentEncoding(this HttpResponse resp, Encoding enc) { }

#elif NET45
        
        public static void SetContentLength(this HttpListenerResponse resp, long len)
        {
            resp.ContentLength64 = (int)len;
        }

        public static void WriteStreamToBody(this HttpListenerResponse resp, MemoryStream content)
        {
            content.WriteTo(resp.OutputStream);
        }

        public static void SetContentEncoding(this HttpListenerResponse resp, Encoding enc)
        {
            resp.ContentEncoding = enc;
        }
#else
#error Unkown framework
#endif
    }
}
