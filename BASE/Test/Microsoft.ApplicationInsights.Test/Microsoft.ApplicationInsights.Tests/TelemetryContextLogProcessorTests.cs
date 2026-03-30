namespace Microsoft.ApplicationInsights.Processors
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Internal;
    using OpenTelemetry.Logs;
    using Xunit;

    public class TelemetryContextLogProcessorTests
    {
        #region Basic attribute application

        [Fact]
        public void OnEnd_AppliesUserIdAttribute()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-123";
            var processor = new TelemetryContextLogProcessor(context);

            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            AssertAttributeValue("user-123", logRecord, SemanticConventions.AttributeEnduserPseudoId);
        }

        [Fact]
        public void OnEnd_AppliesAllContextAttributes()
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

            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            AssertAttributeValue("user-123", logRecord, SemanticConventions.AttributeEnduserPseudoId);
            AssertAttributeValue("auth-456", logRecord, SemanticConventions.AttributeEnduserId);
            AssertAttributeValue("acct-789", logRecord, SemanticConventions.AttributeMicrosoftUserAccountId);
            AssertAttributeValue("GET /api", logRecord, SemanticConventions.AttributeMicrosoftOperationName);
            AssertAttributeValue("bot", logRecord, SemanticConventions.AttributeMicrosoftSyntheticSource);
            AssertAttributeValue("10.0.0.1", logRecord, SemanticConventions.AttributeMicrosoftClientIp);
            AssertAttributeValue("session-1", logRecord, SemanticConventions.AttributeMicrosoftSessionId);
            AssertAttributeValue("device-1", logRecord, SemanticConventions.AttributeAiDeviceId);
            AssertAttributeValue("Surface", logRecord, SemanticConventions.AttributeAiDeviceModel);
            AssertAttributeValue("PC", logRecord, SemanticConventions.AttributeAiDeviceType);
            AssertAttributeValue("Windows 11", logRecord, SemanticConventions.AttributeAiDeviceOsVersion);
        }

        [Fact]
        public void OnEnd_AppliesGlobalProperties()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "production";
            context.GlobalProperties["region"] = "westus2";

            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            AssertAttributeValue("production", logRecord, "env");
            AssertAttributeValue("westus2", logRecord, "region");
        }

        #endregion

        #region Skip-if-present semantics

        [Fact]
        public void OnEnd_DoesNotOverwriteExistingAttribute()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";

            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecordWithAttributes(
                new KeyValuePair<string, object>(SemanticConventions.AttributeEnduserPseudoId, "item-user"));
            processor.OnEnd(logRecord);

            AssertAttributeValue("item-user", logRecord, SemanticConventions.AttributeEnduserPseudoId);
        }

        [Fact]
        public void OnEnd_DoesNotOverwriteExistingGlobalPropertyAttribute()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "production";

            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecordWithAttributes(
                new KeyValuePair<string, object>("env", "staging"));
            processor.OnEnd(logRecord);

            AssertAttributeValue("staging", logRecord, "env");
        }

        [Fact]
        public void OnEnd_PreservesExistingAttributesWhenMerging()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecordWithAttributes(
                new KeyValuePair<string, object>("custom-key", "custom-value"));
            processor.OnEnd(logRecord);

            AssertAttributeValue("custom-value", logRecord, "custom-key");
            AssertAttributeValue("user-1", logRecord, SemanticConventions.AttributeEnduserPseudoId);
        }

        #endregion

        #region Null/empty handling

        [Fact]
        public void OnEnd_SkipsNullValues()
        {
            var context = new TelemetryContext();
            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            // No attributes should be added since nothing is set.
            Assert.True(logRecord.Attributes == null || logRecord.Attributes.Count == 0);
        }

        [Fact]
        public void OnEnd_HandlesNullLogRecord()
        {
            var context = new TelemetryContext();
            var processor = new TelemetryContextLogProcessor(context);

            // Should not throw.
            processor.OnEnd(null);
        }

        [Fact]
        public void OnEnd_HandlesLogRecordWithNullAttributes()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecord();
            Assert.Null(logRecord.Attributes);

            processor.OnEnd(logRecord);

            AssertAttributeValue("user-1", logRecord, SemanticConventions.AttributeEnduserPseudoId);
        }

        #endregion

        #region Warmup and snapshot freezing

        [Fact]
        public void OnEnd_UsesSlowPathDuringWarmup()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextLogProcessor(context);

            for (int i = 0; i < TelemetryContextLogProcessor.WarmupCountThreshold - 1; i++)
            {
                var logRecord = CreateLogRecord();
                processor.OnEnd(logRecord);
                AssertAttributeValue("user-1", logRecord, SemanticConventions.AttributeEnduserPseudoId);
            }
        }

        [Fact]
        public void OnEnd_FreezesSnapshotAfterWarmup()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";
            context.Device.Id = "device-1";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            // Mutate context — frozen snapshot should retain original values.
            context.User.Id = "user-CHANGED";

            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            AssertAttributeValue("user-1", logRecord, SemanticConventions.AttributeEnduserPseudoId);
            AssertAttributeValue("device-1", logRecord, SemanticConventions.AttributeAiDeviceId);
        }

        [Fact]
        public void OnEnd_SnapshotOnlyContainsNonNullValues()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var frozenAttrs = GetFrozenAttributes(processor);
            Assert.NotNull(frozenAttrs);
            Assert.Single(frozenAttrs);
            Assert.Equal(SemanticConventions.AttributeEnduserPseudoId, frozenAttrs[0].Key);
        }

        [Fact]
        public void OnEnd_SnapshotIncludesGlobalProperties()
        {
            var context = new TelemetryContext();
            context.GlobalProperties["env"] = "prod";
            context.User.Id = "user-1";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            AssertAttributeValue("prod", logRecord, "env");
            AssertAttributeValue("user-1", logRecord, SemanticConventions.AttributeEnduserPseudoId);
        }

        [Fact]
        public void OnEnd_SnapshotRespectsSkipIfPresent()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var logRecord = CreateLogRecordWithAttributes(
                new KeyValuePair<string, object>(SemanticConventions.AttributeEnduserPseudoId, "item-user"));
            processor.OnEnd(logRecord);

            AssertAttributeValue("item-user", logRecord, SemanticConventions.AttributeEnduserPseudoId);
        }

        [Fact]
        public void WarmupDoesNotFreezeBeforeTimeThreshold()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextLogProcessor(context);

            for (int i = 0; i < TelemetryContextLogProcessor.WarmupCountThreshold + 5; i++)
            {
                var logRecord = CreateLogRecord();
                processor.OnEnd(logRecord);
            }

            var frozenAttrs = GetFrozenAttributes(processor);
            Assert.Null(frozenAttrs);
        }

        [Fact]
        public void WarmupFreezesAfterBothThresholdsMet()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var frozenAttrs = GetFrozenAttributes(processor);
            Assert.NotNull(frozenAttrs);
            Assert.Single(frozenAttrs);
        }

        #endregion

        #region Allocation reduction verification

        [Fact]
        public void OnEnd_FastPathDoesNotAllocateWhenNoAttributesToAdd()
        {
            var context = new TelemetryContext();
            // No properties set — snapshot will be empty array.
            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var frozenAttrs = GetFrozenAttributes(processor);
            Assert.NotNull(frozenAttrs);
            Assert.Empty(frozenAttrs);

            // Fast path with empty snapshot should not modify the log record.
            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            Assert.Null(logRecord.Attributes);
        }

        [Fact]
        public void OnEnd_FastPathDoesNotModifyAttributesWhenAllKeysConflict()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var originalAttrs = new List<KeyValuePair<string, object>>
            {
                new KeyValuePair<string, object>(SemanticConventions.AttributeEnduserPseudoId, "existing-user"),
            };
            var logRecord = CreateLogRecordWithAttributes(originalAttrs.ToArray());
            var attrsBefore = logRecord.Attributes;
            processor.OnEnd(logRecord);

            // Attributes should not have been replaced since no new keys were added.
            Assert.Same(attrsBefore, logRecord.Attributes);
        }

        #endregion

        #region Thread safety

        [Fact]
        public void OnEnd_IsThreadSafe()
        {
            var context = new TelemetryContext();
            context.User.Id = "user-1";
            context.Device.Id = "device-1";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

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
                            var logRecord = CreateLogRecord();
                            processor.OnEnd(logRecord);
                            AssertAttributeValue("user-1", logRecord, SemanticConventions.AttributeEnduserPseudoId);
                            AssertAttributeValue("device-1", logRecord, SemanticConventions.AttributeAiDeviceId);
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

            var processor = new TelemetryContextLogProcessor(context);
            ForceTimestampToFarPast(processor);

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
                            var logRecord = CreateLogRecord();
                            processor.OnEnd(logRecord);
                            AssertAttributeValue("user-1", logRecord, SemanticConventions.AttributeEnduserPseudoId);
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

            var frozenAttrs = GetFrozenAttributes(processor);
            Assert.NotNull(frozenAttrs);
            Assert.Equal(2, frozenAttrs.Length);
        }

        #endregion

        #region GlobalProperties overlap with context fields

        [Fact]
        public void OnEnd_SlowPath_NoDuplicateKeysWhenGlobalPropertiesOverlapContextFields()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";
            context.Location.Ip = "1.1.1.1";
            context.GlobalProperties[SemanticConventions.AttributeEnduserPseudoId] = "global-user";
            context.GlobalProperties[SemanticConventions.AttributeMicrosoftClientIp] = "2.2.2.2";

            var processor = new TelemetryContextLogProcessor(context);
            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            // GlobalProperties should win over explicit context fields (applied first).
            // There must be no duplicate keys.
            Assert.NotNull(logRecord.Attributes);
            var keys = new HashSet<string>();
            foreach (var attr in logRecord.Attributes)
            {
                Assert.True(keys.Add(attr.Key), $"Duplicate key found: '{attr.Key}'");
            }

            AssertAttributeValue("global-user", logRecord, SemanticConventions.AttributeEnduserPseudoId);
            AssertAttributeValue("2.2.2.2", logRecord, SemanticConventions.AttributeMicrosoftClientIp);
        }

        [Fact]
        public void OnEnd_FastPath_NoDuplicateKeysWhenGlobalPropertiesOverlapContextFields()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";
            context.Location.Ip = "1.1.1.1";
            context.GlobalProperties[SemanticConventions.AttributeEnduserPseudoId] = "global-user";
            context.GlobalProperties[SemanticConventions.AttributeMicrosoftClientIp] = "2.2.2.2";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var logRecord = CreateLogRecord();
            processor.OnEnd(logRecord);

            Assert.NotNull(logRecord.Attributes);
            var keys = new HashSet<string>();
            foreach (var attr in logRecord.Attributes)
            {
                Assert.True(keys.Add(attr.Key), $"Duplicate key found in snapshot: '{attr.Key}'");
            }

            // GlobalProperties win (they are added to the snapshot first).
            AssertAttributeValue("global-user", logRecord, SemanticConventions.AttributeEnduserPseudoId);
            AssertAttributeValue("2.2.2.2", logRecord, SemanticConventions.AttributeMicrosoftClientIp);
        }

        [Fact]
        public void BuildSnapshot_NoDuplicateKeysWhenGlobalPropertiesOverlapContextFields()
        {
            var context = new TelemetryContext();
            context.User.Id = "context-user";
            context.GlobalProperties[SemanticConventions.AttributeEnduserPseudoId] = "global-user";
            context.GlobalProperties["extra"] = "value";

            var processor = new TelemetryContextLogProcessor(context);
            ForceWarmupComplete(processor);

            var frozenAttrs = GetFrozenAttributes(processor);
            Assert.NotNull(frozenAttrs);

            var keys = new HashSet<string>();
            foreach (var kvp in frozenAttrs)
            {
                Assert.True(keys.Add(kvp.Key), $"Duplicate key in snapshot: '{kvp.Key}'");
            }

            // 2 entries: "enduser.pseudo.id" from GlobalProperties, "extra" from GlobalProperties.
            // User.Id should NOT add a duplicate "enduser.pseudo.id".
            Assert.Equal(2, frozenAttrs.Length);
        }

        #endregion

        #region Helpers

        private static LogRecord CreateLogRecord()
        {
            // LogRecord has no public parameterless constructor; use reflection to invoke internal/private ctor.
            return (LogRecord)Activator.CreateInstance(
                typeof(LogRecord),
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                binder: null,
                args: null,
                culture: null);
        }

        private static LogRecord CreateLogRecordWithAttributes(params KeyValuePair<string, object>[] attributes)
        {
            var logRecord = CreateLogRecord();
            logRecord.Attributes = new List<KeyValuePair<string, object>>(attributes);
            return logRecord;
        }

        private static void AssertAttributeValue(object expected, LogRecord logRecord, string key)
        {
            Assert.NotNull(logRecord.Attributes);
            foreach (var attr in logRecord.Attributes)
            {
                if (attr.Key == key)
                {
                    Assert.Equal(expected, attr.Value);
                    return;
                }
            }

            Assert.True(false, $"Attribute '{key}' not found in log record.");
        }

        private static void ForceWarmupComplete(TelemetryContextLogProcessor processor)
        {
            ForceTimestampToFarPast(processor);

            for (int i = 0; i < TelemetryContextLogProcessor.WarmupCountThreshold + 1; i++)
            {
                var logRecord = CreateLogRecord();
                processor.OnEnd(logRecord);
            }
        }

        private static void ForceTimestampToFarPast(TelemetryContextLogProcessor processor)
        {
            var field = typeof(TelemetryContextLogProcessor)
                .GetField("constructedTimestamp", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            long pastTimestamp = Stopwatch.GetTimestamp() - (Stopwatch.Frequency * 10);
            field.SetValue(processor, pastTimestamp);
        }

        private static KeyValuePair<string, object>[] GetFrozenAttributes(TelemetryContextLogProcessor processor)
        {
            var field = typeof(TelemetryContextLogProcessor)
                .GetField("frozenAttributes", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return (KeyValuePair<string, object>[])field.GetValue(processor);
        }

        #endregion
    }
}
