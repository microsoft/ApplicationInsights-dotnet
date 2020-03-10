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
    using System.Threading.Tasks;

    [TestClass]
    public class ServerTelemetryChannelE2ETests
    {
        private const string Localurl = "http://localhost:6090";
        private const string LocalurlNotRunning = "http://localhost:6091";
        private const long AllKeywords = -1;
        private const int SleepInMilliseconds = 10000;
        private const int DelayfromWebServerInMilliseconds = 6000;
        private const int CancellationTimeOutInMilliseconds = 5000;

        [TestMethod]
        [Ignore("Ignored as unstable in Test/Build machines. Run locally when making changes to ServerChannel")]
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
        [Ignore("Ignored as unstable in Test/Build machines. Run locally when making changes to ServerChannel")]
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
                        (EventKeywords)AllKeywords);

                    // ACT
                    var telemetry = new EventTelemetry("test event name");
                    telemetry.Context.InstrumentationKey = "dummy";
                    channel.Send(telemetry);
                    Thread.Sleep(SleepInMilliseconds);

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
        [Ignore("Ignored as unstable in Test/Build machines. Run locally when making changes to ServerChannel")]
        public void ChannelLogsFailedTransmissionDueToServerError()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                localServer.ServerLogic = async (ctx) =>
                {
                    // Error from AI Backend.
                    ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
                    Thread.Sleep(SleepInMilliseconds);

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
        [Ignore("Ignored as unstable in Test/Build machines. Run locally when making changes to ServerChannel")]
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
                    Thread.Sleep(SleepInMilliseconds);

                    // Assert:
                    var allTraces = listener.Messages.ToList();
                    var traces = allTraces.Where(item => item.EventId == 54).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.IsTrue(traces[0].Payload[1].ToString().Contains("An error occurred while sending the request"));
                }
            }
        }

        [TestMethod]
        [Ignore("Ignored as unstable in Test/Build machines. Run locally when making changes to ServerChannel")]
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
                            (EventKeywords)AllKeywords);


                        // ACT
                        var telemetry = new EventTelemetry("test event name");
                        telemetry.Context.InstrumentationKey = "dummy";
                        channel.Send(telemetry);
                        Thread.Sleep(SleepInMilliseconds);
                    }

                    // Assert:
                    var allTraces = listener.Messages.ToList();
                    var traces = allTraces.Where(item => item.EventId == 70).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.IsTrue(traces[0].Payload[1].ToString().Contains(expectedResponseContents));
                }
            }
        }

        [TestMethod]
        public async Task ChannelSendsTransmissionOnFlushAsync()
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
                var flushTask = channel.FlushAsync(default(CancellationToken));
                try
                {
                    await flushTask;
                }
                catch
                {
                }
            }
        }

        [TestMethod]
        public async Task ChannelLogsSuccessfulTransmissionOnFlushAsync()
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // VERIFY
                    // We validate by checking SDK traces.
                    var allTraces = listener.Messages.ToList();
                    // Event 22 is logged upon successful transmission.
                    var traces = allTraces.Where(item => item.EventId == 22).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.IsTrue(flushTask.Result);
                }
            }
        }

        [TestMethod]
        public async Task ChannelTransmissionSuccessDueToServerErrorOnFlushAsync()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                localServer.ServerLogic = async (ctx) =>
                {
                    // Error from AI Backend.
                    ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    await ctx.Response.WriteAsync("InternalError");
                };

                var channel = new ServerTelemetryChannel
                {
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // VERIFY
                    // We validate by checking SDK traces
                    var allTraces = listener.Messages.ToList();

                    // Event 54 is logged upon transmission failure.
                    var traces = allTraces.Where(item => item.EventId == 54).ToList();
                    Assert.IsTrue(traces.Count > 0);
                    // 500 is the response code.
                    Assert.AreEqual("500", traces[0].Payload[1]);
                    // Returns success, telemetry items are in storage as transmission. Control has transferred out of process. 
                    Assert.IsTrue(flushTask.Result);
                }
            }
        }

        [TestMethod]
        public async Task ChannelTransmissionSuccessDueToUnknownNetworkErrorOnFlushAsync()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                localServer.ServerLogic = (ctx) =>
                {
                    // This code does not matter as Channel is configured to 
                    // with an incorrect endpoint.
                    return null;
                };

                var channel = new ServerTelemetryChannel
                {
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // Assert:
                    var allTraces = listener.Messages.ToList();
                    // Event 54 is logged upon transmission failure.
                    var traces = allTraces.Where(item => item.EventId == 54).ToList();
                    Assert.IsTrue(traces.Count > 0);
                    Assert.AreEqual("500", traces[0].Payload[1].ToString());
                    // Returns success, telemetry items are in storage as transmission. Control has transferred out of process. 
                    Assert.IsTrue(flushTask.Result);
                }
            }
        }

        [TestMethod]
        public async Task ChannelDropsTransmissionDueToResponseCodeTooManyRequestsOverExtendedTimeOnFlushAsync()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                localServer.ServerLogic = async (ctx) =>
                {
                    // Error from AI Backend.
                    ctx.Response.StatusCode = (int)ResponseStatusCodes.ResponseCodeTooManyRequestsOverExtendedTime;
                    await ctx.Response.WriteAsync("ResponseCodeTooManyRequestsOverExtendedTime");
                };

                var channel = new ServerTelemetryChannel
                {
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // Assert:
                    var allTraces = listener.Messages.ToList();
                    // Event 54 is logged upon transmission failure.
                    var traces = allTraces.Where(item => item.EventId == 54).ToList();
                    Assert.AreEqual(1, traces.Count);
                    // 439 is the response code.
                    Assert.AreEqual("439", traces[0].Payload[1]);
                    Assert.IsFalse(flushTask.Result);
                }
            }
        }

        [TestMethod]
        public async Task ChannelLogsResponseBodyFromTransmissionWhenVerboseEnabledOnFlushAsync()
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
                            (EventKeywords)AllKeywords);


                        // ACT
                        var telemetry = new EventTelemetry("test event name");
                        telemetry.Context.InstrumentationKey = "dummy";
                        channel.Send(telemetry);
                        var flushTask = channel.FlushAsync(default(CancellationToken));
                        try
                        {
                            await flushTask;
                        }
                        catch
                        {
                        }
                        Thread.Sleep(SleepInMilliseconds);
                        Assert.IsTrue(flushTask.Result);
                    }

                    // Assert:
                    var allTraces = listener.Messages.ToList();
                    // Event 70 is logged upon raw response content from backend.
                    var traces = allTraces.Where(item => item.EventId == 70).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.IsTrue(traces[0].Payload[1].ToString().Contains(expectedResponseContents));
                }
            }
        }

        [TestMethod]
        public async Task ChannelSetsCancellationDueToCancellationTokenTimeOutOnFlushAsync()
        {
            using (var localServer = new LocalInProcHttpServer(Localurl))
            {
                localServer.ServerLogic = async (ctx) =>
                {
                    // Delay response from AI Backend.
                    await Task.Delay(DelayfromWebServerInMilliseconds);
                };

                var channel = new ServerTelemetryChannel
                {
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
                    var cancellationTokenSource = new CancellationTokenSource();
                    cancellationTokenSource.CancelAfter(CancellationTimeOutInMilliseconds);

                    var flushTask = channel.FlushAsync(cancellationTokenSource.Token);
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }

                    Assert.IsTrue(flushTask.IsCanceled);
                }
            }
        }

        [TestMethod]
        public async Task ChannelTransmissionMovedToStorageDueToMaxTransmissionSenderCapacityZeroOnFlushAsync()
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
                    EndpointAddress = Localurl,
                    MaxTransmissionSenderCapacity = 0
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // VERIFY
                    // We validate by checking SDK traces.
                    var allTraces = listener.Messages.ToList();
                    // Event 52 is logged when once items are moved from Buffer to Storage.
                    var traces = allTraces.Where(item => item.EventId == 52).ToList();
                    Assert.AreEqual(1, traces.Count);
                    // All items are moved to storage
                    Assert.IsTrue(flushTask.Result);
                }
            }
        } 

        [TestMethod]
        public async Task ChannelDropsTransmissionDueToMaxTransmissionStorageCapacityZeroOnFlushAsync()
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
                    EndpointAddress = Localurl,
                    MaxTransmissionSenderCapacity = 0,
                    MaxTransmissionStorageCapacity = 0                    
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // VERIFY
                    // We validate by checking SDK traces.
                    var allTraces = listener.Messages.ToList();
                    // Event 25 is logged when storage enqueue has no capacity.
                    var traces = allTraces.Where(item => item.EventId == 25).ToList();
                    Assert.AreEqual(1, traces.Count);
                    // Returns failure as telemetry items did not store either in webserver or storage, failure is within the process. 
                    Assert.IsFalse(flushTask.Result);
                }
            }
        }

        [TestMethod]
        public async Task ChannelTransmissionMovedtoStorageDueToMaxTransmissionBufferCapacityZeroOnFlushAsync()
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
                    EndpointAddress = Localurl,
                    MaxTransmissionSenderCapacity = 0,
                    MaxTransmissionBufferCapacity = 0
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // VERIFY
                    // We validate by checking SDK traces.
                    var allTraces = listener.Messages.ToList();
                    // Event 26 is logged when items are moved to storage.
                    var traces = allTraces.Where(item => item.EventId == 26).ToList();
                    Assert.AreEqual(1, traces.Count);
                    Assert.IsTrue(flushTask.Result);
                }
            }
        }

        [TestMethod]
        public async Task ChannelDropsTransmissionDueToAllCapacityZeroOnFlushAsync()
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
                    EndpointAddress = Localurl,
                    MaxTransmissionSenderCapacity = 0,
                    MaxTransmissionBufferCapacity = 0,
                    MaxTransmissionStorageCapacity = 0
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
                    var flushTask = channel.FlushAsync(default(CancellationToken));
                    try
                    {
                        await flushTask;
                    }
                    catch
                    {
                    }
                    Thread.Sleep(SleepInMilliseconds);

                    // VERIFY
                    // We validate by checking SDK traces.
                    var allTraces = listener.Messages.ToList();
                    // Event 42 is logged when items are not stored in storage.
                    var traces = allTraces.Where(item => item.EventId == 42).ToList();
                    Assert.AreEqual(1, traces.Count);
                    // We lose telemetry.
                    Assert.IsFalse(flushTask.Result);
                }
            }
        }

    }
}
#endif