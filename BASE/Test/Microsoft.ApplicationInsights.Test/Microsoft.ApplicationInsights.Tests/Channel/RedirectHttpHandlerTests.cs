#if NETCOREAPP
namespace Microsoft.ApplicationInsights.TestFramework.Channel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
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

    using Microsoft.Net.Http.Headers;

    [TestClass]
    [TestCategory("WindowsOnly")] // The LocalInProcHttpServer does not perform well on Linux.
    public class RedirectHttpHandlerTests
    {
        private const string helloString = "Hello World!";

        private TimeSpan testCache = TimeSpan.FromSeconds(2);

        private const string LocalUrl1 = "http://localhost:1111";
        private const string LocalUrl2 = "http://localhost:2222";

        /// <summary>
        /// Verify behavior of HttpClient without <see cref="RedirectHttpHandler"/>.
        /// Setup two local servers, where server #1 will redirect requests to #2.
        /// </summary>
        [TestMethod]
        public async Task DefaultUseCase()
        {
            using var localServer1 = new LocalInProcHttpServer(LocalUrl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", LocalUrl2);
                    await httpContext.Response.WriteAsync("redirect");
                },
            };

            using var localServer2 = new LocalInProcHttpServer(LocalUrl2)
            {
                ServerLogic = async (httpContext) =>
                {
                    await httpContext.Response.WriteAsync(helloString);
                },
            };

            var client = new MyCustomClient(url: LocalUrl1);

            // Default behavior. 1st server will redirect to 2nd.
            var testStr1 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr1);
            Assert.AreEqual(1, localServer1.RequestCounter);
            Assert.AreEqual(1, localServer2.RequestCounter);

            // Default behavior. Nothing is cached, repeat previous workflow.
            var testStr2 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr2);
            Assert.AreEqual(2, localServer1.RequestCounter);
            Assert.AreEqual(2, localServer2.RequestCounter);
        }

        /// <summary>
        /// Verify behavior of HttpClient and <see cref="RedirectHttpHandler"/>.
        /// Setup two local servers, where server #1 will redirect requests to #2.
        /// After the first request, it is expected that the client will cache the redirect.
        /// Additional requests should skip server #1 and go to server #2.
        /// </summary>
        [TestMethod]
        public async Task VerifyRedirect()
        {
            using var localServer1 = new LocalInProcHttpServer(LocalUrl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", LocalUrl2);

                    // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.cachecontrolheadervalue?view=net-5.0
                    httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(1),
                    };

                    await httpContext.Response.WriteAsync("redirect");
                },
            };

            using var localServer2 = new LocalInProcHttpServer(LocalUrl2)
            {
                ServerLogic = async (httpContext) =>
                {
                    await httpContext.Response.WriteAsync(helloString);
                },
            };

            var client = new MyCustomClient(url: LocalUrl1, new RedirectHttpHandler());

            // Default behavior. 1st server will redirect to 2nd.
            var testStr1 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr1);
            Assert.AreEqual(1, localServer1.RequestCounter);
            Assert.AreEqual(1, localServer2.RequestCounter);

            // Redirect is cached. Request will go to 2nd server.
            var testStr2 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr2);
            Assert.AreEqual(1, localServer1.RequestCounter, "redirect should be cached");
            Assert.AreEqual(2, localServer2.RequestCounter);
        }

        /// <summary>
        /// Verify behavior of HttpClient and <see cref="RedirectHttpHandler"/>.
        /// Setup two local servers, where server #1 will redirect requests to #2.
        /// After the first request, it is expected that the client will cache the redirect.
        /// Additional requests should skip server #1 and go to server #2.
        /// </summary>
        [TestMethod]
        public async Task StressTest()
        {
            using var localServer1 = new LocalInProcHttpServer(LocalUrl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", LocalUrl2);

                    // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.cachecontrolheadervalue?view=net-5.0
                    httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(1),
                    };

                    await httpContext.Response.WriteAsync("redirect1");
                },
            };

            using var localServer2 = new LocalInProcHttpServer(LocalUrl2)
            {
                ServerLogic = async (httpContext) =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", LocalUrl1);

                    // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.cachecontrolheadervalue?view=net-5.0
                    httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(1),
                    };

                    await httpContext.Response.WriteAsync("redirect2");
                },
            };

            var client = new MyCustomClient(url: LocalUrl1, new RedirectHttpHandler());

            // TODO: TRYING TO CAUSE DEADLOCKS
            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                tasks.Add(client.GetAsync());
            }

            Task.WaitAll(tasks.ToArray());

            Assert.IsTrue(localServer1.RequestCounter > 1);
            Assert.IsTrue(localServer2.RequestCounter > 1);
        }

        /// <summary>
        /// Verify behavior of HttpClient with <see cref="RedirectHttpHandler"/>.
        /// Setup two local servers, where server #1 will redirect requests to #2.
        /// After the first request, it is expected that the client will cache the redirect.
        /// After this cache expries, requests will go to server #1.
        /// </summary>
        [TestMethod]
        public async Task VerifyRedirectCache()
        {
            using var localServer1 = new LocalInProcHttpServer(LocalUrl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", LocalUrl2);

                    // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.cachecontrolheadervalue?view=net-5.0
                    httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = testCache,
                    };

                    await httpContext.Response.WriteAsync("redirect");
                },
            };

            using var localServer2 = new LocalInProcHttpServer(LocalUrl2)
            {
                ServerLogic = async (httpContext) =>
                {
                    await httpContext.Response.WriteAsync(helloString);
                },
            };

            var client = new MyCustomClient(url: LocalUrl1, new RedirectHttpHandler());

            // Default behavior. 1st server will redirect to 2nd.
            var testStr1 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr1);
            Assert.AreEqual(1, localServer1.RequestCounter);
            Assert.AreEqual(1, localServer2.RequestCounter);

            // Redirect is cached. Request will go to 2nd server.
            var testStr2 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr2);
            Assert.AreEqual(1, localServer1.RequestCounter, "redirect should be cached");
            Assert.AreEqual(2, localServer2.RequestCounter);

            // wait for cache to expire
            await Task.Delay(testCache * 2);

            // Default behavior. 1st server will redirect to 2nd.
            var testStr3 = await client.GetAsync();
            Assert.AreEqual(helloString, testStr3);
            Assert.AreEqual(2, localServer1.RequestCounter);
            Assert.AreEqual(3, localServer2.RequestCounter);
        }

        /// <summary>
        /// Verify behavior of HttpClient with <see cref="RedirectHttpHandler"/>.
        /// In this test, server1 will redirect to itself.
        /// Verify that <see cref="RedirectHttpHandler.MaxRedirect"/> is enforced.
        /// </summary>
        [TestMethod]
        public async Task VerifyMaxRedirects()
        {
            using var localServer1 = new LocalInProcHttpServer(LocalUrl1)
            {
                ServerLogic = async (httpContext) =>
                {
                    httpContext.Response.StatusCode = StatusCodes.Status308PermanentRedirect;//.Status307TemporaryRedirect;
                    httpContext.Response.Headers.Add("Location", LocalUrl1);

                    // https://docs.microsoft.com/en-us/aspnet/core/performance/caching/middleware?view=aspnetcore-5.0
                    // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.cachecontrolheadervalue?view=net-5.0
                    httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = TimeSpan.FromDays(1),
                    };

                    await httpContext.Response.WriteAsync("redirect");
                },
            };

            var client = new MyCustomClient(url: LocalUrl1, new RedirectHttpHandler());

            var testStr1 = await client.GetAsync();
            Assert.AreEqual("redirect", testStr1);

            Assert.AreEqual(RedirectHttpHandler.MaxRedirect + 1, localServer1.RequestCounter, $"expecting 1 original request + {RedirectHttpHandler.MaxRedirect} additional requests");
        }

        /// <summary>
        /// This class is a wrapper around <see cref="HttpClient"/>.
        /// I'm using this to simplify the tests above.
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