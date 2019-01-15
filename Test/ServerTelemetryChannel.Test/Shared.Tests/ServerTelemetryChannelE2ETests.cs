#if NETCOREAPP2_0
namespace Microsoft.ApplicationInsights.WindowsServer.Channel
{    
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;    
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;        
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;    
    using Microsoft.ApplicationInsights.WindowsServer.Channel.Helpers;
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation;    
    using Microsoft.AspNetCore.Http;
    using Microsoft.VisualStudio.TestTools.UnitTesting;


    [TestClass]      
    public class ServerTelemetryChannelE2ETests
    {
        private const string Localurl = "http://localhost:6090";
        private const string LocalurlNotRunning = "http://localhost:6091";
        private const long AllKeywords = -1;

        [TestMethod]
        public void ChannelSendsTransmission()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                IList<ITelemetry> telemetryItems = new List<ITelemetry>();
                var telemetry = new EventTelemetry("test event name");
                telemetry.Context.InstrumentationKey = "dummy";
                telemetryItems.Add((telemetry));
                var serializedExpected = JsonSerializer.Serialize(telemetryItems);

                localServer.ServerLogic = async (ctx) =>
                {
                    byte[] buffer = new byte[2000];
                    await ctx.Request.Body.ReadAsync(buffer, 0, 2000);     
                    Assert.AreEqual(serializedExpected, buffer);
                    await ctx.Response.WriteAsync("Ok");
                };

                var channel = new ServerTelemetryChannel
                {
                    DeveloperMode = true,
                    EndpointAddress = Localurl
                };

                var config = new TelemetryConfiguration("dummy")
                {
                    TelemetryChannel = channel
                };
                channel.Initialize(config);

                // ACT 
                // Data would be sent to the LocalServer which validates it.
                channel.Send(telemetry);                    
            }
        }

        [TestMethod]
        public void ChannelLogsSuccessfulTransmission()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {                
                localServer.ServerLogic = async (ctx) =>
                {
                    // Success from AI Backend.
                    await ctx.Response.WriteAsync("Ok");
                };

                var channel = new ServerTelemetryChannel
                {
                    DeveloperMode = true,
                    EndpointAddress = Localurl
                };
                var config = new TelemetryConfiguration("dummy")
                {
                    TelemetryChannel = channel
                };
                channel.Initialize(config);

                using (var listener = new TestEventListener())
                {
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways,
                        (EventKeywords) AllKeywords);

                    // ACT
                    var telemetry = new EventTelemetry("test event name");
                    telemetry.Context.InstrumentationKey = "dummy";
                    channel.Send(telemetry);
                    Thread.Sleep(1000);

                    // VERIFY
                    // We validate by checking SDK traces.
                    var allTraces = listener.Messages.ToList();
                    // Event 22 is logged upon successful transmission.
                    var traces = allTraces.Where(item => item.EventId == 22).ToList();
                    Assert.AreEqual(1, traces.Count);                    
                }
            }            
        }

        [TestMethod]
        public void ChannelLogsFailedTransmissionDueToServerError()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {                
                localServer.ServerLogic = async (ctx) =>
                {
                    // Error from AI Backend.
                    ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                    await ctx.Response.WriteAsync("InternalError");
                };

                var channel = new ServerTelemetryChannel
                {
                    DeveloperMode = true,
                    EndpointAddress = Localurl
                };
                var config = new TelemetryConfiguration("dummy")
                {
                    TelemetryChannel = channel
                };
                channel.Initialize(config);

                using (var listener = new TestEventListener())
                {
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways,
                        (EventKeywords)AllKeywords);

                    // ACT
                    var telemetry = new EventTelemetry("test event name");
                    telemetry.Context.InstrumentationKey = "dummy";
                    channel.Send(telemetry);
                    Thread.Sleep(1000);

                    // VERIFY
                    // We validate by checking SDK traces
                    var allTraces = listener.Messages.ToList();
                    
                    // Event 54 is logged upon successful transmission.
                    var traces = allTraces.Where(item => item.EventId == 54).ToList();
                    Assert.AreEqual(1, traces.Count);
                    // 500 is the response code.
                    Assert.AreEqual("500", traces[0].Payload[1]);
                }
            }
        }

        [TestMethod]
        public void ChannelHandlesFailedTransmissionDueToUnknownNetworkError()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                localServer.ServerLogic = async (ctx) =>
                {
                    // This code does not matter as Channel is configured to 
                   // with an incorrect endpoint.
                    await ctx.Response.WriteAsync("InternalError");
                };

                var channel = new ServerTelemetryChannel
                {
                    DeveloperMode = true,
                    EndpointAddress = LocalurlNotRunning
                };
                var config = new TelemetryConfiguration("dummy")
                {
                    TelemetryChannel = channel
                };
                channel.Initialize(config);

                using (var listener = new TestEventListener())
                {
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways,
                        (EventKeywords)AllKeywords);

                    // ACT
                    var telemetry = new EventTelemetry("test event name");
                    telemetry.Context.InstrumentationKey = "dummy";
                    channel.Send(telemetry);
                    Thread.Sleep(5000);

                    // Assert:
                    var allTraces = listener.Messages.ToList();
                    var traces = allTraces.Where(item => item.EventId == 54).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.IsTrue(traces[0].Payload[1].ToString().Contains("An error occurred while sending the request"));
                }
            }
        }

        [TestMethod]
        public void ChannelLogsResponseBodyFromTransmissionWhenVerboseEnabled()
        {
            var expectedResponseContents = "this is the expected response";

            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                localServer.ServerLogic = async (ctx) =>
                {                                        
                    await ctx.Response.WriteAsync(expectedResponseContents);
                };

                var channel = new ServerTelemetryChannel
                {
                    DeveloperMode = true,
                    EndpointAddress = Localurl
                };
                var config = new TelemetryConfiguration("dummy")
                {
                    TelemetryChannel = channel
                };
                channel.Initialize(config);

                using (var listener = new TestEventListener())
                {
                    listener.EnableEvents(TelemetryChannelEventSource.Log, EventLevel.LogAlways,
                        (EventKeywords)AllKeywords);

                    // Enable CoreEventSource as Transmission logic is in base sdk.
                    // and it'll parse response only on Verbose enabled.
                    using (var listenerCore = new TestEventListener())
                    {
                        listener.EnableEvents(CoreEventSource.Log, EventLevel.LogAlways,
                            (EventKeywords) AllKeywords);


                        // ACT
                        var telemetry = new EventTelemetry("test event name");
                        telemetry.Context.InstrumentationKey = "dummy";
                        channel.Send(telemetry);
                        Thread.Sleep(1000);
                    }

                    // Assert:
                    var allTraces = listener.Messages.ToList();
                    var traces = allTraces.Where(item => item.EventId == 70).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.IsTrue(traces[0].Payload[1].ToString().Contains(expectedResponseContents));
                }
            }
        }
    }
}
#endif