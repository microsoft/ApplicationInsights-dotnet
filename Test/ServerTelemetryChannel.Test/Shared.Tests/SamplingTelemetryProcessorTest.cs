namespace Microsoft.ApplicationInsights.WindowsServer.Channel
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Web.TestFramework;    
    using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
    
    [TestClass]
    public class SamplingTelemetryProcessorTest
    {
        [TestMethod]
        public void ThrowsAgrumentNullExceptionWithoutNextPocessor()
        {
            Assert.Throws<ArgumentNullException>(() => new SamplingTelemetryProcessor(null));
        }

        [TestMethod]
        public void DefaultSamplingRateIs100Percent()
        {
            var processor = new SamplingTelemetryProcessor(new StubTelemetryProcessor(null));

            Assert.Equal(processor.SamplingPercentage, 100.0, 12);
        }

        [TestMethod]
        public void AllTelemetryIsSentWithDefaultSamplingRate()
        {
            var sentTelemetry = new List<ITelemetry>();
            var processor = new SamplingTelemetryProcessor(new StubTelemetryProcessor(null) { OnProcess = t => sentTelemetry.Add(t) });

            const int ItemsToGenerate = 100;

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                processor.Process(new RequestTelemetry());
            }

            Assert.Equal(ItemsToGenerate, sentTelemetry.Count);
        }

        [TestMethod]
        public void TelemetryItemHasSamplingPercentageSet()
        {
            var sentTelemetry = new List<ITelemetry>();
            var processor = new SamplingTelemetryProcessor(new StubTelemetryProcessor(null) { OnProcess = t => sentTelemetry.Add(t) })
                {
                    SamplingPercentage = 20
                };

            do
            {
                processor.Process(new RequestTelemetry());
            }
            while (sentTelemetry.Count == 0);

            Assert.Equal(20, ((ISupportSampling)sentTelemetry[0]).SamplingPercentage);
        }

        [TestMethod]
        public void TelemetryItemSamplingIsSkippedWhenSetByUser()
        {
            var sentTelemetry = new List<ITelemetry>();
            var processor = new SamplingTelemetryProcessor(new StubTelemetryProcessor(null) { OnProcess = t => sentTelemetry.Add(t) })
            {
                SamplingPercentage = 0
            };

            var requestTelemetry = new RequestTelemetry();
            ((ISupportSampling)requestTelemetry).SamplingPercentage = 100;
            processor.Process(requestTelemetry);

            Assert.Equal(1, sentTelemetry.Count);
        }

        [TestMethod]
        public void DependencyTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling(telemetryProcessors => telemetryProcessors.Process(new DependencyTelemetry()));
        }
        
        [TestMethod]
        public void EventTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling(telemetryProcessors => telemetryProcessors.Process(new EventTelemetry("event")));
        }
        
        [TestMethod]
        public void ExceptionTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling(telemetryProcessors => telemetryProcessors.Process(new ExceptionTelemetry(new Exception("exception"))));
        }
        
        [TestMethod]
        public void MetricTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new MetricTelemetry("metric", 1.0));
                return 1;
            });
        }
        
        [TestMethod]
        public void PageViewTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling(telemetryProcessors => telemetryProcessors.Process(new PageViewTelemetry("page")));
        }
        
        [TestMethod]
        public void PerformanceCounterTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling(
                telemetryProcessors =>
                {
                    telemetryProcessors.Process(new PerformanceCounterTelemetry("category", "counter", "instance", 1.0));
                    return 1;
                });
        }
        
        [TestMethod]
        public void RequestTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling(telemetryProcessors => telemetryProcessors.Process(new RequestTelemetry()));
        }
        
        [TestMethod]
        public void SessionStateTelemetryIsNotSubjectToSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new SessionStateTelemetry());
                return 1;
            });
        }
        
        [TestMethod]
        public void TraceTelemetryIsSubjectToSampling()
        {
            TelemetryTypeSupportsSampling(telemetryProcessors => telemetryProcessors.Process(new TraceTelemetry("my trace")));
        }

        [TestMethod]
        public void RequestCanBeExcludedFromSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new RequestTelemetry());
                return 1;
            }, "request");
        }

        [TestMethod]
        public void DependencyCanBeExcludedFromSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new DependencyTelemetry());
                return 1;
            }, 
            "dependency");
        }

        [TestMethod]
        public void EventCanBeExcludedFromSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new EventTelemetry());
                return 1;
            }, 
            "event");
        }

        [TestMethod]
        public void ExceptionCanBeExcludedFromSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new ExceptionTelemetry());
                return 1;
            }, 
            "exception");
        }

        [TestMethod]
        public void TraceCanBeExcludedFromSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new TraceTelemetry());
                return 1;
            }, 
            "trace");
        }

        [TestMethod]
        public void PageViewCanBeExcludedFromSampling()
        {
            TelemetryTypeDoesNotSupportSampling(telemetryProcessors =>
            {
                telemetryProcessors.Process(new PageViewTelemetry());
                return 1;
            }, 
            "pageview");
        }

        [TestMethod]
        public void MultipleItemsCanBeExcludedFromSampling()
        {
            TelemetryTypeDoesNotSupportSampling(
                telemetryProcessors =>
                {
                    telemetryProcessors.Process(new PageViewTelemetry());
                    telemetryProcessors.Process(new RequestTelemetry());
                    return 2;
                }, 
                "pageview;request");
        }

        [TestMethod]
        public void UnknownExcludedTypesAreIgnored()
        {
            TelemetryTypeSupportsSampling(telemetryProcessors => telemetryProcessors.Process(new TraceTelemetry("my trace")), "lala1;lala2,lala3");
        }

        [TestMethod]
        public void IncorrectFormatDoesNotAffectCorrectExcludedTypes()
        {
            TelemetryTypeDoesNotSupportSampling(
                telemetryProcessors =>
                {
                    telemetryProcessors.Process(new PageViewTelemetry());
                    telemetryProcessors.Process(new RequestTelemetry());
                    return 2;
                },
                ";;;;;lala1;;;;;pageview;lala2;request;;;;;");
        }

        private static void TelemetryTypeDoesNotSupportSampling(Func<TelemetryProcessorChain, int> sendAction, string excludedTypes = null)
        {
            const int SamplingPercentage = 10;
            var sentTelemetry = new List<ITelemetry>();
            var telemetryProcessorChainWithSampling = CreateTelemetryProcessorChainWithSampling(sentTelemetry, SamplingPercentage, excludedTypes);

            int generatedCount = 0;
            for (int i = 0; i < 100; i++)
            {
                generatedCount += sendAction.Invoke(telemetryProcessorChainWithSampling);
            }

            Assert.Equal(generatedCount, sentTelemetry.Count);
        }

        private static void TelemetryTypeSupportsSampling(Action<TelemetryProcessorChain> sendAction, string excludedTypes = null)
        {
            const int ItemsToGenerate = 100;
            const int SamplingPercentage = 10;
            var sentTelemetry = new List<ITelemetry>();
            var telemetryProcessorChainWithSampling = CreateTelemetryProcessorChainWithSampling(sentTelemetry, SamplingPercentage, excludedTypes);

            for (int i = 0; i < ItemsToGenerate; i++)
            {
                sendAction.Invoke(telemetryProcessorChainWithSampling);
            }

            Assert.NotNull(sentTelemetry[0] as ISupportSampling);
            Assert.True(sentTelemetry.Count > 0);
            Assert.True(sentTelemetry.Count < ItemsToGenerate);
            Assert.Equal(SamplingPercentage, ((ISupportSampling)sentTelemetry[0]).SamplingPercentage);
        }

        private static TelemetryProcessorChain CreateTelemetryProcessorChainWithSampling(IList<ITelemetry> sentTelemetry, double samplingPercentage, string excludedTypes = null)
        {
            var tc = new TelemetryConfiguration {TelemetryChannel = new StubTelemetryChannel()};
            var channelBuilder = new TelemetryProcessorChainBuilder(tc);            
            channelBuilder.UseSampling(samplingPercentage, excludedTypes);
            channelBuilder.Use(next => new StubTelemetryProcessor(next) { OnProcess = t => sentTelemetry.Add(t) });
            
            channelBuilder.Build();

            return tc.TelemetryProcessorChain;
        }
    }
}
