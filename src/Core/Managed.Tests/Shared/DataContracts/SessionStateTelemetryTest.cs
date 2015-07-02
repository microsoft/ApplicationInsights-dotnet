namespace Microsoft.ApplicationInsights.DataContracts
{
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.TestFramework;
    using Microsoft.Developer.Analytics.DataCollection.Model.v2;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;

    [TestClass]
    public class SessionStateTelemetryTest
    {
        [TestMethod]
        public void SessionStateTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<SessionStateTelemetry, DataPlatformModel.SessionStateData>();
            test.Run();
        }

        [TestMethod]
        public void ConstructorInitializesStateWithSpecifiedValue()
        {
            var telemetry = new SessionStateTelemetry(SessionState.End);
            Assert.Equal(SessionState.End, telemetry.State);
        }

        [TestMethod]
        public void ParameterizedConstructorPerformsDefaultInitialization()
        {
            var telemetry = new SessionStateTelemetry(SessionState.Start);
            Assert.NotNull(telemetry.Context);
        }

        [TestMethod]
        public void SessionStateIsStartByDefault()
        {
            var telemetry = new SessionStateTelemetry();
            Assert.Equal(SessionState.Start, telemetry.State);
        }

        [TestMethod]
        public void SessionStateCanBeSetByUser()
        {
            var telemetry = new SessionStateTelemetry();
            telemetry.State = SessionState.End;
            Assert.Equal(SessionState.End, telemetry.State);
        }

        [TestMethod]
        public void SerializeWritesStateAsExpectedByEndpoint()
        {
            var telemetry = new SessionStateTelemetry { State = SessionState.End };
            TelemetryItem<SessionStateData> envelope = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<SessionStateTelemetry, SessionStateData>(telemetry);
            Assert.Equal(DataPlatformModel.SessionState.End, envelope.Data.BaseData.State);
            Assert.Equal(2, envelope.Data.BaseData.Ver);
        }

        [TestMethod]
        public void SessionStateTelemetryIsNotSubjectToSampling()
        {
            var sentTelemetry = new List<ITelemetry>();
            var channel = new StubTelemetryChannel { OnSend = t => sentTelemetry.Add(t) };
            var configuration = new TelemetryConfiguration { InstrumentationKey = "Test key" };

            var client = new TelemetryClient(configuration) { Channel = channel, SamplingPercentage = 10 };

            const int ItemsToGenerate = 100;

            for (int i = 0; i < 100; i++)
            {
                client.Track(new SessionStateTelemetry());
            }

            Assert.Equal(ItemsToGenerate, sentTelemetry.Count);
        }
    }
}
