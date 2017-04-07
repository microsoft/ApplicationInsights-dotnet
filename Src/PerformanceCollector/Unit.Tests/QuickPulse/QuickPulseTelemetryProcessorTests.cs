namespace Unit.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.Implementation.QuickPulse;
    using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
    using Microsoft.ApplicationInsights.Web.Helpers;
    using Microsoft.ManagementServices.RealTimeDataProcessing.QuickPulseService;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class QuickPulseTelemetryProcessorTests
    {
        const int MaxFieldLength = 32768;

        [TestInitialize]
        public void TestInitialize()
        {
            QuickPulseTestHelper.ClearEnvironment();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void QuickPulseTelemetryProcessorThrowsIfNextIsNull()
        {
            new QuickPulseTelemetryProcessor(null);
        }
        
        [TestMethod]
        public void QuickPulseTelemetryProcessorRegistersWithModule()
        {
            // ARRANGE
            var module = new QuickPulseTelemetryModule(null, null, null, null, null, null);

            TelemetryModules.Instance.Modules.Add(module);

            // ACT
            var spy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(spy);
            telemetryProcessor.Initialize(new TelemetryConfiguration());

            // ASSERT
            Assert.AreEqual(telemetryProcessor, QuickPulseTestHelper.GetTelemetryProcessors(module).Single());
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorCallsNext()
        {
            // ARRANGE
            var spy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(spy);

            // ACT
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(1, spy.ReceivedCalls);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfRequests()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = false,
                        ResponseCode = "200",
                        Duration = TimeSpan.FromSeconds(1),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = true,
                        ResponseCode = "200",
                        Duration = TimeSpan.FromSeconds(2),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = false,
                        ResponseCode = string.Empty,
                        Duration = TimeSpan.FromSeconds(3),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = null,
                        ResponseCode = string.Empty,
                        Duration = TimeSpan.FromSeconds(4),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = true,
                        ResponseCode = string.Empty,
                        Duration = TimeSpan.FromSeconds(5),
                        Context = { InstrumentationKey = "some ikey" }
                    });
            telemetryProcessor.Process(
                new RequestTelemetry()
                    {
                        Success = null,
                        ResponseCode = "404",
                        Duration = TimeSpan.FromSeconds(6),
                        Context = { InstrumentationKey = "some ikey" }
                    });

            // ASSERT
            Assert.AreEqual(6, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(
                1 + 2 + 3 + 4 + 5 + 6,
                TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulator.AIRequestDurationInTicks).TotalSeconds);
            Assert.AreEqual(4, accumulatorManager.CurrentDataAccumulator.AIRequestSuccessCount);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulator.AIRequestFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfDependencies()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1), Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = true, Duration = TimeSpan.FromSeconds(1), Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = false, Duration = TimeSpan.FromSeconds(2), Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Success = null, Duration = TimeSpan.FromSeconds(3), Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(4, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
            Assert.AreEqual(1 + 1 + 2 + 3, TimeSpan.FromTicks(accumulatorManager.CurrentDataAccumulator.AIDependencyCallDurationInTicks).TotalSeconds);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulator.AIDependencyCallSuccessCount);
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIDependencyCallFailureCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorKeepsAccurateCountOfExceptions()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(3, accumulatorManager.CurrentDataAccumulator.AIExceptionCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorStopsCollection()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var endpoint = new Uri("http://microsoft.com");
            var config = new TelemetryConfiguration() { InstrumentationKey = "some ikey" };
            
            // ACT
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(accumulatorManager, endpoint, config);
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StopCollection();
            telemetryProcessor.Process(new DependencyTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorIgnoresUnrelatedTelemetryItems()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(new EventTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new ExceptionTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new MetricTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new PageViewTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            telemetryProcessor.Process(new TraceTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorIgnoresTelemetryItemsToDifferentInstrumentationKeys()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // ACT
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some other ikey" } });
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });

            // ASSERT
            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorHandlesMultipleThreadsCorrectly()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = "some ikey" });

            // expected data loss if threading is misimplemented is around 10% (established through experiment)
            int taskCount = 10000;
            var tasks = new List<Task>(taskCount);

            for (int i = 0; i < taskCount; i++)
            {
                var requestTelemetry = new RequestTelemetry()
                                           {
                                               ResponseCode = (i % 2 == 0) ? "200" : "500",
                                               Duration = TimeSpan.FromMilliseconds(i),
                                               Context = { InstrumentationKey = "some ikey" }
                                           };

                var task = new Task(() => telemetryProcessor.Process(requestTelemetry));
                tasks.Add(task);
            }

            // ACT
            tasks.ForEach(task => task.Start());

            Task.WaitAll(tasks.ToArray());

            // ASSERT
            Assert.AreEqual(taskCount, accumulatorManager.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(taskCount / 2, accumulatorManager.CurrentDataAccumulator.AIRequestSuccessCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorSwitchesBetweenMultipleAccumulatorManagers()
        {
            // ARRANGE
            var accumulatorManager1 = new QuickPulseDataAccumulatorManager();
            var accumulatorManager2 = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            // ACT
            var serviceEndpoint = new Uri("http://microsoft.com");
            var config = new TelemetryConfiguration() { InstrumentationKey = "some ikey" };
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager1,
                serviceEndpoint,
                config);
            telemetryProcessor.Process(new RequestTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StopCollection();

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager2,
                serviceEndpoint,
                config);
            telemetryProcessor.Process(new DependencyTelemetry() { Context = { InstrumentationKey = "some ikey" } });
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StopCollection();

            // ASSERT
            Assert.AreEqual(1, accumulatorManager1.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(0, accumulatorManager1.CurrentDataAccumulator.AIDependencyCallCount);

            Assert.AreEqual(0, accumulatorManager2.CurrentDataAccumulator.AIRequestCount);
            Assert.AreEqual(1, accumulatorManager2.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void QuickPulseTelemetryProcessorMustBeStoppedBeforeReceivingStartCommand()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://test.com"),
                new TelemetryConfiguration());

            // ACT
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://test.com"),
                new TelemetryConfiguration());

            // ASSERT
            // must throw
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorFiltersOutDependencyCallsToQuickPulseServiceDuringCollection()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var simpleTelemetryProcessorSpy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(simpleTelemetryProcessorSpy);
            var config = new TelemetryConfiguration() { InstrumentationKey = "some ikey" };

            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("https://qps.cloudapp.net/endpoint.svc"),
                config);

            // ACT
            telemetryProcessor.Process(
                new DependencyTelemetry() { Target = "microsoft.ru", Context = { InstrumentationKey = config.InstrumentationKey } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Target = "qps.cloudapp.net", Context = { InstrumentationKey = config.InstrumentationKey } });
            telemetryProcessor.Process(
               new DependencyTelemetry() { Target = "qps.cloudapp.net | I5UqryrMvK", Context = { InstrumentationKey = config.InstrumentationKey } });
            telemetryProcessor.Process(
                new DependencyTelemetry() { Target = "bing.com", Context = { InstrumentationKey = config.InstrumentationKey } });

            // ASSERT
            Assert.AreEqual(2, simpleTelemetryProcessorSpy.ReceivedCalls);
            Assert.AreEqual("microsoft.ru", (simpleTelemetryProcessorSpy.ReceivedItems[0] as DependencyTelemetry).Target);
            Assert.AreEqual("bing.com", (simpleTelemetryProcessorSpy.ReceivedItems[1] as DependencyTelemetry).Target);
            Assert.AreEqual(2, accumulatorManager.CurrentDataAccumulator.AIDependencyCallCount);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorFiltersOutDependencyCallsToDefaultQuickPulseServiceEndpointInIdleMode()
        {
            // ARRANGE
            var simpleTelemetryProcessorSpy = new SimpleTelemetryProcessorSpy();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(simpleTelemetryProcessorSpy);
            
            // ACT
            telemetryProcessor.Process(new DependencyTelemetry() { Target = "microsoft.ru" });
            telemetryProcessor.Process(new DependencyTelemetry() { Target = "rt.services.visualstudio.com" });
            telemetryProcessor.Process(new DependencyTelemetry() { Target = "rt.services.visualstudio.com | I5UqryrMvK" });
            telemetryProcessor.Process(new DependencyTelemetry() { Target = "bing.com" });

            // ASSERT
            Assert.AreEqual(2, simpleTelemetryProcessorSpy.ReceivedCalls);
            Assert.AreEqual("microsoft.ru", (simpleTelemetryProcessorSpy.ReceivedItems[0] as DependencyTelemetry).Target);
            Assert.AreEqual("bing.com", (simpleTelemetryProcessorSpy.ReceivedItems[1] as DependencyTelemetry).Target);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorCollectsFullTelemetryItems()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var request = new RequestTelemetry()
                              {
                                  Name = Guid.NewGuid().ToString(),
                                  Success = false,
                                  ResponseCode = "500",
                                  Duration = TimeSpan.FromSeconds(1),
                                  Properties = { { "Prop1", "Val1" }, { "Prop2", "Val2" }, { "Prop3", "Val3" }, { "Prop4", "Val4" } },
                                  Context = { InstrumentationKey = instrumentationKey }
                              };

            var dependency = new DependencyTelemetry()
                                 {
                                     Name = Guid.NewGuid().ToString(),
                                     Success = false,
                                     Duration = TimeSpan.FromSeconds(1),
                                     Properties = { { "Prop1", "Val1" }, { "Prop2", "Val2" }, { "Prop3", "Val3" }, { "Prop4", "Val4" }, { "ErrorMessage", "EMValue" } },
                                     Context = { InstrumentationKey = instrumentationKey }
                                 };
            
            var exception = new ExceptionTelemetry(new ArgumentNullException())
                                {
                                    Properties = { { "Prop1", "Val1" }, { "Prop2", "Val2" }, { "Prop3", "Val3" }, { "Prop4", "Val4" } },
                                    Context = { InstrumentationKey = instrumentationKey }
                                };

            telemetryProcessor.Process(request);
            telemetryProcessor.Process(dependency);
            telemetryProcessor.Process(exception);

            // ASSERT
            var collectedTelemetry = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.ToArray().Reverse().ToArray();

            Assert.AreEqual(3, accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Count);

            Assert.AreEqual(TelemetryDocumentType.Request, Enum.Parse(typeof(TelemetryDocumentType), collectedTelemetry[0].DocumentType));
            Assert.AreEqual(request.Name, ((RequestTelemetryDocument)collectedTelemetry[0]).Name);
            Assert.AreEqual(3, collectedTelemetry[0].Properties.Length);

            Assert.IsTrue(collectedTelemetry[0].Properties.ToList().TrueForAll(pair => pair.Key.Contains("Prop") && pair.Value.Contains("Val")));
            
            Assert.AreEqual(TelemetryDocumentType.RemoteDependency, Enum.Parse(typeof(TelemetryDocumentType), collectedTelemetry[1].DocumentType));
            Assert.AreEqual(dependency.Name, ((DependencyTelemetryDocument)collectedTelemetry[1]).Name);
            Assert.AreEqual(3 + 1, collectedTelemetry[1].Properties.Length);

            Assert.IsTrue(collectedTelemetry[1].Properties.ToList().TrueForAll(pair => (pair.Key.Contains("Prop") && pair.Value.Contains("Val")) || (pair.Key == "ErrorMessage" && pair.Value == "EMValue")));

            Assert.AreEqual(TelemetryDocumentType.Exception, Enum.Parse(typeof(TelemetryDocumentType), collectedTelemetry[2].DocumentType));
            Assert.AreEqual(exception.Exception.ToString(), ((ExceptionTelemetryDocument)collectedTelemetry[2]).Exception);
            Assert.AreEqual(3, collectedTelemetry[2].Properties.Length);
            Assert.IsTrue(collectedTelemetry[2].Properties.ToList().TrueForAll(pair => (pair.Key.Contains("Prop") && pair.Value.Contains("Val"))));
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorDoesNotCollectSucceededFullTelemetryItems()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var request = new RequestTelemetry()
            {
                Success = true,
                ResponseCode = "200",
                Duration = TimeSpan.FromSeconds(1),
                Context = { InstrumentationKey = instrumentationKey }
            };

            var dependency = new DependencyTelemetry()
            {
                Success = true,
                Duration = TimeSpan.FromSeconds(1),
                Context = { InstrumentationKey = instrumentationKey }
            };

            var exception = new ExceptionTelemetry(new ArgumentException("bla")) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(request);
            telemetryProcessor.Process(dependency);
            telemetryProcessor.Process(exception);

            // ASSERT
            var collectedTelemetry = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.ToArray().Reverse().ToArray();

            Assert.AreEqual(1, accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Count);
            Assert.AreEqual(exception.Exception.ToString(), ((ExceptionTelemetryDocument)collectedTelemetry[0]).Exception);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorDoesNotCollectFullRequestTelemetryItemsOnceQuotaIsExhausted()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var timeProvider = new ClockMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy(), timeProvider, 60, 5);
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            int counter = 0;
            for (int i = 0; i < 100; i++)
            {
                var request = new RequestTelemetry()
                                  {
                                      Success = false,
                                      ResponseCode = "400",
                                      Duration = TimeSpan.FromSeconds(counter++),
                                      Context = { InstrumentationKey = instrumentationKey }
                                  };

                telemetryProcessor.Process(request);
            }

            timeProvider.FastForward(TimeSpan.FromSeconds(30));

            for (int i = 0; i < 100; i++)
            {
                var request = new RequestTelemetry()
                {
                    Success = false,
                    ResponseCode = "400",
                    Duration = TimeSpan.FromSeconds(counter++),
                    Context = { InstrumentationKey = instrumentationKey }
                };

                telemetryProcessor.Process(request);
            }

            // ASSERT
            var collectedTelemetry = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.ToArray().Reverse().Cast<RequestTelemetryDocument>().ToArray();

            Assert.AreEqual(5 + 30, accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Count);

            // out of the first 100 items we expect to see items 0 through 4 (the initial quota)
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, collectedTelemetry[i].Duration.TotalSeconds);
            }

            // out of the second 100 items we expect to see items 100 through 129 (the new quota for 30 seconds)
            for (int i = 5; i < 35; i++)
            {
                Assert.AreEqual(95 + i, collectedTelemetry[i].Duration.TotalSeconds);
            }
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorDoesNotCollectFullDependencyTelemetryItemsOnceQuotaIsExhausted()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var timeProvider = new ClockMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy(), timeProvider, 60, 5);
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            int counter = 0;
            for (int i = 0; i < 100; i++)
            {
                var dependency = new DependencyTelemetry()
                {
                    Success = false,
                    Duration = TimeSpan.FromSeconds(counter++),
                    Context = { InstrumentationKey = instrumentationKey }
                };

                telemetryProcessor.Process(dependency);
            }

            timeProvider.FastForward(TimeSpan.FromSeconds(30));

            for (int i = 0; i < 100; i++)
            {
                var dependency = new DependencyTelemetry()
                {
                    Success = false,
                    Duration = TimeSpan.FromSeconds(counter++),
                    Context = { InstrumentationKey = instrumentationKey }
                };

                telemetryProcessor.Process(dependency);
            }

            // ASSERT
            var collectedTelemetry = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.ToArray().Reverse().Cast<DependencyTelemetryDocument>().ToArray();

            Assert.AreEqual(5 + 30, accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Count);

            // out of the first 100 items we expect to see items 0 through 4 (the initial quota)
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, collectedTelemetry[i].Duration.TotalSeconds);
            }

            // out of the second 100 items we expect to see items 100 through 129 (the new quota for 30 seconds)
            for (int i = 5; i < 35; i++)
            {
                Assert.AreEqual(95 + i, collectedTelemetry[i].Duration.TotalSeconds);
            }
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorDoesNotCollectFullExceptionTelemetryItemsOnceQuotaIsExhausted()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var timeProvider = new ClockMock();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy(), timeProvider, 60, 5);
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            int counter = 0;
            for (int i = 0; i < 100; i++)
            {
                var exception = new ExceptionTelemetry(new Exception((counter++).ToString(CultureInfo.InvariantCulture)))
                                    {
                                        Context = { InstrumentationKey = instrumentationKey }
                                    };

                telemetryProcessor.Process(exception);
            }

            timeProvider.FastForward(TimeSpan.FromSeconds(30));

            for (int i = 0; i < 100; i++)
            {
                var exception = new ExceptionTelemetry(new Exception((counter++).ToString(CultureInfo.InvariantCulture)))
                                    {
                                        Context = { InstrumentationKey = instrumentationKey }
                                    };

                telemetryProcessor.Process(exception);
            }

            // ASSERT
            var collectedTelemetry = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.ToArray().Reverse().Cast<ExceptionTelemetryDocument>().ToArray();

            Assert.AreEqual(5 + 30, accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Count);

            // out of the first 100 items we expect to see items 0 through 4 (the initial quota)
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, int.Parse(collectedTelemetry[i].ExceptionMessage, CultureInfo.InvariantCulture));
            }

            // out of the second 100 items we expect to see items 100 through 129 (the new quota for 30 seconds)
            for (int i = 5; i < 35; i++)
            {
                Assert.AreEqual(95 + i, int.Parse(collectedTelemetry[i].ExceptionMessage, CultureInfo.InvariantCulture));
            }
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorDoesNotCollectFullTelemetryItemsWhenSwitchIsOff()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey },
                disableFullTelemetryItems: true);

            // ACT
            var request = new RequestTelemetry()
            {
                Success = false,
                ResponseCode = "500",
                Duration = TimeSpan.FromSeconds(1),
                Context = { InstrumentationKey = instrumentationKey }
            };

            var dependency = new DependencyTelemetry()
            {
                Success = false,
                Duration = TimeSpan.FromSeconds(1),
                Context = { InstrumentationKey = instrumentationKey }
            };

            var exception = new ExceptionTelemetry(new ArgumentException("bla")) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(request);
            telemetryProcessor.Process(dependency);
            telemetryProcessor.Process(exception);

            // ASSERT
            Assert.AreEqual(0, accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Count);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorTruncatesLongFullRequestTelemetryItemName()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var requestShort = new RequestTelemetry(new string('r', MaxFieldLength), DateTimeOffset.Now, TimeSpan.FromSeconds(1), "500", false)
                { Context = { InstrumentationKey = instrumentationKey } };
            var requestLong = new RequestTelemetry(new string('r', MaxFieldLength + 1), DateTimeOffset.Now, TimeSpan.FromSeconds(1), "500", false)
                { Context = { InstrumentationKey = instrumentationKey } };

            // process in the opposite order to allow for an easier validation order
            telemetryProcessor.Process(requestLong);
            telemetryProcessor.Process(requestShort);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<RequestTelemetryDocument>().ToList();

            Assert.AreEqual(telemetryDocuments[0].Name, requestShort.Name);
            Assert.AreEqual(telemetryDocuments[1].Name, requestShort.Name);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorTruncatesLongFullRequestTelemetryItemProperties()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var requestShort = new RequestTelemetry("requestShort", DateTimeOffset.Now, TimeSpan.FromSeconds(1), "500", false)
            {
                Properties = { { new string('p', MaxFieldLength), new string('v', MaxFieldLength) } },
                Context = { InstrumentationKey = instrumentationKey }
            };

            var requestLong = new RequestTelemetry("requestLong", DateTimeOffset.Now, TimeSpan.FromSeconds(1), "500", false)
            {
                Properties = { { new string('p', MaxFieldLength + 1), new string('v', MaxFieldLength + 1) } },
                Context = { InstrumentationKey = instrumentationKey }
            };

            // process in the opposite order to allow for an easier validation order
            telemetryProcessor.Process(requestLong);
            telemetryProcessor.Process(requestShort);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<RequestTelemetryDocument>().ToList();

            var actual = telemetryDocuments[0].Properties.First();
            var expected = requestShort.Properties.First();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);

            actual = telemetryDocuments[1].Properties.First();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorTruncatesLongFullDependencyTelemetryItemCommandName()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var dependencyShort = new DependencyTelemetry(
                new string('c', MaxFieldLength),
                new string('c', MaxFieldLength),
                new string('c', MaxFieldLength),
                new string('c', MaxFieldLength),
                DateTimeOffset.Now,
                TimeSpan.FromSeconds(1),
                new string('c', MaxFieldLength),
                false) { Context = { InstrumentationKey = instrumentationKey } };

            var dependencyLong = new DependencyTelemetry(
                new string('c', MaxFieldLength + 1),
                new string('c', MaxFieldLength + 1),
                new string('c', MaxFieldLength + 1),
                new string('c', MaxFieldLength + 1),
                DateTimeOffset.Now,
                TimeSpan.FromSeconds(1),
                new string('c', MaxFieldLength + 1),
                false) { Context = { InstrumentationKey = instrumentationKey } };

            // process in the opposite order to allow for an easier validation order
            telemetryProcessor.Process(dependencyLong);
            telemetryProcessor.Process(dependencyShort);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<DependencyTelemetryDocument>().ToList();

            Assert.AreEqual(telemetryDocuments[0].CommandName, dependencyShort.Data);
            Assert.AreEqual(telemetryDocuments[1].CommandName, dependencyLong.Data.Substring(0, MaxFieldLength));
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorTruncatesLongFullDependencyTelemetryItemName()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var dependencyShort = new DependencyTelemetry(
                new string('c', MaxFieldLength),
                new string('c', MaxFieldLength),
                new string('c', MaxFieldLength),
                new string('c', MaxFieldLength),
                DateTimeOffset.Now,
                TimeSpan.FromSeconds(1),
                new string('c', MaxFieldLength),
                false)
            { Context = { InstrumentationKey = instrumentationKey } };

            var dependencyLong = new DependencyTelemetry(
                new string('c', MaxFieldLength + 1),
                new string('c', MaxFieldLength + 1),
                new string('c', MaxFieldLength + 1),
                new string('c', MaxFieldLength + 1),
                DateTimeOffset.Now,
                TimeSpan.FromSeconds(1),
                new string('c', MaxFieldLength + 1),
                false)
            { Context = { InstrumentationKey = instrumentationKey } };

            // process in the opposite order to allow for an easier validation order
            telemetryProcessor.Process(dependencyLong);
            telemetryProcessor.Process(dependencyShort);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<DependencyTelemetryDocument>().ToList();

            Assert.AreEqual(telemetryDocuments[0].Name, dependencyShort.Name);
            Assert.AreEqual(telemetryDocuments[1].Name, dependencyLong.Name.Substring(0, MaxFieldLength));
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorTruncatesLongFullDependencyTelemetryItemProperties()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var dependencyShort = new DependencyTelemetry(
                "dependencyShort",
                "dependencyShort",
                "dependencyShort",
                "dependencyShort",
                DateTimeOffset.Now,
                TimeSpan.FromSeconds(1),
                "dependencyShort",
                false)
            {
                Properties = { { new string('p', MaxFieldLength), new string('v', MaxFieldLength) } },
                Context = { InstrumentationKey = instrumentationKey }
            };

            var dependencyLong = new DependencyTelemetry(
                "dependencyLong",
                "dependencyLong",
                "dependencyLong",
                "dependencyLong",
                DateTimeOffset.Now,
                TimeSpan.FromSeconds(1),
                "dependencyLong",
                false)
            {
                Properties = { { new string('p', MaxFieldLength + 1), new string('v', MaxFieldLength + 1) } },
                Context = { InstrumentationKey = instrumentationKey }
            };

            // process in the opposite order to allow for an easier validation order
            telemetryProcessor.Process(dependencyLong);
            telemetryProcessor.Process(dependencyShort);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<DependencyTelemetryDocument>().ToList();

            var expected = dependencyShort.Properties.First();
            var actual = telemetryDocuments[0].Properties.First();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);

            actual = telemetryDocuments[1].Properties.First();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorTruncatesLongFullExceptionTelemetryItemMessage()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var exceptionShort = new ExceptionTelemetry(new ArgumentException(new string('m', MaxFieldLength)))
            {
                Context = { InstrumentationKey = instrumentationKey }
            };

            var exceptionLong = new ExceptionTelemetry(new ArgumentException(new string('m', MaxFieldLength + 1)))
            {
                Context = { InstrumentationKey = instrumentationKey }
            };

            // process in the opposite order to allow for an easier validation order
            telemetryProcessor.Process(exceptionLong);
            telemetryProcessor.Process(exceptionShort);
            
            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual(telemetryDocuments[0].ExceptionMessage, exceptionShort.Exception.Message);
            Assert.AreEqual(telemetryDocuments[1].ExceptionMessage, exceptionLong.Exception.Message.Substring(0, MaxFieldLength));
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorTruncatesLongFullExceptionTelemetryItemProperties()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var exceptionShort = new ExceptionTelemetry(new ArgumentException())
            {
                Properties = { { new string('p', MaxFieldLength), new string('v', MaxFieldLength) } },
                Message = new string('m', MaxFieldLength),
                Context = { InstrumentationKey = instrumentationKey }
            };

            var exceptionLong = new ExceptionTelemetry(new ArgumentException())
            {
                Properties = { { new string('p', MaxFieldLength + 1), new string('v', MaxFieldLength + 1) } },
                Message = new string('m', MaxFieldLength),
                Context = { InstrumentationKey = instrumentationKey }
            };

            // process in the opposite order to allow for an easier validation order
            telemetryProcessor.Process(exceptionLong);
            telemetryProcessor.Process(exceptionShort);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            var expected = exceptionShort.Properties.First();
            var actual = telemetryDocuments[0].Properties.First();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);

            actual = telemetryDocuments[1].Properties.First();
            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Value, actual.Value);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorHandlesDuplicatePropertyNamesDueToTruncation()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            // ACT
            var exception = new ExceptionTelemetry(new ArgumentException())
            {
                Properties =
                {
                    { new string('p', MaxFieldLength + 1), "Val1" },
                    { new string('p', MaxFieldLength + 2), "Val2" }
                },
                Message = "Message",
                Context = { InstrumentationKey = instrumentationKey }
            };

            telemetryProcessor.Process(exception);
            
            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual(1, telemetryDocuments[0].Properties.Length);
            Assert.AreEqual(new string('p', MaxFieldLength), telemetryDocuments[0].Properties.First().Key);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorExpandsAggregateExceptionMessage()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            var exception1 = new Exception("Exception 1");
            var exception2 = new Exception("Exception 2");
            var exception3 = new AggregateException("Exception 3", new Exception("Exception 4"), new Exception("Exception 5"));

            var aggregateException = new AggregateException("Top level message", exception1, exception2, exception3);

            // ACT
            var exceptionTelemetry = new ExceptionTelemetry(aggregateException) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(exceptionTelemetry);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual("Exception 1 <--- Exception 2 <--- Exception 4 <--- Exception 5", telemetryDocuments[0].ExceptionMessage);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorExpandsAggregateExceptionMessageWhenEmpty()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            var exception = new AggregateException(string.Empty);

            // ACT
            var exceptionTelemetry = new ExceptionTelemetry(exception) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(exceptionTelemetry);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual(string.Empty, telemetryDocuments[0].ExceptionMessage);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorExpandsExceptionMessageWhenSingleInnerException()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            var exception = new Exception("Exception 1", new Exception("Exception 2"));

            // ACT
            var exceptionTelemetry = new ExceptionTelemetry(exception) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(exceptionTelemetry);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual("Exception 1 <--- Exception 2", telemetryDocuments[0].ExceptionMessage);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorExpandsExceptionMessageWhenNoInnerExceptions()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            var exception = new Exception("Exception 1");

            // ACT
            var exceptionTelemetry = new ExceptionTelemetry(exception) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(exceptionTelemetry);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual("Exception 1", telemetryDocuments[0].ExceptionMessage);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorExpandsExceptionMessageWhenMultipleInnerExceptions()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            var exception = new Exception("Exception 1", new Exception("Exception 2", new Exception("Exception 3")));

            // ACT
            var exceptionTelemetry = new ExceptionTelemetry(exception) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(exceptionTelemetry);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual("Exception 1 <--- Exception 2 <--- Exception 3", telemetryDocuments[0].ExceptionMessage);
        }

        [TestMethod]
        public void QuickPulseTelemetryProcessorExpandsExceptionMessagesAndDedupesThem()
        {
            // ARRANGE
            var accumulatorManager = new QuickPulseDataAccumulatorManager();
            var telemetryProcessor = new QuickPulseTelemetryProcessor(new SimpleTelemetryProcessorSpy());
            var instrumentationKey = "some ikey";
            ((IQuickPulseTelemetryProcessor)telemetryProcessor).StartCollection(
                accumulatorManager,
                new Uri("http://microsoft.com"),
                new TelemetryConfiguration() { InstrumentationKey = instrumentationKey });

            var exception = new AggregateException(
                "Exception 1",
                new Exception("Exception 1", new Exception("Exception 1")),
                new Exception("Exception 1"));

            // ACT
            var exceptionTelemetry = new ExceptionTelemetry(exception) { Context = { InstrumentationKey = instrumentationKey } };

            telemetryProcessor.Process(exceptionTelemetry);

            // ASSERT
            var telemetryDocuments = accumulatorManager.CurrentDataAccumulator.TelemetryDocuments.Cast<ExceptionTelemetryDocument>().ToList();

            Assert.AreEqual("Exception 1", telemetryDocuments[0].ExceptionMessage);
        }
    }
}