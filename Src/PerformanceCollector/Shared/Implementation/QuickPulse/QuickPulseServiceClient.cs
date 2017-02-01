namespace Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.Serialization.Json;
    using Helpers;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;

    /// <summary>
    /// Service client for QPS service.
    /// </summary>
    internal sealed class QuickPulseServiceClient : IQuickPulseServiceClient
    {
        private readonly string instanceName;

        private readonly string streamId;

        private readonly string machineName;

        private readonly string version;

        private readonly TimeSpan timeout = TimeSpan.FromSeconds(3);

        private readonly Clock timeProvider;

        private readonly bool isWebApp;

        private readonly DataContractJsonSerializer serializerDataPoint = new DataContractJsonSerializer(typeof(MonitoringDataPoint));

        private readonly DataContractJsonSerializer serializerDataPointArray = new DataContractJsonSerializer(typeof(MonitoringDataPoint[]));

        public QuickPulseServiceClient(Uri serviceUri, string instanceName, string streamId, string machineName, string version, Clock timeProvider, bool isWebApp, TimeSpan? timeout = null)
        {
            this.ServiceUri = serviceUri;
            this.instanceName = instanceName;
            this.streamId = streamId;
            this.machineName = machineName;
            this.version = version;
            this.timeProvider = timeProvider;
            this.isWebApp = isWebApp;
            this.timeout = timeout ?? this.timeout;
        }

        public Uri ServiceUri { get; }

        public bool? Ping(string instrumentationKey, DateTimeOffset timestamp)
        {
            var path = string.Format(CultureInfo.InvariantCulture, "ping?ikey={0}", Uri.EscapeUriString(instrumentationKey));
            HttpWebResponse response = this.SendRequest(WebRequestMethods.Http.Post, path, stream => this.WritePingData(timestamp, stream));

            if (response == null)
            {
                return null;
            }

            return ProcessResponse(response);
        }

        public bool? SubmitSamples(IEnumerable<QuickPulseDataSample> samples, string instrumentationKey)
        {
            var path = string.Format(CultureInfo.InvariantCulture, "post?ikey={0}", Uri.EscapeUriString(instrumentationKey));
            HttpWebResponse response = this.SendRequest(
                WebRequestMethods.Http.Post,
                path,
                stream => this.WriteSamples(samples, instrumentationKey, stream));

            if (response == null)
            {
                return null;
            }

            return ProcessResponse(response);
        }

        private static bool? ProcessResponse(HttpWebResponse response)
        {
            bool isSubscribed;
            if (!bool.TryParse(response.GetResponseHeader(QuickPulseConstants.XMsQpsSubscribedHeaderName), out isSubscribed))
            {
                return null;
            }

            return isSubscribed;
        }

        private static double Round(double value)
        {
            return Math.Round(value, 4, MidpointRounding.AwayFromZero);
        }

        private void WritePingData(DateTimeOffset timestamp, Stream stream)
        {
            var dataPoint = new MonitoringDataPoint
            {
                Version = this.version,
                InvariantVersion = MonitoringDataPoint.CurrentInvariantVersion,
                //InstrumentationKey = instrumentationKey, // ikey is currently set in query string parameter
                Instance = this.instanceName,
                StreamId = this.streamId,
                MachineName = this.machineName,
                Timestamp = timestamp.UtcDateTime,
                IsWebApp = this.isWebApp
            };

            this.serializerDataPoint.WriteObject(stream, dataPoint);
        }

        private void WriteSamples(IEnumerable<QuickPulseDataSample> samples, string instrumentationKey, Stream stream)
        {
            var monitoringPoints = new List<MonitoringDataPoint>();

            foreach (var sample in samples)
            {
                var metricPoints = new List<MetricPoint>
                                       {
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Requests/Sec",
                                                   Value = Round(sample.AIRequestsPerSecond),
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Request Duration",
                                                   Value = Round(sample.AIRequestDurationAveInMs),
                                                   Weight = sample.AIRequests
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Requests Failed/Sec",
                                                   Value = Round(sample.AIRequestsFailedPerSecond),
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Requests Succeeded/Sec",
                                                   Value = Round(sample.AIRequestsSucceededPerSecond),
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Calls/Sec",
                                                   Value = Round(sample.AIDependencyCallsPerSecond),
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Call Duration",
                                                   Value = Round(sample.AIDependencyCallDurationAveInMs),
                                                   Weight = sample.AIDependencyCalls
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Calls Failed/Sec",
                                                   Value = Round(sample.AIDependencyCallsFailedPerSecond),
                                                   Weight = 1
                                               },
                                           new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Dependency Calls Succeeded/Sec",
                                                   Value = Round(sample.AIDependencyCallsSucceededPerSecond),
                                                   Weight = 1
                                               },
                                            new MetricPoint
                                               {
                                                   Name = @"\ApplicationInsights\Exceptions/Sec",
                                                   Value = Round(sample.AIExceptionsPerSecond),
                                                   Weight = 1
                                               }
                                       };

                metricPoints.AddRange(sample.PerfCountersLookup.Select(counter => new MetricPoint { Name = counter.Key, Value = Round(counter.Value), Weight = 1 }));

                ITelemetryDocument[] documents = sample.TelemetryDocuments.ToArray();
                Array.Reverse(documents);

                ProcessCpuData[] topCpuProcesses =
                    sample.TopCpuData.Select(p => new ProcessCpuData() { ProcessName = p.Item1, CpuPercentage = p.Item2 }).ToArray();

                var dataPoint = new MonitoringDataPoint
                                    {
                                        Version = this.version,
                                        InvariantVersion = MonitoringDataPoint.CurrentInvariantVersion,
                                        InstrumentationKey = instrumentationKey,
                                        Instance = this.instanceName,
                                        StreamId = this.streamId,
                                        MachineName = this.machineName,
                                        Timestamp = sample.EndTimestamp.UtcDateTime,
                                        IsWebApp = this.isWebApp,
                                        Metrics = metricPoints.ToArray(),
                                        Documents = documents,
                                        TopCpuProcesses = topCpuProcesses.Length > 0 ? topCpuProcesses : null,
                                        TopCpuDataAccessDenied = sample.TopCpuDataAccessDenied
                                    };

                monitoringPoints.Add(dataPoint);
            }

            this.serializerDataPointArray.WriteObject(stream, monitoringPoints.ToArray());
        }

        private HttpWebResponse SendRequest(string httpVerb, string path, Action<Stream> onWriteBody)
        {
            var requestUri = string.Format(CultureInfo.InvariantCulture, "{0}/{1}", this.ServiceUri.AbsoluteUri.TrimEnd('/'), path.TrimStart('/'));

            try
            {
                var request = WebRequest.Create(requestUri) as HttpWebRequest;
                request.Method = httpVerb;
                request.Timeout = (int)this.timeout.TotalMilliseconds;
                request.Headers.Add(QuickPulseConstants.XMsQpsTransmissionTimeHeaderName, this.timeProvider.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));

                onWriteBody?.Invoke(request.GetRequestStream());

                var response = request.GetResponse() as HttpWebResponse;
                if (response != null)
                {
                    return response;
                }
            }
            catch (Exception e)
            {
                QuickPulseEventSource.Log.ServiceCommunicationFailedEvent(e.ToInvariantString());
            }

            return null;
        }
    }
}