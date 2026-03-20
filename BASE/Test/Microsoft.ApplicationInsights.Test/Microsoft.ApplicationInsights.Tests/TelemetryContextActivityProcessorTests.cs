namespace Microsoft.ApplicationInsights.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;
    using Xunit;

    public class TelemetryContextActivityProcessorTests : IDisposable
    {
        private readonly ActivitySource activitySource;
        private readonly ActivityListener activityListener;

        public TelemetryContextActivityProcessorTests()
        {
            this.activitySource = new ActivitySource("Test.TelemetryContextActivityProcessor");
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

        #region Basic tag application

        [Fact]
        public void OnEnd_AppliesUserIdTag()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-123";
            var processor = new TelemetryContextActivityProcessor(context);

            using var activity = this.CreateStoppedActivity();
            processor.OnEnd(activity);

            Assert.Equal("user-123", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void OnEnd_AppliesAllContextTags()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-123";
            context.User.AuthenticatedUserId = "auth-456";
            context.User.AccountId = "acct-789";
            context.Operation.Name = "GET /api";
            context.Operation.SyntheticSource = "bot";
            context.Location.Ip = "10.0.0.1";
            context.Session.Id = "session-1";
            context.Device.Id = "device-1";
            context.Device.Model = "Surface";
            context.Device.Type = "PC";
            context.Device.OperatingSystem = "Windows 11";

            var processor = new TelemetryContextActivityProcessor(context);
            using var activity = this.CreateStoppedActivity();
            processor.OnEnd(activity);

            Assert.Equal("user-123", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
            Assert.Equal("auth-456", activity.GetTagItem(SemanticConventions.AttributeEnduserId));
            Assert.Equal("acct-789", activity.GetTagItem(SemanticConventions.AttributeMicrosoftUserAccountId));
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
        public void OnEnd_AppliesGlobalProperties()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "production";
            context.GlobalProperties["region"] = "westus2";

            var processor = new TelemetryContextActivityProcessor(context);
            using var activity = this.CreateStoppedActivity();
            processor.OnEnd(activity);

            Assert.Equal("production", activity.GetTagItem("env"));
            Assert.Equal("westus2", activity.GetTagItem("region"));
        }

        #endregion

        #region Skip-if-present semantics

        [Fact]
        public void OnEnd_DoesNotOverwriteExistingTag()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";

            var processor = new TelemetryContextActivityProcessor(context);
            using var activity = this.CreateStoppedActivity();
            activity.SetTag(SemanticConventions.AttributeEnduserPseudoId, "item-user");
            processor.OnEnd(activity);

            Assert.Equal("item-user", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void OnEnd_DoesNotOverwriteExistingGlobalPropertyTag()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "production";

            var processor = new TelemetryContextActivityProcessor(context);
            using var activity = this.CreateStoppedActivity();
            activity.SetTag("env", "staging");
            processor.OnEnd(activity);

            Assert.Equal("staging", activity.GetTagItem("env"));
        }

        #endregion

        #region Null/empty handling

        [Fact]
        public void OnEnd_SkipsNullValues()
        {
            var context = new TelemetryContext();
            // Don't set any properties — all are null by default.
            var processor = new TelemetryContextActivityProcessor(context);
            using var activity = this.CreateStoppedActivity();
            processor.OnEnd(activity);

            Assert.Null(activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
            Assert.Null(activity.GetTagItem(SemanticConventions.AttributeEnduserId));
            Assert.Null(activity.GetTagItem(SemanticConventions.AttributeMicrosoftOperationName));
        }

        [Fact]
        public void OnEnd_HandlesNullActivity()
        {
            var context = new TelemetryContext();
            var processor = new TelemetryContextActivityProcessor(context);

            // Should not throw.
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_HandlesEmptyContext()
        {
            var context = new TelemetryContext();
            var processor = new TelemetryContextActivityProcessor(context);
            using var activity = this.CreateStoppedActivity();

            // Should not throw, and no tags should be added.
            processor.OnEnd(activity);

            var tags = new List<KeyValuePair<string, object>>();
            foreach (var tag in activity.Tags)
            {
                tags.Add(new KeyValuePair<string, object>(tag.Key, tag.Value));
            }

            // Only the tags that the Activity already had (none from context).
            Assert.Empty(tags);
        }

        #endregion

        #region Warmup and snapshot freezing

        [Fact]
        public void OnEnd_UsesSlowPathDuringWarmup()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextActivityProcessor(context);

            // During warmup, each call should still correctly apply tags.
            for (int i = 0; i < TelemetryContextActivityProcessor.WarmupCountThreshold - 1; i++)
            {
                using var activity = this.CreateStoppedActivity();
                processor.OnEnd(activity);
                Assert.Equal("user-1", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
            }
        }

        [Fact]
        public void OnEnd_FreezesSnapshotAfterWarmup()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";
            context.Device.Id = "device-1";

            var processor = new TelemetryContextActivityProcessor(context);
            ForceWarmupComplete(processor);

            // After warmup, snapshot should be frozen.
            // Verify by mutating context — the frozen snapshot should still have original values.
            context.User.Id = "user-CHANGED";

            using var activity = this.CreateStoppedActivity();
            processor.OnEnd(activity);

            // The frozen snapshot captured "user-1", so even though context changed,
            // the activity should get the original value.
            Assert.Equal("user-1", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
            Assert.Equal("device-1", activity.GetTagItem(SemanticConventions.AttributeAiDeviceId));
        }

        [Fact]
        public void OnEnd_SnapshotOnlyContainsNonNullValues()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";
            // All other properties are null.

            var processor = new TelemetryContextActivityProcessor(context);
            ForceWarmupComplete(processor);

            using var activity = this.CreateStoppedActivity();
            processor.OnEnd(activity);

            Assert.Equal("user-1", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
            // Null properties should not appear as tags.
            Assert.Null(activity.GetTagItem(SemanticConventions.AttributeAiDeviceId));
            Assert.Null(activity.GetTagItem(SemanticConventions.AttributeMicrosoftOperationName));
        }

        [Fact]
        public void OnEnd_SnapshotIncludesGlobalProperties()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "prod";
            context.User.Id = "user-1";

            var processor = new TelemetryContextActivityProcessor(context);
            ForceWarmupComplete(processor);

            using var activity = this.CreateStoppedActivity();
            processor.OnEnd(activity);

            Assert.Equal("prod", activity.GetTagItem("env"));
            Assert.Equal("user-1", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void OnEnd_SnapshotRespectsSkipIfPresent()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";

            var processor = new TelemetryContextActivityProcessor(context);
            ForceWarmupComplete(processor);

            using var activity = this.CreateStoppedActivity();
            activity.SetTag(SemanticConventions.AttributeEnduserPseudoId, "item-user");
            processor.OnEnd(activity);

            Assert.Equal("item-user", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
        }

        [Fact]
        public void WarmupDoesNotFreezeBeforeTimeThreshold()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextActivityProcessor(context);

            // Run enough calls to pass the count threshold, but the time threshold
            // should prevent freezing (we just created the processor).
            for (int i = 0; i < TelemetryContextActivityProcessor.WarmupCountThreshold + 5; i++)
            {
                using var activity = this.CreateStoppedActivity();
                processor.OnEnd(activity);
            }

            // The frozenTags field should still be null because <5s has elapsed.
            var frozenTags = GetFrozenTags(processor);
            Assert.Null(frozenTags);
        }

        [Fact]
        public void WarmupFreezesAfterBothThresholdsMet()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextActivityProcessor(context);
            ForceWarmupComplete(processor);

            var frozenTags = GetFrozenTags(processor);
            Assert.NotNull(frozenTags);
            Assert.Single(frozenTags); // Only user.Id was set.
        }

        [Fact]
        public void BuildSnapshot_NoDuplicateKeysWhenGlobalPropertiesOverlapContextFields()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";
            context.GlobalProperties[SemanticConventions.AttributeEnduserPseudoId] = "global-user";
            context.GlobalProperties["extra"] = "value";

            var processor = new TelemetryContextActivityProcessor(context);
            ForceWarmupComplete(processor);

            var frozenTags = GetFrozenTags(processor);
            Assert.NotNull(frozenTags);

            var keys = new HashSet<string>();
            foreach (var kvp in frozenTags)
            {
                Assert.True(keys.Add(kvp.Key), $"Duplicate key in snapshot: '{kvp.Key}'");
            }

            // 2 entries: "enduser.pseudo.id" from GlobalProperties, "extra" from GlobalProperties.
            // User.Id should NOT add a duplicate "enduser.pseudo.id".
            Assert.Equal(2, frozenTags.Length);
        }

        #endregion

        #region Thread safety

        [Fact]
        public void OnEnd_IsThreadSafe()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";
            context.Device.Id = "device-1";

            var processor = new TelemetryContextActivityProcessor(context);
            ForceWarmupComplete(processor);

            // Run many concurrent OnEnd calls on the frozen snapshot path.
            const int threadCount = 8;
            const int iterationsPerThread = 1000;
            var exceptions = new List<Exception>();

            var barrier = new Barrier(threadCount);
            var threads = new Thread[threadCount];

            for (int t = 0; t < threadCount; t++)
            {
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (int i = 0; i < iterationsPerThread; i++)
                        {
                            using var activity = this.CreateStoppedActivity();
                            processor.OnEnd(activity);

                            Assert.Equal("user-1", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
                            Assert.Equal("device-1", activity.GetTagItem(SemanticConventions.AttributeAiDeviceId));
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                threads[t].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.Empty(exceptions);
        }

        [Fact]
        public void OnEnd_ConcurrentWarmupProducesValidSnapshot()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";
            context.Session.Id = "session-1";

            var processor = new TelemetryContextActivityProcessor(context);

            // Backdate the timestamp so the time threshold is already met.
            ForceTimestampToFarPast(processor);

            // Fire many threads concurrently to race through warmup and snapshot creation.
            const int threadCount = 16;
            var barrier = new Barrier(threadCount);
            var threads = new Thread[threadCount];
            var exceptions = new List<Exception>();

            for (int t = 0; t < threadCount; t++)
            {
                threads[t] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                        for (int i = 0; i < 20; i++)
                        {
                            using var activity = this.CreateStoppedActivity();
                            processor.OnEnd(activity);
                            Assert.Equal("user-1", activity.GetTagItem(SemanticConventions.AttributeEnduserPseudoId));
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
                threads[t].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.Empty(exceptions);

            // After all threads finished, snapshot must exist and be valid.
            var frozenTags = GetFrozenTags(processor);
            Assert.NotNull(frozenTags);
            Assert.Equal(2, frozenTags.Length);
        }

        #endregion

        #region Helpers

        private Activity CreateStoppedActivity()
        {
            var activity = this.activitySource.StartActivity("TestOp");
            Assert.NotNull(activity);
            activity.Stop();
            return activity;
        }

        /// <summary>
        /// Forces the processor past both warmup thresholds (count + time) by:
        /// 1. Backdating the constructedTimestamp so the time threshold is met.
        /// 2. Calling OnEnd enough times to trigger the count threshold.
        /// </summary>
        private void ForceWarmupComplete(TelemetryContextActivityProcessor processor)
        {
            ForceTimestampToFarPast(processor);

            for (int i = 0; i < TelemetryContextActivityProcessor.WarmupCountThreshold + 1; i++)
            {
                using var activity = this.CreateStoppedActivity();
                processor.OnEnd(activity);
            }
        }

        /// <summary>
        /// Sets the constructedTimestamp to a value far in the past so the time threshold is immediately met.
        /// </summary>
        private static void ForceTimestampToFarPast(TelemetryContextActivityProcessor processor)
        {
            var field = typeof(TelemetryContextActivityProcessor)
                .GetField("constructedTimestamp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);

            // Set to a timestamp 10 seconds in the past.
            long pastTimestamp = Stopwatch.GetTimestamp() - (Stopwatch.Frequency * 10);
            field.SetValue(processor, pastTimestamp);
        }

        /// <summary>
        /// Reads the frozenTags field via reflection for test assertions.
        /// </summary>
        private static KeyValuePair<string, string>[] GetFrozenTags(TelemetryContextActivityProcessor processor)
        {
            var field = typeof(TelemetryContextActivityProcessor)
                .GetField("frozenTags", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (KeyValuePair<string, string>[]?)field.GetValue(processor);
        }

        #endregion
    }
}
