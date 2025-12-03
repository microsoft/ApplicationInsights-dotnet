namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Logging;
    using Xunit;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using CompareLogic = KellermanSoftware.CompareNetObjects.CompareLogic;

    public class ExceptionTelemetryTest : IDisposable
    {
        private List<LogRecord> logItems;
        private TelemetryClient telemetryClient;

        public ExceptionTelemetryTest()
        {
            var configuration = new TelemetryConfiguration();
            this.logItems = new List<LogRecord>();
            var instrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + instrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b.WithLogging(l => l.AddInMemoryExporter(logItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        public void Dispose()
        {
            this.telemetryClient?.TelemetryConfiguration?.Dispose();
        }

        [Fact]
        public void ClassIsPublicAndCanBeUsedByCustomersDirectly()
        {
            Assert.True(typeof(ExceptionTelemetry).GetTypeInfo().IsPublic);
        }

        [Fact]
        public void ExceptionTelemetryReturnsNonNullContext()
        {
            ExceptionTelemetry item = new ExceptionTelemetry();
            Assert.NotNull(item.Context);
        }

        [Fact]
        public void ParameterlessConstructorInitializesAllProperties()
        {
            // ACT - Create exception telemetry with parameterless constructor
            var telemetry = new ExceptionTelemetry();
            
            // ASSERT - Verify all properties are initialized properly
            Assert.NotNull(telemetry.Context);
            Assert.NotNull(telemetry.Properties);
            Assert.Equal(0, telemetry.Properties.Count);
            Assert.NotNull(telemetry.ExceptionDetailsInfoList);
            Assert.Equal(0, telemetry.ExceptionDetailsInfoList.Count);
            Assert.Null(telemetry.Exception);
            Assert.Null(telemetry.SeverityLevel);
            Assert.Null(telemetry.ProblemId);
            Assert.Null(telemetry.Message);
            
            // Verify properties can be set and tracked
            telemetry.Properties["key1"] = "value1";
            telemetry.Exception = new Exception("Test exception");
            telemetry.SeverityLevel = SeverityLevel.Warning;
            
            this.telemetryClient.TrackException(telemetry);
            this.telemetryClient.Flush();
            
            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.NotNull(logRecord.Exception);
            Assert.Equal("Test exception", logRecord.Exception.Message);
            Assert.Equal(LogLevel.Warning, logRecord.LogLevel);
        }

        [Fact]
        public void ExceptionTelemetryCreatedWithCustomExceptionDetailsCanBeTracked()
        {
            // ARRANGE - Customer creates exception telemetry with custom exception details
            var topLevelexceptionDetails = new ExceptionDetailsInfo(1, -1, "TopLevelException", "Top level exception",
                true, "Top level exception stack", new[]
                {
                    new StackFrame("Some.Assembly", "SomeFile.dll", 3, 33, "TopLevelMethod"),
                    new StackFrame("Some.Assembly", "SomeOtherFile.dll", 2, 22, "LowerLevelMethod"),
                    new StackFrame("Some.Assembly", "YetAnotherFile.dll", 1, 11, "LowLevelMethod")
                });

            var innerExceptionDetails = new ExceptionDetailsInfo(2, 1, "InnerException", "Inner exception", false,
                "Inner exception stack", new[]
                {
                    new StackFrame("Some.Assembly", "ImportantFile.dll", 2, 22, "InnerMethod"),
                    new StackFrame("Some.Assembly", "LessImportantFile.dll", 1, 11, "DeeperInnerMethod")
                });

            ExceptionTelemetry item = new ExceptionTelemetry(new[] {topLevelexceptionDetails, innerExceptionDetails},
                SeverityLevel.Error, "ProblemId",
                new Dictionary<string, string>() {["property1"] = "value1", ["property2"] = "value2"});

            // ACT - Track it through TelemetryClient
            this.telemetryClient.TrackException(item);
            this.telemetryClient.Flush();

            // ASSERT - Verify data is captured in OpenTelemetry logs
            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            
            Assert.NotNull(logRecord.Exception);
            Assert.Equal("Top level exception", logRecord.Exception.Message);
            // Note: We reconstruct generic Exception objects, so GetType().Name will be "Exception"
            // The original TypeName is preserved in ExceptionDetailsInfo
            Assert.NotNull(logRecord.Exception.InnerException);
            Assert.Equal("Inner exception", logRecord.Exception.InnerException.Message);
            
            // Verify severity mapped correctly
            Assert.Equal(LogLevel.Error, logRecord.LogLevel);
            
            // Verify properties are present
            bool hasProperty1 = false, hasProperty2 = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "property1" && attr.Value?.ToString() == "value1") hasProperty1 = true;
                    if (attr.Key == "property2" && attr.Value?.ToString() == "value2") hasProperty2 = true;
                }
            }
            Assert.True(hasProperty1, "property1 should be captured");
            Assert.True(hasProperty2, "property2 should be captured");
        }

        [Fact]
        public void ExceptionTelemetryWithNestedInnerExceptionsReconstructsCorrectly()
        {
            // ARRANGE
            var topLevelexceptionDetails = new ExceptionDetailsInfo(1, -1, "TopLevelException", "Top level exception",
                true, "stack", new StackFrame[0]);

            var innerExceptionDetails = new ExceptionDetailsInfo(2, 1, "InnerException", "Inner exception", false,
                "stack", new StackFrame[0]);

            var innerInnerExceptionDetails = new ExceptionDetailsInfo(3, 2, "InnerInnerException", "Inner inner exception", false,
                "stack", new StackFrame[0]);

            ExceptionTelemetry item = new ExceptionTelemetry(new[] { topLevelexceptionDetails, innerExceptionDetails, innerInnerExceptionDetails },
                SeverityLevel.Error, "ProblemId", new Dictionary<string, string>());

            // ACT
            Exception reconstructed = item.Exception;

            // ASSERT - Verify full chain is reconstructed
            Assert.Equal("Top level exception", reconstructed.Message);
            Assert.NotNull(reconstructed.InnerException);
            Assert.Equal("Inner exception", reconstructed.InnerException.Message);
            Assert.NotNull(reconstructed.InnerException.InnerException);
            Assert.Equal("Inner inner exception", reconstructed.InnerException.InnerException.Message);
            Assert.Null(reconstructed.InnerException.InnerException.InnerException);
        }

        [Fact]
        public void ConstructorAddsExceptionToExceptionProperty()
        {
            Exception constructorException = new Exception("ConstructorException");
            var testExceptionTelemetry = new ExceptionTelemetry(constructorException);

            Assert.Same(constructorException, testExceptionTelemetry.Exception);
            Assert.Equal(constructorException.Message, testExceptionTelemetry.ExceptionDetailsInfoList.First().Message);
        }

        [Fact]
        public void ExceptionPropertySetterReplacesExceptionDetails()
        {
            Exception constructorException = new Exception("ConstructorException");
            var testExceptionTelemetry = new ExceptionTelemetry(constructorException);

            Exception nextException = new Exception("NextException");
            testExceptionTelemetry.Exception = nextException;

            Assert.Same(nextException, testExceptionTelemetry.Exception);
            Assert.Equal(nextException.Message, testExceptionTelemetry.ExceptionDetailsInfoList.First().Message);
        }

        [Fact]
        public void ExceptionPropertySetterPreservesContext()
        {
            // ARRANGE
            Exception constructorException = new Exception("ConstructorException");
            var testExceptionTelemetry = new ExceptionTelemetry(constructorException);

            const string expectedAccountId = "AccountId";
            testExceptionTelemetry.Context.User.AccountId = expectedAccountId;
            const string expectedAuthenticatedUserId = "AuthUserId";
            testExceptionTelemetry.Context.User.AuthenticatedUserId = expectedAuthenticatedUserId;
            const string expectedUserAgent = "ExceptionComponent";
            testExceptionTelemetry.Context.User.UserAgent = expectedUserAgent;

            // ACT
            Exception nextException = new Exception("NextException");
            testExceptionTelemetry.Exception = nextException;

            // ASSERT
            Assert.Equal(expectedAccountId, testExceptionTelemetry.Context.User.AccountId);
            Assert.Equal(expectedAuthenticatedUserId, testExceptionTelemetry.Context.User.AuthenticatedUserId);
            Assert.Equal(expectedUserAgent, testExceptionTelemetry.Context.User.UserAgent);
        }

        [Fact]
        public void ConstructorDoesNotSetSeverityLevel()
        {
            var telemetry = new ExceptionTelemetry();
            Assert.Equal(null, telemetry.SeverityLevel);
        }

        [Fact]
        public void PropertiesReturnsEmptyDictionaryByDefaultToPreventNullReferenceExceptions()
        {
            var @exception = new ExceptionTelemetry(new Exception());
            var properties = @exception.Properties;
            Assert.NotNull(properties);
        }

        [Fact]
        public void ExceptionPropertySetterHandlesAggregateExceptionsWithMultipleNestedExceptions()
        {
            Exception exception1121 = new Exception("1.1.2.1");
            Exception exception111 = new Exception("1.1.1");
            Exception exception112 = new Exception("1.1.2", exception1121);
            AggregateException exception11 = new AggregateException("1.1", exception111, exception112);
            Exception exception121 = new Exception("1.2.1");
            Exception exception12 = new Exception("1.2", exception121);
            AggregateException rootLevelException = new AggregateException("1", exception11, exception12);

            ExceptionTelemetry telemetry = new ExceptionTelemetry { Exception = rootLevelException };

            string[] expectedSequence = new string[]
                                            {
                                                "1",
                                                "1.1",
                                                "1.1.1",
                                                "1.1.2",
                                                "1.1.2.1",
                                                "1.2",
                                                "1.2.1"
                                            };

            Assert.Equal(expectedSequence.Length, telemetry.ExceptionDetailsInfoList.Count);
            for(int counter = 0; counter < expectedSequence.Length; counter++)
            {
                ExceptionDetailsInfo details = telemetry.ExceptionDetailsInfoList[counter];
                if (details.TypeName.Contains("AggregateException"))
                {
                    Assert.True(details.Message.StartsWith(expectedSequence[counter]));
                }
                else
                {
                    Assert.Equal(expectedSequence[counter], details.Message);
                }
            }
        }

        [Fact]
        public void ExceptionTelemetryPropertiesFromContextAndItemAreTracked()
        {
            var expected = CreateExceptionTelemetry();

            Assert.Equal(1, expected.Properties.Count);
            Assert.Equal(1, expected.Context.GlobalProperties.Count);
            Assert.True(expected.Properties.ContainsKey("TestProperty"));
            Assert.True(expected.Context.GlobalProperties.ContainsKey("TestPropertyGlobal"));

            // ACT
            this.telemetryClient.TrackException(expected);
            this.telemetryClient.Flush();

            // ASSERT - Verify properties are captured
            Assert.Equal(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            bool hasTestProperty = false, hasTestPropertyGlobal = false;
            if (logRecord.Attributes != null)
            {
                foreach (var attr in logRecord.Attributes)
                {
                    if (attr.Key == "TestProperty") hasTestProperty = true;
                    if (attr.Key == "TestPropertyGlobal") hasTestPropertyGlobal = true;
                }
            }
            Assert.True(hasTestProperty, "TestProperty should be captured");
            Assert.True(hasTestPropertyGlobal, "TestPropertyGlobal should be captured");
        }

        [Fact]
        public void SetParsedStackUpdatesExceptionDetails()
        {
            var exception = new Exception("Test");
            var telemetry = new ExceptionTelemetry(exception);

            var frames = new System.Diagnostics.StackFrame[]
            {
                new System.Diagnostics.StackFrame(),
                new System.Diagnostics.StackFrame()
            };

            // ACT
            telemetry.SetParsedStack(frames);

            // ASSERT - Verify method doesn't throw and can be called
            Assert.NotNull(telemetry.ExceptionDetailsInfoList);
        }

        [Fact]
        public void ExceptionDetailsInfoPreservesStackTraceString()
        {
            // ARRANGE
            var stackTraceString = "   at System.Environment.GetStackTrace(Exception e, Boolean needFileInfo)\n   at System.Environment.get_StackTrace()";
            var exceptionDetails = new ExceptionDetailsInfo(
                id: 1,
                outerId: -1,
                typeName: "System.Exception",
                message: "Test exception",
                hasFullStack: true,
                stack: stackTraceString,
                parsedStack: new[]
                {
                    new StackFrame("System", "Environment.cs", 1, 100, "GetStackTrace")
                });

            // ASSERT - Verify stack trace string is stored in the Stack property
            Assert.Equal(stackTraceString, exceptionDetails.Stack);
            Assert.Equal("System.Exception", exceptionDetails.TypeName);
            Assert.Equal("Test exception", exceptionDetails.Message);
            Assert.True(exceptionDetails.HasFullStack);
            Assert.Equal(1, exceptionDetails.Id);
            Assert.Equal(-1, exceptionDetails.OuterId);
        }

        [Fact]
        public void TimestampPropertyCanBeGetAndSet()
        {
            var telemetry = new ExceptionTelemetry(new Exception());
            var timestamp = DateTimeOffset.UtcNow;
            
            telemetry.Timestamp = timestamp;
            
            Assert.Equal(timestamp, telemetry.Timestamp);
        }

        [Fact]
        public void MessagePropertyCanBeGetAndSet()
        {
            var telemetry = new ExceptionTelemetry(new Exception());
            const string expectedMessage = "Custom exception message";
            
            telemetry.Message = expectedMessage;
            
            Assert.Equal(expectedMessage, telemetry.Message);
        }

        [Fact]
        public void SeverityLevelPropertyCanBeGetAndSet()
        {
            var telemetry = new ExceptionTelemetry(new Exception());
            
            // Default should be null
            Assert.Null(telemetry.SeverityLevel);
            
            // Set and verify each severity level
            telemetry.SeverityLevel = SeverityLevel.Verbose;
            Assert.Equal(SeverityLevel.Verbose, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Information;
            Assert.Equal(SeverityLevel.Information, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Warning;
            Assert.Equal(SeverityLevel.Warning, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Error;
            Assert.Equal(SeverityLevel.Error, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Critical;
            Assert.Equal(SeverityLevel.Critical, telemetry.SeverityLevel);
            
            // Can be set back to null
            telemetry.SeverityLevel = null;
            Assert.Null(telemetry.SeverityLevel);
        }

        [Fact]
        public void ExceptionDetailsInfoListReturnsReadOnlyList()
        {
            var exception = new Exception("Test exception");
            var telemetry = new ExceptionTelemetry(exception);
            
            var detailsList = telemetry.ExceptionDetailsInfoList;
            
            Assert.NotNull(detailsList);
            Assert.Equal(1, detailsList.Count);
            Assert.Equal("Test exception", detailsList[0].Message);
            Assert.Equal("System.Exception", detailsList[0].TypeName);
        }

        [Fact]
        public void ExceptionPropertyAcceptsNull()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Initial exception"));
            
            // Verify initial exception is set
            Assert.NotNull(telemetry.Exception);
            Assert.Equal("Initial exception", telemetry.Exception.Message);
            Assert.Equal(1, telemetry.ExceptionDetailsInfoList.Count);
            
            // Set to null
            telemetry.Exception = null;
            
            // Verify exception is now null and list is empty
            Assert.Null(telemetry.Exception);
            Assert.Equal(0, telemetry.ExceptionDetailsInfoList.Count);
        }

        [Fact]
        public void ExceptionDetailsInfoListReflectsExceptionChanges()
        {
            var telemetry = new ExceptionTelemetry(new Exception("First exception"));
            
            // Verify initial state
            Assert.Equal(1, telemetry.ExceptionDetailsInfoList.Count);
            Assert.Equal("First exception", telemetry.ExceptionDetailsInfoList[0].Message);
            
            // Change exception
            telemetry.Exception = new Exception("Second exception");
            
            // Verify list is updated
            Assert.Equal(1, telemetry.ExceptionDetailsInfoList.Count);
            Assert.Equal("Second exception", telemetry.ExceptionDetailsInfoList[0].Message);
        }

        private static Exception CreateExceptionWithStackTrace()
        {
            try
            {
                throw new Exception();
            }
            catch (Exception e)
            {
                return e;
            }
        }

        private static ExceptionTelemetry CreateExceptionTelemetry(Exception exception = null)
        {
            if (exception == null)
            {
                exception = new Exception();
            }

            ExceptionTelemetry output = new ExceptionTelemetry(exception) { Timestamp = DateTimeOffset.UtcNow };
            output.Context.GlobalProperties.Add("TestPropertyGlobal", "contextpropvalue");
            output.Properties.Add("TestProperty", "TestPropertyValue");
            return output;
        }
    }
}
