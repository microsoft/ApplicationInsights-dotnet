namespace System
{
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    internal static class WebRequestExtensions
    {
        public static Task<Stream> GetRequestStreamAsync(this WebRequest request)
        {
            return Task.Factory.FromAsync(
                                    (asyncCallback, asyncState) => request.BeginGetRequestStream(asyncCallback, asyncState),
                                    asyncResult => request.EndGetRequestStream(asyncResult),
                                    null);
        }

        public static Task<WebResponse> GetResponseAsync(this WebRequest request)
        {
            return Task.Factory.FromAsync(
                                    (asyncCallback, asyncState) => request.BeginGetResponse(asyncCallback, asyncState),
                                    asyncResult => request.EndGetResponse(asyncResult),
                                    null);
        }
    }
}
