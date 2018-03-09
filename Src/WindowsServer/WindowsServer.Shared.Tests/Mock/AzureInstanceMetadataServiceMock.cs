namespace Microsoft.ApplicationInsights.WindowsServer.Mock
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    internal class AzureInstanceMetadataServiceMock : IDisposable
    {
        private readonly HttpListener listener;
        private readonly CancellationTokenSource cts;

        public AzureInstanceMetadataServiceMock(string url, Action<HttpListenerContext> onRequest = null)
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
            this.listener.Start();
            this.cts = new CancellationTokenSource();

            Task.Run(
                () =>
                {
                    if (!this.cts.IsCancellationRequested)
                    {
                        HttpListenerContext context = this.listener.GetContext();
                        if (onRequest != null)
                        {
                            onRequest(context);
                        }
                        else
                        {
                            context.Response.StatusCode = 200;
                        }

                        context.Response.OutputStream.Close();
                        context.Response.Close();
                    }
                },
                this.cts.Token);
        }

        public void Dispose()
        {
            this.cts.Cancel(false);
            this.listener.Abort();
            ((IDisposable)this.listener).Dispose();
            this.cts.Dispose();
        }
    }
}
