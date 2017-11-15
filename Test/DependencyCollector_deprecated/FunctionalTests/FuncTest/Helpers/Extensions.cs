// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <summary>
// Extensions to help with Http Listener
// </summary>
// ----
namespace FuncTest.Helpers
{
    using System.IO;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Helper extensions
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets content of the body for HttpListener request
        /// </summary>
        /// <param name="request">request object</param>
        /// <returns>the result</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
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
