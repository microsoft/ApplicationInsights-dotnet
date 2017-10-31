using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Extensibility;

using System.Linq;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Metrics.TestUtil;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace Microsoft.ApplicationInsights.Metrics.Extensibility
{
    /// <summary />
    [TestClass]
    public class ApplicationInsightsTelemetryPipelineTests
    {
        /// <summary />
        [TestMethod]
        public void Ctor()
        {
            {
                Assert.ThrowsException<ArgumentNullException>( () => new ApplicationInsightsTelemetryPipeline((TelemetryClient) null) );
                Assert.ThrowsException<ArgumentNullException>( () => new ApplicationInsightsTelemetryPipeline((TelemetryConfiguration) null) );
            }
            {
                TelemetryConfiguration defaultPipeline = TelemetryConfiguration.Active;
                using (defaultPipeline)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(defaultPipeline);
                    Assert.IsNotNull(pipelineAdapter);
                }
            }
        }

        /// <summary />
        [TestMethod]
        public async Task TrackAsync_SendsCorrectly()
        {
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    using (var cancelControl = new CancellationTokenSource())
                    {
                        cancelControl.Cancel();
                        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => pipelineAdapter.TrackAsync(null, cancelControl.Token));
                    }
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    using (var cancelControl = new CancellationTokenSource())
                    {
                        cancelControl.Cancel();
                        await Assert.ThrowsExceptionAsync<ArgumentException>(() => pipelineAdapter.TrackAsync(
                                                                                                        new MetricAggregate("mid", "BadMoniker"),
                                                                                                        cancelControl.Token));
                    }
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    using (var cancelControl = new CancellationTokenSource())
                    {
                        cancelControl.Cancel();
                        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => pipelineAdapter.TrackAsync(
                                                                                new MetricAggregate("mid", MetricAggregateKinds.SimpleStatistics.Moniker),
                                                                                cancelControl.Token));
                    }
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    {
                        var agregate = new MetricAggregate("M1", MetricAggregateKinds.SimpleStatistics.Moniker);
                        agregate.AggregateData["Count"] = 1;
                        agregate.AggregateData["Sum"] = 10;
                        await pipelineAdapter.TrackAsync(agregate, CancellationToken.None);
                    }
                    {
                        var agregate = new MetricAggregate("M2", MetricAggregateKinds.SimpleStatistics.Moniker);
                        agregate.AggregateData["Count"] = 0;
                        agregate.AggregateData["Sum"] = 20;
                        await pipelineAdapter.TrackAsync(agregate, CancellationToken.None);
                    }
                    {
                        var agregate = new MetricAggregate("M3", MetricAggregateKinds.SimpleStatistics.Moniker);
                        agregate.AggregateData["Sum"] = 30;
                        await pipelineAdapter.TrackAsync(agregate, CancellationToken.None);
                    }
                    {
                        var agregate = new MetricAggregate("M4", MetricAggregateKinds.SimpleStatistics.Moniker);
                        agregate.AggregateData["Count"] = 2.9;
                        agregate.AggregateData["Sum"] = -40;
                        await pipelineAdapter.TrackAsync(agregate, CancellationToken.None);
                    }

                    // This is super strange. Count is changed from 0 to 1 in MetricTelemetry.Sanitize(). But it seems to happen only in Debug mode!
                    // This needs to be investigated! (@ToDo)
                    // It would indicate that we are sending different telemetry from exactly the same code depending on whether the app
                    // runs under a debugger. That wouldn't be good. (Noticed with SDK 2.3)
                    int expectedCountWhenZero = Debugger.IsAttached ? 1 : 0;

                    Assert.IsNotNull(telemetrySentToChannel);
                    Assert.AreEqual(4, telemetrySentToChannel.Count);

                    Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M1") ).Count());
                    Assert.AreEqual(1, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M1") ) as MetricTelemetry).Count);
                    Assert.AreEqual(10.0, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M1") ) as MetricTelemetry).Sum);

                    Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M2") ).Count());
                    Assert.AreEqual(expectedCountWhenZero, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M2") ) as MetricTelemetry).Count);
                    Assert.AreEqual(20.0, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M2") ) as MetricTelemetry).Sum);

                    Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M3") ).Count());
                    Assert.AreEqual(expectedCountWhenZero, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M3") ) as MetricTelemetry).Count);
                    Assert.AreEqual(30.0, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M3") ) as MetricTelemetry).Sum);

                    Assert.AreEqual(1, telemetrySentToChannel.Where( (item) => (item as MetricTelemetry).Name.Equals("M4") ).Count());
                    Assert.AreEqual(3, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M4") ) as MetricTelemetry).Count);
                    Assert.AreEqual(-40.0, (telemetrySentToChannel.First( (item) => (item as MetricTelemetry).Name.Equals("M4") ) as MetricTelemetry).Sum);
                }
            }
        }

        /// <summary />
        [TestMethod]
        public async Task TrackAsync_HandlesDifferentAggregates()
        {
            const string mockInstrumentationKey = "103CFCEC-BDA6-4EBC-B1F0-2652654DC6FD";

            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    var aggregate = new MetricAggregate("mid-foobar", "Something");
                    await Assert.ThrowsExceptionAsync<ArgumentException>( () => pipelineAdapter.TrackAsync(aggregate, CancellationToken.None) );

                    Assert.AreEqual(0, telemetrySentToChannel.Count);
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    telemetryPipeline.InstrumentationKey = mockInstrumentationKey;
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    var aggregate = new MetricAggregate("mid-foobar", "Microsoft.ApplicationInsights.SimpleStatistics");
                    await pipelineAdapter.TrackAsync(aggregate, CancellationToken.None);

                    Assert.AreEqual(1, telemetrySentToChannel.Count);
                    Assert.IsInstanceOfType(telemetrySentToChannel[0], typeof(MetricTelemetry));
                    MetricTelemetry metricTelemetry = (MetricTelemetry) telemetrySentToChannel[0];

                    // This is super strange. Count is changed from 0 to 1 in MetricTelemetry.Sanitize(). But it seems to happen only in Debug mode!
                    // This needs to be investigated! (@ToDo)
                    // It would indicate that we are sending different telemetry from exactly the same code depending on whether the app
                    // runs under a debugger. That wouldn't be good. (Noticed with SDK 2.3)
                    int expectedCount = Debugger.IsAttached ? 1 : 0;
                    Util.ValidateNumericAggregateValues(metricTelemetry, "mid-foobar", expectedCount, sum: 0, max: 0, min: 0, stdDev: 0);
                    
                    Assert.AreEqual(1, metricTelemetry.Properties.Count);
                    Assert.IsTrue(metricTelemetry.Context.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));
                    Assert.AreEqual("0", metricTelemetry.Context.Properties[Util.AggregationIntervalMonikerPropertyKey]);
                    Assert.IsTrue((metricTelemetry.Timestamp - DateTimeOffset.Now).Duration() < TimeSpan.FromMilliseconds(100));


                    Assert.AreEqual(mockInstrumentationKey, metricTelemetry.Context.InstrumentationKey);
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    telemetryPipeline.InstrumentationKey = mockInstrumentationKey;
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    var aggregate = new MetricAggregate("mid-foobar", "Microsoft.ApplicationInsights.SimpleStatistics");

                    aggregate.AggregationPeriodStart = new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8));
                    aggregate.AggregationPeriodDuration = TimeSpan.FromSeconds(90);

                    await pipelineAdapter.TrackAsync(aggregate, CancellationToken.None);

                    Assert.AreEqual(1, telemetrySentToChannel.Count);
                    Assert.IsInstanceOfType(telemetrySentToChannel[0], typeof(MetricTelemetry));
                    MetricTelemetry metricTelemetry = (MetricTelemetry) telemetrySentToChannel[0];

                    // This is super strange. Count is changed from 0 to 1 in MetricTelemetry.Sanitize(). But it seems to happen only in Debug mode!
                    // This needs to be investigated! (@ToDo)
                    // It would indicate that we are sending different telemetry from exactly the same code depending on whether the app
                    // runs under a debugger. That wouldn't be good. (Noticed with SDK 2.3)
                    int expectedCount = Debugger.IsAttached ? 1 : 0;
                    Util.ValidateNumericAggregateValues(
                                                    metricTelemetry,
                                                    "mid-foobar",
                                                    expectedCount,
                                                    sum:        0,
                                                    max:        0,
                                                    min:        0,
                                                    stdDev:     0,
                                                    timestamp:  new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8)),
                                                    periodMs:   "90000");
                    Assert.AreEqual(1, metricTelemetry.Properties.Count);

                    Assert.AreEqual(mockInstrumentationKey, metricTelemetry.Context.InstrumentationKey);
                    Util.ValidateSdkVersionString(metricTelemetry.Context.GetInternalContext().SdkVersion);
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    telemetryPipeline.InstrumentationKey = mockInstrumentationKey;
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    var aggregate = new MetricAggregate("mid-foobar", "Microsoft.ApplicationInsights.SimpleStatistics");

                    aggregate.AggregationPeriodStart = new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8));
                    aggregate.AggregationPeriodDuration = TimeSpan.FromSeconds(90);

                    aggregate.AggregateData["Foo"] = "Bar";

                    await pipelineAdapter.TrackAsync(aggregate, CancellationToken.None);

                    Assert.AreEqual(1, telemetrySentToChannel.Count);
                    Assert.IsInstanceOfType(telemetrySentToChannel[0], typeof(MetricTelemetry));
                    MetricTelemetry metricTelemetry = (MetricTelemetry) telemetrySentToChannel[0];

                    // This is super strange. Count is changed from 0 to 1 in MetricTelemetry.Sanitize(). But it seems to happen only in Debug mode!
                    // This needs to be investigated! (@ToDo)
                    // It would indicate that we are sending different telemetry from exactly the same code depending on whether the app
                    // runs under a debugger. That wouldn't be good. (Noticed with SDK 2.3)
                    int expectedCount = Debugger.IsAttached ? 1 : 0;
                    Util.ValidateNumericAggregateValues(
                                                    metricTelemetry,
                                                    "mid-foobar",
                                                    expectedCount,
                                                    sum:        0,
                                                    max:        0,
                                                    min:        0,
                                                    stdDev:     0,
                                                    timestamp:  new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8)),
                                                    periodMs:   "90000");
                    Assert.AreEqual(1, metricTelemetry.Properties.Count);

                    Assert.AreEqual(mockInstrumentationKey, metricTelemetry.Context.InstrumentationKey);
                    Util.ValidateSdkVersionString(metricTelemetry.Context.GetInternalContext().SdkVersion);
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    telemetryPipeline.InstrumentationKey = mockInstrumentationKey;
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    var aggregate = new MetricAggregate("mid-foobar", "Microsoft.ApplicationInsights.SimpleStatistics");

                    aggregate.AggregationPeriodStart = new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8));
                    aggregate.AggregationPeriodDuration = TimeSpan.FromSeconds(90);

                    aggregate.AggregateData["Murr"] = "Miau";
                    aggregate.AggregateData["Purr"] = null;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Count] = 1.2;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Sum] = "one";
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Min] = "2.3";
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Max] = "-4";
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev] = 5;

                    await pipelineAdapter.TrackAsync(aggregate, CancellationToken.None);

                    Assert.AreEqual(1, telemetrySentToChannel.Count);
                    Assert.IsInstanceOfType(telemetrySentToChannel[0], typeof(MetricTelemetry));
                    MetricTelemetry metricTelemetry = (MetricTelemetry) telemetrySentToChannel[0];

                    Util.ValidateNumericAggregateValues(
                                                    metricTelemetry,
                                                    "mid-foobar",
                                                    count:      1,
                                                    sum:        0,
                                                    max:        -4,
                                                    min:        2.3,
                                                    stdDev:     5,
                                                    timestamp:  new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8)),
                                                    periodMs:   "90000");
                    Assert.AreEqual(1, metricTelemetry.Properties.Count);

                    Assert.AreEqual(mockInstrumentationKey, metricTelemetry.Context.InstrumentationKey);
                    Util.ValidateSdkVersionString(metricTelemetry.Context.GetInternalContext().SdkVersion);
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    telemetryPipeline.InstrumentationKey = mockInstrumentationKey;
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    var aggregate = new MetricAggregate("mid-foobar", "Microsoft.ApplicationInsights.SimpleStatistics");

                    aggregate.AggregationPeriodStart = new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8));
                    aggregate.AggregationPeriodDuration = TimeSpan.FromSeconds(90);

                    aggregate.AggregateData["Murr"] = "Miau";
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Count] = -3.7;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Sum] = "-100";
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Min] = -10000000000;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Max] = ((double) Int32.MaxValue) + 100;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev] = -2;

                    aggregate.Dimensions["Dim 1"] = "DV1";
                    aggregate.Dimensions["Dim 2"] = "DV2";
                    aggregate.Dimensions["Dim 3"] = "DV3";
                    aggregate.Dimensions["Dim 2"] = "DV2a";
                    aggregate.Dimensions["Dim 4"] = "";
                    aggregate.Dimensions["Dim 5"] = null;
                    aggregate.Dimensions["Dim 6"] = "  ";

                    await pipelineAdapter.TrackAsync(aggregate, CancellationToken.None);

                    Assert.AreEqual(1, telemetrySentToChannel.Count);
                    Assert.IsInstanceOfType(telemetrySentToChannel[0], typeof(MetricTelemetry));
                    MetricTelemetry metricTelemetry = (MetricTelemetry) telemetrySentToChannel[0];

                    // This is super strange. Count is changed from 0 to 1 in MetricTelemetry.Sanitize(). But it seems to happen only in Debug mode!
                    // This needs to be investigated! (@ToDo)
                    // It would indicate that we are sending different telemetry from exactly the same code depending on whether the app
                    // runs under a debugger. That wouldn't be good. (Noticed with SDK 2.3)
                    int expectedCount = Debugger.IsAttached ? 1 : -4;
                    Util.ValidateNumericAggregateValues(
                                                    metricTelemetry,
                                                    "mid-foobar",
                                                    expectedCount,
                                                    sum:        -100,
                                                    max:        ((double) Int32.MaxValue) + 100,
                                                    min:        -10000000000,
                                                    stdDev:     -2,
                                                    timestamp:  new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8)),
                                                    periodMs:   "90000");

                    string props = $"metricTelemetry.Properties[{metricTelemetry.Properties.Count}] {{ ";
                    foreach (KeyValuePair<string, string> kvp in metricTelemetry.Properties)
                    {
                        props = props + $"[\"{kvp.Key}\"]=\"{kvp.Value}\", ";
                    }
                    props = props + " }";

                    // This is another super strange case where we seem to be gettin gdifferent results depending on whether a dubugger is attached.
                    // It seems to be the same Sanitization issue as above (@ToDo)
                    if (Debugger.IsAttached)
                    {
                        Assert.AreEqual(4, metricTelemetry.Properties.Count, props);
                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 1"));
                        Assert.AreEqual("DV1", metricTelemetry.Properties["Dim 1"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 2"));
                        Assert.AreEqual("DV2a", metricTelemetry.Properties["Dim 2"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 3"));
                        Assert.AreEqual("DV3", metricTelemetry.Properties["Dim 3"]);
                    }
                    else
                    {
                        Assert.AreEqual(6, metricTelemetry.Properties.Count, props);
                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 1"));
                        Assert.AreEqual("DV1", metricTelemetry.Properties["Dim 1"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 2"));
                        Assert.AreEqual("DV2a", metricTelemetry.Properties["Dim 2"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 3"));
                        Assert.AreEqual("DV3", metricTelemetry.Properties["Dim 3"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 4"));
                        Assert.AreEqual("", metricTelemetry.Properties["Dim 4"]);

                        Assert.IsFalse(metricTelemetry.Properties.ContainsKey("Dim 5"));

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 6"));
                        Assert.AreEqual("  ", metricTelemetry.Properties["Dim 6"]);
                    }

                    Assert.AreEqual(mockInstrumentationKey, metricTelemetry.Context.InstrumentationKey);
                    Util.ValidateSdkVersionString(metricTelemetry.Context.GetInternalContext().SdkVersion);
                }
            }
            {
                IList<ITelemetry> telemetrySentToChannel;
                TelemetryConfiguration telemetryPipeline = Util.CreateAITelemetryConfig(out telemetrySentToChannel);
                using (telemetryPipeline)
                {
                    telemetryPipeline.InstrumentationKey = mockInstrumentationKey;
                    var pipelineAdapter = new ApplicationInsightsTelemetryPipeline(telemetryPipeline);

                    var aggregate = new MetricAggregate("mid-foobar", "Microsoft.ApplicationInsights.SimpleStatistics");

                    aggregate.AggregationPeriodStart = new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8));
                    aggregate.AggregationPeriodDuration = TimeSpan.FromSeconds(90);

                    aggregate.AggregateData["Murr"] = "Miau";
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Count] = -3.7;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Sum] = null;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Min] = -10000000000;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.Max] = ((double) Int32.MaxValue) + 100;
                    aggregate.AggregateData[MetricAggregateKinds.SimpleStatistics.DataKeys.StdDev] = -2;

                    aggregate.Dimensions["Dim 1"] = "DV1";
                    aggregate.Dimensions["Dim 2"] = "DV2";
                    aggregate.Dimensions["Dim 3"] = "DV3";
                    aggregate.Dimensions["Dim 2"] = "DV2a";
                    aggregate.Dimensions["Dim 4"] = "";
                    aggregate.Dimensions["Dim 5"] = null;
                    aggregate.Dimensions["Dim 6"] = "  ";
                    aggregate.Dimensions[""] = "DVb1";
                    aggregate.Dimensions[" "] = "DVb2";
                    Assert.ThrowsException<ArgumentNullException>( () => { aggregate.Dimensions[null] = "DVb2"; } );

                    Assert.IsNull(aggregate.AdditionalDataContext);
                    var telemetryContext = new TelemetryContext();
                    telemetryContext.InstrumentationKey = "Aggregate's Instrumentsion Key";
                    telemetryContext.Properties["Prop 1"] = "PV1";
                    telemetryContext.Properties["Prop 2"] = "PV2";
                    telemetryContext.Properties["Dim 1"] = "PV3";
                    telemetryContext.Operation.Id = "OpId xx";
                    telemetryContext.Operation.Name = "OpName xx";
                    telemetryContext.User.UserAgent = "UA xx";
                    telemetryContext.Cloud.RoleName = "RN xx";
                    aggregate.AdditionalDataContext = telemetryContext;


                    await pipelineAdapter.TrackAsync(aggregate, CancellationToken.None);

                    Assert.AreEqual(1, telemetrySentToChannel.Count);
                    Assert.IsInstanceOfType(telemetrySentToChannel[0], typeof(MetricTelemetry));
                    MetricTelemetry metricTelemetry = (MetricTelemetry) telemetrySentToChannel[0];

                    // This is super strange. Count is changed from 0 to 1 in MetricTelemetry.Sanitize(). But it seems to happen only in Debug mode!
                    // This needs to be investigated! (@ToDo)
                    // It would indicate that we are sending different telemetry from exactly the same code depending on whether the app
                    // runs under a debugger. That wouldn't be good. (Noticed with SDK 2.3)
                    int expectedCount = Debugger.IsAttached ? 1 : -4;
                    Util.ValidateNumericAggregateValues(
                                                    metricTelemetry,
                                                    "mid-foobar",
                                                    expectedCount,
                                                    sum:        0,
                                                    max:        ((double) Int32.MaxValue) + 100,
                                                    min:        -10000000000,
                                                    stdDev:     -2,
                                                    timestamp:  new DateTimeOffset(2017, 10, 30, 0, 1, 0, TimeSpan.FromHours(8)),
                                                    periodMs:   "90000");

                    string props = $"metricTelemetry.Properties[{metricTelemetry.Properties.Count}] {{ ";
                    foreach (KeyValuePair<string, string> kvp in metricTelemetry.Properties)
                    {
                        props = props + $"[\"{kvp.Key}\"]=\"{kvp.Value}\", ";
                    }
                    props = props + " }";

                    // This is another super strange case where we seem to be gettin gdifferent results depending on whether a dubugger is attached.
                    // It seems to be the same Sanitization issue as above (@ToDo)
                    if (Debugger.IsAttached)
                    {
                        Assert.AreEqual(6, metricTelemetry.Properties.Count, props);
                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 1"));
                        Assert.AreEqual("DV1", metricTelemetry.Properties["Dim 1"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 2"));
                        Assert.AreEqual("DV2a", metricTelemetry.Properties["Dim 2"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 3"));
                        Assert.AreEqual("DV3", metricTelemetry.Properties["Dim 3"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Prop 1"));
                        Assert.AreEqual("PV1", metricTelemetry.Properties["Prop 1"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Prop 2"));
                        Assert.AreEqual("PV2", metricTelemetry.Properties["Prop 2"]);
                    }
                    else
                    {
                        Assert.AreEqual(8, metricTelemetry.Properties.Count, props);
                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey(Util.AggregationIntervalMonikerPropertyKey));

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 1"));
                        Assert.AreEqual("DV1", metricTelemetry.Properties["Dim 1"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 2"));
                        Assert.AreEqual("DV2a", metricTelemetry.Properties["Dim 2"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 3"));
                        Assert.AreEqual("DV3", metricTelemetry.Properties["Dim 3"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 4"));
                        Assert.AreEqual("", metricTelemetry.Properties["Dim 4"]);

                        Assert.IsFalse(metricTelemetry.Properties.ContainsKey("Dim 5"));

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Dim 6"));
                        Assert.AreEqual("  ", metricTelemetry.Properties["Dim 6"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Prop 1"));
                        Assert.AreEqual("PV1", metricTelemetry.Properties["Prop 1"]);

                        Assert.IsTrue(metricTelemetry.Properties.ContainsKey("Prop 2"));
                        Assert.AreEqual("PV2", metricTelemetry.Properties["Prop 2"]);
                    }

                    Assert.AreEqual("OpId xx", metricTelemetry.Context.Operation.Id);
                    Assert.AreEqual("OpName xx", metricTelemetry.Context.Operation.Name);
                    Assert.AreEqual("UA xx", metricTelemetry.Context.User.UserAgent);
                    Assert.AreEqual("RN xx", metricTelemetry.Context.Cloud.RoleName);

                    Assert.AreEqual("Aggregate's Instrumentsion Key", metricTelemetry.Context.InstrumentationKey);
                    Util.ValidateSdkVersionString(metricTelemetry.Context.GetInternalContext().SdkVersion);
                }
            }
        }
    }
}
