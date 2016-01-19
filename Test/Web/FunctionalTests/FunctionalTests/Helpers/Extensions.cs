
using System.IO;
using System.Net;
using System.Text;

namespace FunctionalTests.Helpers
{
    /// <summary>
    /// Helper extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets content of the body for HttpListner request
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static string GetContent(this HttpListenerRequest request)
        {
            var result = string.Empty;

            if (request.HasEntityBody)
            {
                using (var requestInputStream = request.InputStream)
                {
                    var encoding = request.ContentEncoding;
                    using (var reader = new StreamReader(requestInputStream, encoding))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets content of the body for Http Web Response
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static string GetContent(this WebResponse response)
        {
            var result = string.Empty;

            using (var requestInputStream = response.GetResponseStream())
            {
                if (requestInputStream != null)
                {
                    using (var reader = new StreamReader(requestInputStream, Encoding.Default))
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }

            return result;
        }
    }
}
