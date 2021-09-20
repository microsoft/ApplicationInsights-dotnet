#if NETCOREAPP
namespace Microsoft.ApplicationInsights.TestFramework.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RedirectHttpHandlerTests
    {
        private const string helloString = "Hello World!";

        private const string Localurl1 = "http://localhost:1111";
        private const string Localurl2 = "http://localhost:2222";

        /// <summary>
        /// Verify behavior of HttpClient without <see cref="RedirectHttpHandler"/>.
        /// Setup two local servers, where server #1 will redirect requests to #2.
        /// </summary>
        [TestMethod]
        public async Task DefaultUseCase()
        {
            int counter1 = 0;
            int counter2 = 0;

            using var localServer1 = new LocalInProcHttpServer(Localurl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    counter1++;
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", Localurl2);
                    await httpContext.Response.WriteAsync("redirect");
                },
            };

            using var localServer2 = new LocalInProcHttpServer(Localurl2)
            {
                ServerLogic = async (httpContext) =>
                {
                    counter2++;
                    await httpContext.Response.WriteAsync(helloString);
                },
            };

            var client = new MyCustomClient(url: Localurl1);

            // Default behavior. 1st server will redirect to 2nd.
            var testStr1 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr1);
            Assert.AreEqual(1, counter1);
            Assert.AreEqual(1, counter2);

            // Default behavior. Nothing is cached, repeat previous workflow.
            var testStr2 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr2);
            Assert.AreEqual(2, counter1);
            Assert.AreEqual(2, counter2);
        }

        /// <summary>
        /// Verify behavior of HttpClient and <see cref="RedirectHttpHandler"/>.
        /// Setup two local servers, where server #1 will redirect requests to #2.
        /// After the first request, it is expected that the client will cache the redirect.
        /// Additional requests should skip server #1 and go to server #2.
        /// </summary>
        [TestMethod]
        public async Task VerifyRedirectHandler()
        {
            int counter1 = 0;
            int counter2 = 0;

            using var localServer1 = new LocalInProcHttpServer(Localurl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    counter1++;
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", Localurl2);

                    // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.cachecontrolheadervalue?view=net-5.0
                    httpContext.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(1),
                    };

                    await httpContext.Response.WriteAsync("redirect");
                },
            };

            using var localServer2 = new LocalInProcHttpServer(Localurl2)
            {
                ServerLogic = async (httpContext) =>
                {
                    counter2++;
                    await httpContext.Response.WriteAsync(helloString);
                },
            };

            var client = new MyCustomClient(url: Localurl1, new RedirectHttpHandler());

            // Default behavior. 1st server will redirect to 2nd.
            var testStr1 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr1);
            Assert.AreEqual(1, counter1);
            Assert.AreEqual(1, counter2);

            // Redirect is cached. Request will go to 2nd server.
            var testStr2 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr2);
            Assert.AreEqual(1, counter1, "redirect should be cached");
            Assert.AreEqual(2, counter2);
        }

        /// <summary>
        /// Verify behavior of HttpClient without <see cref="RedirectHttpHandler"/>.
        /// Setup two local servers, where server #1 will redirect requests to #2.
        /// After the first request, it is expected that the client will cache the redirect.
        /// After this cache expries, requests will go to server #1.
        /// </summary>
        [TestMethod]
        public async Task VerifyRedirectCache()
        {
            int counter1 = 0;
            int counter2 = 0;

            int cacheSeconds = 5;

            using var localServer1 = new LocalInProcHttpServer(Localurl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    counter1++;
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", Localurl2);

                    // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.cachecontrolheadervalue?view=net-5.0
                    httpContext.Response.GetTypedHeaders().CacheControl =
                    new Microsoft.Net.Http.Headers.CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromSeconds(cacheSeconds),
                    };

                    await httpContext.Response.WriteAsync("redirect");
                },
            };

            using var localServer2 = new LocalInProcHttpServer(Localurl2)
            {
                ServerLogic = async (httpContext) =>
                {
                    counter2++;
                    await httpContext.Response.WriteAsync(helloString);
                },
            };

            var client = new MyCustomClient(url: Localurl1, new RedirectHttpHandler());

            // Default behavior. 1st server will redirect to 2nd.
            var testStr1 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr1);
            Assert.AreEqual(1, counter1);
            Assert.AreEqual(1, counter2);

            // Redirect is cached. Request will go to 2nd server.
            var testStr2 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr2);
            Assert.AreEqual(1, counter1, "redirect should be cached");
            Assert.AreEqual(2, counter2);

            // wait for cache to expire
            await Task.Delay(TimeSpan.FromSeconds(2 * cacheSeconds));

            // Default behavior. 1st server will redirect to 2nd.
            var testStr3 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr3);
            Assert.AreEqual(2, counter1);
            Assert.AreEqual(3, counter2);
        }

        /// <summary>
        /// This class is a wrapper around <see cref="HttpClient"/>.
        /// </summary>
        private class MyCustomClient
        {
            public readonly Uri uri;
            public readonly HttpClient httpClient;

            public MyCustomClient(string url, RedirectHttpHandler httpMessageHandler = null)
            {
                this.uri = new Uri(url);
                this.httpClient = (httpMessageHandler == null)
                    ? new HttpClient()
                    : new HttpClient(httpMessageHandler);
            }

            public async Task<string> GetAsync()
            {
                var result = await this.httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, this.uri));
                return await result.Content.ReadAsStringAsync();
            }
        }
    }
}
#endif