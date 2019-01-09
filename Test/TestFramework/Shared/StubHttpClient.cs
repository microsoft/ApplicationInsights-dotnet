using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.TestFramework
{
    internal class StubHttpClient : HttpClient
    {
         public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSendAsync;
         public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
         {
           return this.OnSendAsync(request, cancellationToken);
         }
    }
}
