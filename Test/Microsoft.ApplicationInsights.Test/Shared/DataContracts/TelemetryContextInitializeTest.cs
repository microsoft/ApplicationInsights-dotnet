namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// If a user sets the context on a TelemetryClient, it is expected that those values will propagate to individual TelemetryItems.
    /// </summary>
    [TestClass]
    public class TelemetryContextInitializeTest
    {
        private TelemetryBuffer TelemetryBuffer;
        private ITelemetryChannel TelemetryChannel;
        private TelemetryClient TelemetryClient;

        #region Telemetry Context Properties
        private const string TestInstrumentationKey = "00000000-0000-0000-0000-000000000000";

        private const string TestComponentVersion = "TestComponentVersion";

        private const string TestDeviceType = "TestDeviceType";
        private const string TestDeviceId = "TestDeviceId";
        private const string TestDeviceOperatingSystem = "TestDeviceOperatingSystem";
        private const string TestDeviceOemName = "TestDeviceOemName";
        private const string TestDeviceModel = "TestDeviceModel";

        private const string TestCloudRoleName = "TestCloudRoleName";
        private const string TestCloudRoleInstance = "TestCloudRoleInstance";

        private const string TestSessionId = "TestSessionId";
        private const bool TestSessionIsFirst = true;

        private const string TestUserId = "TestUserId";
        private const string TestUserAccountId = "TestUserAccountId";
        private const string TestUserUserAgent = "TestUserUserAgent";
        private const string TestUserAuthenticatedUserId = "TestUserAuthenticatedUserId";

        private const string TestOperationId = "TestOperationId";
        private const string TestOperationParentId = "TestOperationParentId";
        private const string TestOperationCorrelationVector = "TestOperationCorrelationVector";
        private const string TestOperationSyntheticSource = "TestOperationSyntheticSource";
        private const string TestOperationName = "TestOperationName";

        private const string TestLocationIp = "TestLocationIp";

        private const string TestInternalSdkVersion = "TestInternalSdkVersion";
        private const string TestInternalAgentVersion = "TestInternalAgentVersion";
        private const string TestInternalNodeName = "TestInternalNodeName";
        #endregion

        [TestInitialize]
        public void Initialize()
        {
            this.TelemetryBuffer = new TelemetryBuffer();
            this.TelemetryChannel = new InMemoryChannel(this.TelemetryBuffer, new InMemoryTransmitter(this.TelemetryBuffer));
            this.TelemetryClient = new TelemetryClient(new Extensibility.TelemetryConfiguration(instrumentationKey: TestInstrumentationKey, channel: this.TelemetryChannel));

            this.TelemetryClient.Context.Component.Version = TestComponentVersion;

            this.TelemetryClient.Context.Device.Type = TestDeviceType;
            this.TelemetryClient.Context.Device.Id = TestDeviceId;
            this.TelemetryClient.Context.Device.OperatingSystem = TestDeviceOperatingSystem;
            this.TelemetryClient.Context.Device.OemName = TestDeviceOemName;
            this.TelemetryClient.Context.Device.Model = TestDeviceModel;

            this.TelemetryClient.Context.Cloud.RoleName = TestCloudRoleName;
            this.TelemetryClient.Context.Cloud.RoleInstance = TestCloudRoleInstance;

            this.TelemetryClient.Context.Session.Id = TestSessionId;
            this.TelemetryClient.Context.Session.IsFirst = TestSessionIsFirst;

            this.TelemetryClient.Context.User.Id = TestUserId;
            this.TelemetryClient.Context.User.AccountId = TestUserAccountId;
            this.TelemetryClient.Context.User.UserAgent = TestUserUserAgent;
            this.TelemetryClient.Context.User.AuthenticatedUserId = TestUserAuthenticatedUserId;

            this.TelemetryClient.Context.Operation.Id = TestOperationId;
            this.TelemetryClient.Context.Operation.ParentId = TestOperationParentId;
            this.TelemetryClient.Context.Operation.CorrelationVector = TestOperationCorrelationVector;
            this.TelemetryClient.Context.Operation.SyntheticSource = TestOperationSyntheticSource;
            this.TelemetryClient.Context.Operation.Name = TestOperationName;

            this.TelemetryClient.Context.Location.Ip = TestLocationIp;

            this.TelemetryClient.Context.Internal.SdkVersion = TestInternalSdkVersion;
            this.TelemetryClient.Context.Internal.AgentVersion = TestInternalAgentVersion;
            this.TelemetryClient.Context.Internal.NodeName = TestInternalNodeName;
        }

        [TestMethod]
        public void VerifyContextPropertiesAreCopied()
        {
            var testDependencyName = "TestDependency";
            var testCommandName = "TestCommand";
            var testStartTime = DateTime.Parse("2000-01-01");
            var testTimeSpan = new TimeSpan(0, 0, 5);
            var testSuccess = true;

            this.TelemetryClient.TrackDependency(dependencyName: testDependencyName, commandName: testCommandName, startTime: testStartTime, duration: testTimeSpan, success: testSuccess);

            IEnumerable<ITelemetry> telemetryItems = this.TelemetryBuffer.Dequeue();
            Assert.AreEqual(1, telemetryItems.Count());

            var dependencyTelemetryItem = telemetryItems.First() as DependencyTelemetry;
            Assert.AreEqual(testDependencyName, dependencyTelemetryItem.Name);
            Assert.AreEqual(testCommandName, dependencyTelemetryItem.Data);
            Assert.AreEqual(testSuccess, dependencyTelemetryItem.Success);

            VerifyContext(dependencyTelemetryItem);
        }

        private void VerifyContext(ITelemetry telemetryItem)
        {
            Assert.AreEqual(TestInstrumentationKey, telemetryItem.Context.InstrumentationKey);

            Assert.AreEqual(TestComponentVersion, telemetryItem.Context.Component.Version);
            
            Assert.AreEqual(TestDeviceType, telemetryItem.Context.Device.Type);
            Assert.AreEqual(TestDeviceId, telemetryItem.Context.Device.Id);
            Assert.AreEqual(TestDeviceOperatingSystem, telemetryItem.Context.Device.OperatingSystem);
            Assert.AreEqual(TestDeviceOemName, telemetryItem.Context.Device.OemName);
            Assert.AreEqual(TestDeviceModel, telemetryItem.Context.Device.Model);
            
            Assert.AreEqual(TestCloudRoleName, telemetryItem.Context.Cloud.RoleName);
            Assert.AreEqual(TestCloudRoleInstance, telemetryItem.Context.Cloud.RoleInstance);
            
            Assert.AreEqual(TestSessionId, telemetryItem.Context.Session.Id);
            Assert.AreEqual(TestSessionIsFirst, telemetryItem.Context.Session.IsFirst);
            
            Assert.AreEqual(TestUserId, telemetryItem.Context.User.Id);
            Assert.AreEqual(TestUserAccountId, telemetryItem.Context.User.AccountId);
            Assert.AreEqual(TestUserUserAgent, telemetryItem.Context.User.UserAgent);
            Assert.AreEqual(TestUserAuthenticatedUserId, telemetryItem.Context.User.AuthenticatedUserId);
            
            Assert.AreEqual(TestOperationId, telemetryItem.Context.Operation.Id);
            Assert.AreEqual(TestOperationParentId, telemetryItem.Context.Operation.ParentId);
            Assert.AreEqual(TestOperationCorrelationVector, telemetryItem.Context.Operation.CorrelationVector);
            Assert.AreEqual(TestOperationSyntheticSource, telemetryItem.Context.Operation.SyntheticSource);
            Assert.AreEqual(TestOperationName, telemetryItem.Context.Operation.Name);
            
            Assert.AreEqual(TestLocationIp, telemetryItem.Context.Location.Ip);
            
            Assert.AreEqual(TestInternalSdkVersion, telemetryItem.Context.Internal.SdkVersion);
            Assert.AreEqual(TestInternalAgentVersion, telemetryItem.Context.Internal.AgentVersion);
            Assert.AreEqual(TestInternalNodeName, telemetryItem.Context.Internal.NodeName);
        }
    }
}
