namespace Microsoft.ApplicationInsights.DataContracts
{
    using AI;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
#if !NETCOREAPP1_1
    using KellermanSoftware.CompareNetObjects;
#endif

    [TestClass]
    public class SessionStateTelemetryTest
    {
#pragma warning disable 618
        [TestMethod]
        public void SessionStateTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<SessionStateTelemetry, AI.EventData>();
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
            TelemetryItem<EventData> envelope = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<SessionStateTelemetry, EventData>(telemetry);
            Assert.Equal("Session ended", envelope.data.baseData.name);
            Assert.Equal(2, envelope.data.baseData.ver);
        }

#if !NETCOREAPP1_1
        [TestMethod]
        public void SessionStateTelemetryDeepCloneCopiesAllProperties()
        {
            var telemetry = new SessionStateTelemetry();
            telemetry.State = SessionState.End;
            var other = telemetry.DeepClone();

            CompareLogic deepComparator = new CompareLogic();

            var result = deepComparator.Compare(telemetry, other);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
#endif
#pragma warning restore 618
    }
}
