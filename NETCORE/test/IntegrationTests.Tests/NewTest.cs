using IntegrationTests.Tests.TestFramework;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace IntegrationTests.Tests
{
    public partial class NewTest : IClassFixture<NewWebApplicationFactory<WebApp.Startup>>
    {
        private readonly NewWebApplicationFactory<WebApp.Startup> _factory;
        private readonly ITestOutputHelper _output;

        public NewTest(NewWebApplicationFactory<WebApp.Startup> factory, ITestOutputHelper output)
        {
            this._output = output;
            _factory = factory;
            //_factory.sentItems.Clear();
        }

        [Fact]
        public async Task MyNewTest()
        {
            // TODO, IF I COULD REPLACE THE HTTPCLIENT IN TELEMETRYCLIENT, THIS WOULDN'T BE NECESSARY.
            using (var localServer = new LocalInProcHttpServer(_factory.TestHost))
            {
                localServer.ServerLogic = async (ctx) =>
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    await ctx.Response.WriteAsync("Ok");
                };

                await _factory.SendRequestToWebAppAsync();

            }


            //var client = _factory.CreateClient();
            //var path = "Home/Empty";
            //var url = client.BaseAddress + path;

            //// Act
            //var request = CreateRequestMessage();
            //request.RequestUri = new Uri(url);
            //var response = await client.SendAsync(request);

            //// Assert
            //response.EnsureSuccessStatusCode();

            //await WaitForTelemetryToArrive();

            //var items = _factory.sentItems;
            //PrintItems(items);
            //Assert.Equal(1, items.Count);

            //var reqs = GetTelemetryOfType<RequestTelemetry>(items);
            //Assert.Single(reqs);
            //var req = reqs[0];
            //Assert.NotNull(req);
            //ValidateRequest(
            //     requestTelemetry: req,
            //     expectedResponseCode: "200",
            //     expectedName: "GET " + path,
            //     expectedUrl: url,
            //     expectedSuccess: true);
        }
    }
}
