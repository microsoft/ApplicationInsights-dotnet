// --------------------------------------------------------------------------------------------------------------------
// <copyright file="QuickPulseServiceClient.cs" company="Microsoft">
//   Copyright © Microsoft. All Rights Reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using System.Web;

    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;

    /// <summary>
    /// Service client for QPS service.
    /// </summary>
    internal sealed class QuickPulseServiceClient : IQuickPulseServiceClient
    {
        private const string XMsQpsSubscribedHeaderName = "x-ms-qps-subscribed";

        private const int RetryCount = 3;

        private readonly Uri serviceUri;

        private readonly string instanceName;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(3);

        public QuickPulseServiceClient(Uri serviceUri, string instanceName, TimeSpan? timeout = null)
        {
            this.serviceUri = serviceUri;
            this.instanceName = instanceName;
            this.timeout = timeout ?? this.timeout;
        }

        public bool? Ping(string instrumentationKey)
        {
            var path = string.Format(CultureInfo.InvariantCulture, "ping?ikey={0}", instrumentationKey);
            HttpWebResponse response = this.SendRequest(WebRequestMethods.Http.Head, path, null, true);
            if (response == null)
            {
                return null;
            }
            
            return ProcessResponse(response);
        }

        public bool? SubmitSamples(IEnumerable<QuickPulseDataSample> samples, string instrumentationKey)
        {
            //!!!
            // //!!!System.IO.File.AppendAllText(@"e:\qps.log", $"Sample count: {samples.Count()}{Environment.NewLine}\tAI RPS: {samples.First().AIRequestsPerSecond}\tIIS RPS: {samples.First().PerfIisRequestsPerSecond}\tAI Duration: {TimeSpan.FromTicks((long)samples.First().AIRequestDurationAveInTicks).TotalMilliseconds} ms{Environment.NewLine}");
            var bodyStream = new MemoryStream();

            WriteSamples(samples, bodyStream, instrumentationKey, this.instanceName);
            bodyStream.Position = 0;

            var path = string.Format(CultureInfo.InvariantCulture, "post?ikey={0}", HttpUtility.UrlEncode(instrumentationKey));
            HttpWebResponse response = this.SendRequest(WebRequestMethods.Http.Post, path, bodyStream, false);
            if (response == null)
            {
                return null;
            }

            return ProcessResponse(response);
        }

        private static void WriteSamples(IEnumerable<QuickPulseDataSample> samples, MemoryStream bodyStream, string instrumentationKey, string instanceName)
        {
            var monitoringPoints = new List<MonitoringDataPoint>();

            foreach (var sample in samples)
            {
                var metricPoints = new List<MetricPoint>
                                       {
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Requests/Sec",
                                                   Value = sample.AIRequestsPerSecond,
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Request Duration",
                                                   Value = sample.AIRequestDurationAveInTicks,
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Requests Failed/Sec",
                                                   Value = sample.AIRequestsFailedPerSecond,
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Requests Succeeded/Sec",
                                                   Value = sample.AIRequestsSucceededPerSecond,
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Calls/Sec",
                                                   Value = sample.AIDependencyCallsPerSecond,
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Call Duration",
                                                   Value = sample.AIDependencyCallDurationAveInTicks,
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Calls Failed/Sec",
                                                   Value = sample.AIDependencyCallsFailedPerSecond,
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Calls Succeeded/Sec",
                                                   Value = sample.AIDependencyCallsSucceededPerSecond,
                                                   Weight = 1
                                               }
                                       };

                metricPoints.AddRange(sample.PerfCountersLookup.Select(counter => new MetricPoint { Name = counter.Key, Value = counter.Value, Weight = 1 }));

                var dataPoint = new MonitoringDataPoint
                                    {
                                        InstrumentationKey = instrumentationKey,
                                        Instance = instanceName,
                                        Timestamp = sample.EndTimestamp,
                                        Metrics = metricPoints.ToArray()
                                    };

                monitoringPoints.Add(dataPoint);
            }

            var serializer = new DataContractJsonSerializer(typeof(MonitoringDataPoint[]));
            serializer.WriteObject(bodyStream, monitoringPoints.ToArray());
        }

        private static bool? ProcessResponse(HttpWebResponse response)
        {
            bool isSubscribed;
            if (!bool.TryParse(response.GetResponseHeader(XMsQpsSubscribedHeaderName), out isSubscribed))
            {
                return null;
            }

            return isSubscribed;
        }

        private HttpWebResponse SendRequest(string httpVerb, string path, MemoryStream body, bool enableRetry)
        {
            var requestUri = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", this.serviceUri.AbsoluteUri.TrimEnd('/'), path.TrimStart('/'));

            int attemptMaxCount = enableRetry ? RetryCount : 1;
            int attempt = 0;
            while (attempt < attemptMaxCount)
            {
                try
                {
                    var request = WebRequest.Create(requestUri) as HttpWebRequest;
                    request.Method = httpVerb;
                    request.Timeout = (int)this.timeout.TotalMilliseconds;

                    if (body != null)
                    {
                        var requestStream = request.GetRequestStream();
                        body.CopyTo(requestStream);
                    }

                    var response = request.GetResponse() as HttpWebResponse;
                    if (response != null)
                    {
                        return response;
                    }
                }
                catch (Exception e)
                {
                    QuickPulseEventSource.Log.ServiceCommunicationFailedEvent(e.ToString());
                }

                attempt++;
            }

            return null;
        }
    }
}