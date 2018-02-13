namespace Microsoft.ApplicationInsights.Tests
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;

    internal class HttpWebRequestUtils : IDisposable
    {
        private ManualResetEvent allDone = new ManualResetEvent(false);

        public void Dispose()
        {
            if (this.allDone != null)
            {
                this.allDone.Dispose();
            }
        }

        internal void ExecuteAsyncHttpRequest(string url, HttpMethod httpMethod)
        {
            // Create a new HttpWebRequest object.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.ContentType = "application/x-www-form-urlencoded";

            request.Method = httpMethod.Method;

            if (httpMethod == HttpMethod.Post)
            {
                // start the asynchronous operation
                request.BeginGetRequestStream(new AsyncCallback(this.GetRequestStreamCallback), request);
            }
            else
            {
                // Start the asynchronous operation to get the response
                request.BeginGetResponse(this.GetResponseCallback, request);
            }

            // Keep the main thread from continuing while the asynchronous 
            // operation completes. A real world application 
            // could do something useful such as updating its user interface. 
            this.allDone.WaitOne();
        }

        private void GetRequestStreamCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

                // End the operation
                using (Stream postStream = request.EndGetRequestStream(asynchronousResult))
                {
                    string postData = "Please enter the input data to be posted:";

                    // Convert the string into a byte array. 
                    byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                    // Write to the request stream.
                    postStream.Write(byteArray, 0, postData.Length);
                }

                // Start the asynchronous operation to get the response
                request.BeginGetResponse(this.GetResponseCallback, request);
            }
            catch (Exception)
            {
                // swallowing to not break up the debugging thread

                // set the state to signaled as we don't process further
                this.allDone.Set();
            }
        }

        private void GetResponseCallback(IAsyncResult asynchronousResult)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)asynchronousResult.AsyncState;

                // End the operation
                using (HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(asynchronousResult))
                {
                    Stream streamResponse = response.GetResponseStream();
                    using (StreamReader streamRead = new StreamReader(streamResponse))
                    {
                        string responseString = streamRead.ReadToEnd();
                    }
                }
            }
            catch (Exception)
            {
                // swallowing to not break up the debugging thread
            }
            finally
            {
                this.allDone.Set();
            }
        }
    }
}
