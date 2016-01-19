// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RequestHelper.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   Defines the Request type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FuncTest.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Security.Principal;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    /// <summary>
    /// The request.
    /// </summary>
    public class RequestHelper
    {
        /// <summary>
        /// The default method.
        /// </summary>
        private const string DefaultMethod = "GET";

        /// <summary>
        /// The default content type.
        /// </summary>
        private const string DefaultContentType = "application/xml; charset=utf-8";

        /// <summary>
        /// The cancellation store per request.
        /// </summary>
        private static readonly Dictionary<string, CancellationTokenSource> CancellationStorePerRequest = new Dictionary<string, CancellationTokenSource>();

        /// <summary>
        /// Executes anonymous request.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="count">
        /// The count.
        /// </param>
        /// <param name="sync">
        /// The sync.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <param name="content">
        /// The content.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <param name="timeoutMs">
        /// The timeout milliseconds.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/> indicating that requests were submitted successfully (no check of response in case of async).
        /// </returns>
        public static bool ExecuteAnonymousRequests(
            string uri,
            out string responseText,
            int count = 1,
            bool sync = true,
            string method = DefaultMethod,
            XNode content = null,
            string contentType = DefaultContentType,
            int timeoutMs = 120 * 1000)
        {
            responseText = string.Empty;
            bool startResult = true;
            for (int index = 0; index < count; index++)
            {
                startResult &= ExecuteAnonymousRequest(uri, out responseText, sync, method, content, contentType, timeoutMs);
            }

            return startResult;
        }

        /// <summary>
        /// Executes anonymous request.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="sync">
        /// The sync.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <param name="content">
        /// The content.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <param name="timeoutMs">
        /// The timeout milliseconds.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool ExecuteAnonymousRequest(
            string uri,
            out string responseText,
            bool sync = true,
            string method = DefaultMethod,
            XNode content = null,
            string contentType = DefaultContentType,
            int timeoutMs = 120 * 1000
            )
        {
            WebResponse response = null;
            responseText = string.Empty;
            bool result = true;

            try
            {
                WebRequest request = PrepareRequest(uri, method, content, contentType, timeoutMs);
                if (sync)
                {
                    response = request.GetResponse();
                    Trace.TraceInformation("Executed Web Request To: {0}", uri);
                }
                else
                {
                    request.BeginGetResponse(EmptyWebRequestCallback, new object());
                    Trace.TraceInformation("Initiated Web Request To: {0}", uri);
                }
            }
            catch (WebException webEx)
            {
                WebExceptionStatus status = webEx.Status;
                if (status != WebExceptionStatus.Success)
                {
                    result = false;
                }

                Trace.TraceInformation("Web Request execution failed... Uri: {0} Exception: {1}", uri, webEx.Message);
            }
            catch (System.Net.Sockets.SocketException socketExc)
            {
                result = false;
                Trace.TraceInformation("Web Request execution failed... Uri: {0} Exception: {1}", uri, socketExc.Message);
            }
            finally
            {
                if (response != null)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    if (null != reader)
                    {
                        responseText = reader.ReadToEnd();
                    }
                    else 
                    {
                        responseText = "Failed to get stream reader from response stream";
                    }
                    response.Close();
                }
            }

            return result;
        }

        public static bool ExecuteAnonymousRequestWithResponse(
            string uri,
            out string responseBody)
        {
            WebResponse response = null;
            bool result = true;
            responseBody = string.Empty;

            try
            {
                WebRequest request = PrepareRequest(uri, DefaultMethod, null, DefaultContentType, 120 * 1000);
                response = request.GetResponse();
                Trace.TraceInformation("Executed Web Request To: {0}", uri);
            }
            catch (WebException webEx)
            {
                WebExceptionStatus status = webEx.Status;
                if (status != WebExceptionStatus.Success)
                {
                    result = false;
                }

                responseBody = GetResponseBody(webEx);

                Trace.TraceInformation("Web Request execution failed... Uri: {0} Exception: {1}", uri, webEx.Message);
            }
            catch (System.Net.Sockets.SocketException socketExc)
            {
                result = false;
                Trace.TraceInformation("Web Request execution failed... Uri: {0} Exception: {1}", uri, socketExc.Message);
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }

            return result;
        }

        /// <summary>
        /// Executes request in a loop in the background.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="safetyTimeoutInMinutes">
        /// The safety timeout in minutes.
        /// </param>
        public static void ExecuteRequestsContinuouslyInBackground(string uri, int safetyTimeoutInMinutes = 15)
        {
            if (!CancellationStorePerRequest.ContainsKey(uri))
            {
                CancellationStorePerRequest.Add(uri, new CancellationTokenSource());
            }

            TimeSpan safetyTimeout = new TimeSpan(0, safetyTimeoutInMinutes, 0);

            CancellationToken token = CancellationStorePerRequest[uri].Token;
            Task.Factory.StartNew(
                () =>
                {
                    DateTime startTime = DateTime.Now;
                    int requestCount = 0;
                    while ((DateTime.Now - startTime) < safetyTimeout)
                    {
                        token.ThrowIfCancellationRequested();
                        string response;
                        ExecuteAnonymousRequest(uri, out response);
                        if (requestCount++ % 10 == 0)
                        {
                            Trace.TraceInformation("Executed {0} requests to: {1} ...", requestCount, uri);
                        }

                        Thread.Yield(); // TODO: Change to Task.Delay if 4.5 is available or TimerCallback if only 4.0 is available, otherwise the whole purpose of Task is lost...
                    }
                },
                token);
        }

        /// <summary>
        /// Stops continuous requests to the specified uri if any queued
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        public static void StopContinuousRequests(string uri)
        {
            if (CancellationStorePerRequest.ContainsKey(uri))
            {
                CancellationStorePerRequest[uri].Cancel();
                CancellationStorePerRequest.Remove(uri);
            }
        }

        /// <summary>
        /// Prepares the request based on the parameters.
        /// </summary>
        /// <param name="uri">
        /// The uri.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <param name="content">
        /// The content.
        /// </param>
        /// <param name="contentType">
        /// The content type.
        /// </param>
        /// <param name="timeoutMs">
        /// The timeout milliseconds.
        /// </param>
        /// <returns>
        /// The <see cref="WebRequest"/>.
        /// </returns>
        private static WebRequest PrepareRequest(string uri, string method, XNode content, string contentType, int timeoutMs)
        {
            WebRequest request = WebRequest.Create(uri);
            request.Method = method;
            request.AuthenticationLevel = AuthenticationLevel.None;
            request.ImpersonationLevel = TokenImpersonationLevel.Anonymous;
            request.Timeout = timeoutMs;

            if (content != null)
            {
                string postData = content.ToString();
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = contentType;
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
            }

            return request;
        }

        /// <summary>
        /// The empty web request callback.
        /// </summary>
        /// <param name="asynchronousResult">
        /// The asynchronous result.
        /// </param>
        private static void EmptyWebRequestCallback(IAsyncResult asynchronousResult)
        {
        }

        private static string GetResponseBody(WebException ex)
        {
            var webResponse = ex.Response as HttpWebResponse;
            string response = GetHttpResponseContentSafely(webResponse);
            return response;
        }

        /// <summary>Safely obtains the http web response content if possible.</summary>
        /// <param name="response">The http web response.</param>
        /// <returns>The response content or null if not available.</returns>
        private static string GetHttpResponseContentSafely(HttpWebResponse response)
        {
            if (response == null)
            {
                return null;
            }

            // only response stream related operations are wrapped in try...catch
            try
            {
                Stream stream = response.GetResponseStream();
                if (stream == null)
                {
                    return null;
                }

                // stream may not always be starting from the beginning
                // reset stream
                stream.Position = 0;

                using (var sr = new StreamReader(stream))
                {
                    string content = sr.ReadToEnd();
                    return content;
                }
            }
            catch (Exception)
            {
                // I've seen situations where the stream we get from the http web response is already closed
                // when we get it, which means that we can't read the response content
                // throwing now doesn't really help us provide more useful information about the rest call that failed,
                // so no need to do anything
                return null;
            }
        }
    }
}
