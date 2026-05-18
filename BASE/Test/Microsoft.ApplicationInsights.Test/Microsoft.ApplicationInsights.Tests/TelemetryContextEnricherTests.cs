namespace Microsoft.ApplicationInsights.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;
    using Xunit;

    public class TelemetryContextEnricherTests : IDisposable
    {
        private readonly ActivitySource activitySource;
        private readonly ActivityListener activityListener;

        public TelemetryContextEnricherTests()
        {
            this.activitySource = new ActivitySource("Test.TelemetryContextEnricher");
            this.activityListener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            };

            ActivitySource.AddActivityListener(this.activityListener);
        }

        public void Dispose()
        {
            this.activityListener.Dispose();
            this.activitySource.Dispose();
        }

        [Fact]
        public void ApplyToActivity_SkipsIfPresent()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";

            using var activity = this.CreateStoppedActivity();
            activity.SetTag(SemanticConventions.AttributeEnduserPseudoId, "existing-user");

            TelemetryContextEnricher.ApplyToActivity(activity, context);

            Assert.Equal("existing-user", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void ApplyToActivity_AppliesAllKnownContextFields()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "prod";
            context.User.Id = "user-123";
            context.User.AuthenticatedUserId = "auth-456";
            context.User.AccountId = "acct-789";
            context.User.UserAgent = "agent/1.0";
            context.Operation.Name = "GET /api";
            context.Operation.SyntheticSource = "bot";
            context.Location.Ip = "10.0.0.1";
            context.Session.Id = "session-1";
            context.Device.Id = "device-1";
            context.Device.Model = "Surface";
            context.Device.Type = "PC";
            context.Device.OperatingSystem = "Windows 11";

            using var activity = this.CreateStoppedActivity();
            TelemetryContextEnricher.ApplyToActivity(activity, context);

            Assert.Equal("prod", activity.GetTagItem("env"));
            Assert.Equal("user-123", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
            Assert.Equal("auth-456", activity.GetTagItem(SemanticConventions.AttributeEnduserId));
            Assert.Equal("acct-789", activity.GetTagItem(SemanticConventions.AttributeMicrosoftUserAccountId));
            Assert.Equal("agent/1.0", activity.GetTagItem(SemanticConventions.AttributeUserAgentOriginal));
            Assert.Equal("GET /api", activity.GetTagItem(SemanticConventions.AttributeMicrosoftOperationName));
            Assert.Equal("bot", activity.GetTagItem(SemanticConventions.AttributeMicrosoftSyntheticSource));
            Assert.Equal("10.0.0.1", activity.GetTagItem(SemanticConventions.AttributeMicrosoftClientIp));
            Assert.Equal("session-1", activity.GetTagItem(SemanticConventions.AttributeMicrosoftSessionId));
            Assert.Equal("device-1", activity.GetTagItem(SemanticConventions.AttributeAiDeviceId));
            Assert.Equal("Surface", activity.GetTagItem(SemanticConventions.AttributeAiDeviceModel));
            Assert.Equal("PC", activity.GetTagItem(SemanticConventions.AttributeAiDeviceType));
            Assert.Equal("Windows 11", activity.GetTagItem(SemanticConventions.AttributeAiDeviceOsVersion));
        }

        [Fact]
        public void ApplyToActivity_NullContextIsNoOp()
        {
            using var activity = this.CreateStoppedActivity();

            TelemetryContextEnricher.ApplyToActivity(activity, null);

            Assert.Empty(activity.Tags);
        }

        [Fact]
        public void ApplyToActivity_NullActivityIsNoOp()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-123";

            TelemetryContextEnricher.ApplyToActivity(null, context);
        }

        [Fact]
        public void BuildSnapshot_OmitsNullAndEmpty()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "prod";
            context.GlobalProperties["empty"] = string.Empty;
            context.User.Id = "user-123";
            context.Operation.Name = string.Empty;

            var snapshot = TelemetryContextEnricher.BuildSnapshot(context);

            Assert.Equal(2, snapshot.Tags.Length);
            Assert.Contains(snapshot.Tags, kvp => kvp.Key == "env" && kvp.Value == "prod");
            Assert.Contains(snapshot.Tags, kvp => kvp.Key == SemanticConventions.AttributeEnduserPseudoId && kvp.Value == "user-123");
        }

        [Fact]
        public void ApplySnapshot_DoesNotOverwriteExistingTags()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";
            var snapshot = TelemetryContextEnricher.BuildSnapshot(context);

            using var activity = this.CreateStoppedActivity();
            activity.SetTag(SemanticConventions.AttributeEnduserPseudoId, "existing-user");

            TelemetryContextEnricher.ApplySnapshotToActivity(activity, snapshot);

            Assert.Equal("existing-user", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void ApplyToLogAttributes_SkipsIfPresent()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";
            var attributes = new Dictionary<string, string>
            {
                [SemanticConventions.AttributeEnduserPseudoId] = "existing-user",
            };

            TelemetryContextEnricher.ApplyToLogAttributes(attributes, context);

            Assert.Equal("existing-user", attributes[SemanticConventions.AttributeEnduserPseudoId]);
        }

        [Fact]
        public void WarmupGate_FreezesAfterBothThresholds()
        {
            var gate = new WarmupGate();

            for (int i = 0; i < WarmupGate.CountThreshold - 1; i++)
            {
                Assert.False(gate.ShouldFreeze());
            }

            Assert.False(gate.ShouldFreeze());

            var field = typeof(WarmupGate).GetField("constructedTimestamp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(gate, Stopwatch.GetTimestamp() - (Stopwatch.Frequency * 10));

            Assert.True(gate.ShouldFreeze());
        }

        private Activity CreateStoppedActivity()
        {
            var activity = this.activitySource.StartActivity("TestOp");
            Assert.NotNull(activity);
            activity.Stop();
            return activity;
        }
    }
}
