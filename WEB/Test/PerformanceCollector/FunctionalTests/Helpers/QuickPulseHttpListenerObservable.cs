namespace Functional.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Functional.Serialization;
    using FunctionalTests.Helpers;

    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;
    using Microsoft.ApplicationInsights.Extensibility.Filtering;

    internal class QuickPulseHttpListenerObservable : IObservable<MonitoringDataPoint>, IDisposable
    {
        private readonly HttpListener listener;
        private IObservable<MonitoringDataPoint> stream;
        
        public QuickPulseHttpListenerObservable(string url)
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
        }

        public bool FailureDetected { get; set; }

        public void Start()
        {
            this.FailureDetected = false;
            
            if (this.stream != null)
            {
                this.Stop();   
            }

            if (!this.listener.IsListening)
            {
                this.listener.Start();
            }

            this.stream = this.CreateStream();
        }

        public void Stop()
        {
            this.Dispose();
        }

        public IDisposable Subscribe(IObserver<MonitoringDataPoint> observer)
        {
            if (this.stream == null)
            {
                throw new InvalidOperationException("Call QuickPulseHttpListenerObservable.Start before subscribing to the stream");
            }

            return this.stream.Subscribe(observer);
        }

        public void Dispose()
        {
            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener.Close();
                this.stream = null;
            }
        }

        private IObservable<MonitoringDataPoint> CreateStream()
        {
            return Observable
                .Create<MonitoringDataPoint>
                (obs =>
                    Task.Factory.FromAsync(
                        (a, c) => this.listener.BeginGetContext(a, c),
                        ar => this.listener.EndGetContext(ar),
                        null)
                        .ToObservable()
                        .SelectMany(this.CreateSampleFromContext)
                        .Subscribe(obs)
                )
              .Repeat()
              .Publish()
              .RefCount();
        }

        private IEnumerable<MonitoringDataPoint> CreateSampleFromContext(HttpListenerContext context)
        {
            try
            {
                var request = context.Request;
                var content = request.GetContent();
                var requestData = new MemoryStream(Encoding.UTF8.GetBytes(content ?? string.Empty));
                string requestLocalPath = request.Url.LocalPath;

                Trace.WriteLine("=>");
                Trace.WriteLine("Item received (QPS - " + DateTimeOffset.Now + "): " + content);
                Trace.WriteLine("<=");

                var response = context.Response;

                if (requestLocalPath == "/QuickPulseService.svc/ping")
                {
                    response.AddHeader("x-ms-qps-subscribed", true.ToString());
                    response.AddHeader("x-ms-qps-configuration-etag", "Etag1");
                    this.WriteCollectionConfiguration(response.OutputStream);
                }
                else if (requestLocalPath == "/QuickPulseService.svc/post")
                {
                    var dataPoints = TelemetryItemFactory.CreateQuickPulseSamples(requestData);

                    response.AddHeader("x-ms-qps-subscribed", true.ToString());
                    response.AddHeader("x-ms-qps-configuration-etag", "Etag1");
                    this.WriteCollectionConfiguration(response.OutputStream);

                    return dataPoints;
                }
            }
            catch (HttpListenerException)
            {
                // client disconnected
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch (HttpListenerException)
                {
                    // client disconnected
                }
            }

            return new MonitoringDataPoint[0];
        }

        private void WriteCollectionConfiguration(Stream stream)
        {
            var collectionConfigurationInfo = new CollectionConfigurationInfo()
            {
                ETag = "Etag1",
                Metrics =
                    new[]
                    {
                        new CalculatedMetricInfo()
                        {
                            Id = "Metric1",
                            TelemetryType = TelemetryType.Request,
                            Aggregation = AggregationType.Sum,
                            Projection = "Count()",
                            FilterGroups =
                                new[]
                                {
                                    new FilterConjunctionGroupInfo()
                                    {
                                        Filters =
                                            new[] { new FilterInfo() { FieldName = "Success", Predicate = Predicate.Equal, Comparand = "True" } }
                                    }
                                }
                        }
                    },
                DocumentStreams =
                    new[]
                    {
                        new DocumentStreamInfo()
                        {
                            Id = "Stream1",
                            DocumentFilterGroups =
                                new[]
                                {
                                    new DocumentFilterConjunctionGroupInfo()
                                    {
                                        TelemetryType = TelemetryType.Request,
                                        Filters = new FilterConjunctionGroupInfo() { Filters = new FilterInfo[0] }
                                    },
                                    new DocumentFilterConjunctionGroupInfo()
                                    {
                                        TelemetryType = TelemetryType.Dependency,
                                        Filters = new FilterConjunctionGroupInfo() { Filters = new FilterInfo[0] }
                                    },
                                    new DocumentFilterConjunctionGroupInfo()
                                    {
                                        TelemetryType = TelemetryType.Exception,
                                        Filters = new FilterConjunctionGroupInfo() { Filters = new FilterInfo[0] }
                                    },
                                    new DocumentFilterConjunctionGroupInfo()
                                    {
                                        TelemetryType = TelemetryType.Event,
                                        Filters = new FilterConjunctionGroupInfo() { Filters = new FilterInfo[0] }
                                    },
                                    new DocumentFilterConjunctionGroupInfo()
                                    {
                                        TelemetryType = TelemetryType.Trace,
                                        Filters = new FilterConjunctionGroupInfo() { Filters = new FilterInfo[0] }
                                    }
                                }
                        }
                    }
            };

            var serializerCollectionConfigurationInfo = new DataContractJsonSerializer(typeof(CollectionConfigurationInfo));
            serializerCollectionConfigurationInfo.WriteObject(stream, collectionConfigurationInfo);
        }
    }
}
