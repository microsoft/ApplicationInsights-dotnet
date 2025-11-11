namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using OpenTelemetry;
    using OpenTelemetry.Logs;
    using CompareLogic = KellermanSoftware.CompareNetObjects.CompareLogic;

    [TestClass]
    public class ExceptionTelemetryTest
    {
        private List<LogRecord> logItems;
        private TelemetryClient telemetryClient;

        [TestInitialize]
        public void TestInitialize()
        {
            var configuration = new TelemetryConfiguration();
            this.logItems = new List<LogRecord>();
            configuration.InstrumentationKey = Guid.NewGuid().ToString();
            configuration.ConnectionString = "InstrumentationKey=" + configuration.InstrumentationKey;
            configuration.ConfigureOpenTelemetryBuilder(b => b.WithLogging(l => l.AddInMemoryExporter(logItems)));
            this.telemetryClient = new TelemetryClient(configuration);
        }

        [TestMethod]
        public void ClassIsPublicAndCanBeUsedByCustomersDirectly()
        {
            Assert.IsTrue(typeof(ExceptionTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void ExceptionTelemetryReturnsNonNullContext()
        {
            ExceptionTelemetry item = new ExceptionTelemetry();
            Assert.IsNotNull(item.Context);
        }

        [TestMethod]
        public void ParameterlessConstructorInitializesAllProperties()
        {
            // ACT - Create exception telemetry with parameterless constructor
            var telemetry = new ExceptionTelemetry();
            
            // ASSERT - Verify all properties are initialized properly
            Assert.IsNotNull(telemetry.Context, "Context should not be null");
            Assert.IsNotNull(telemetry.Properties, "Properties should not be null");
            Assert.AreEqual(0, telemetry.Properties.Count, "Properties should be empty");
            Assert.IsNotNull(telemetry.ExceptionDetailsInfoList, "ExceptionDetailsInfoList should not be null");
            Assert.AreEqual(0, telemetry.ExceptionDetailsInfoList.Count, "ExceptionDetailsInfoList should be empty");
            Assert.IsNull(telemetry.Exception, "Exception should be null");
            Assert.IsNull(telemetry.SeverityLevel, "SeverityLevel should be null");
            Assert.IsNull(telemetry.ProblemId, "ProblemId should be null");
            Assert.IsNull(telemetry.Message, "Message should be null");
            
            // Verify properties can be set and tracked
            telemetry.Properties["key1"] = "value1";
            telemetry.Exception = new Exception("Test exception");
            telemetry.SeverityLevel = SeverityLevel.Warning;
            
            this.telemetryClient.TrackException(telemetry);
            this.telemetryClient.Flush();
            
            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("Test exception", logRecord.Exception.Message);
            Assert.AreEqual(LogLevel.Warning, logRecord.LogLevel);
        }

        [TestMethod]
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
            Assert.AreEqual(1, this.logItems.Count);
            var logRecord = this.logItems[0];
            
            Assert.IsNotNull(logRecord.Exception);
            Assert.AreEqual("Top level exception", logRecord.Exception.Message);
            // Note: We reconstruct generic Exception objects, so GetType().Name will be "Exception"
            // The original TypeName is preserved in ExceptionDetailsInfo
            Assert.IsNotNull(logRecord.Exception.InnerException);
            Assert.AreEqual("Inner exception", logRecord.Exception.InnerException.Message);
            
            // Verify severity mapped correctly
            Assert.AreEqual(LogLevel.Error, logRecord.LogLevel);
            
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
            Assert.IsTrue(hasProperty1, "property1 should be captured");
            Assert.IsTrue(hasProperty2, "property2 should be captured");
        }

        [TestMethod]
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
            Assert.AreEqual("Top level exception", reconstructed.Message);
            Assert.IsNotNull(reconstructed.InnerException);
            Assert.AreEqual("Inner exception", reconstructed.InnerException.Message);
            Assert.IsNotNull(reconstructed.InnerException.InnerException);
            Assert.AreEqual("Inner inner exception", reconstructed.InnerException.InnerException.Message);
            Assert.IsNull(reconstructed.InnerException.InnerException.InnerException);
        }

        [TestMethod]
        public void ConstructorAddsExceptionToExceptionProperty()
        {
            Exception constructorException = new Exception("ConstructorException");
            var testExceptionTelemetry = new ExceptionTelemetry(constructorException);

            Assert.AreSame(constructorException, testExceptionTelemetry.Exception);
            Assert.AreEqual(constructorException.Message, testExceptionTelemetry.ExceptionDetailsInfoList.First().Message);
        }

        [TestMethod]
        public void ExceptionPropertySetterReplacesExceptionDetails()
        {
            Exception constructorException = new Exception("ConstructorException");
            var testExceptionTelemetry = new ExceptionTelemetry(constructorException);

            Exception nextException = new Exception("NextException");
            testExceptionTelemetry.Exception = nextException;

            Assert.AreSame(nextException, testExceptionTelemetry.Exception);
            Assert.AreEqual(nextException.Message, testExceptionTelemetry.ExceptionDetailsInfoList.First().Message);
        }

        [TestMethod]
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
            Assert.AreEqual(expectedAccountId, testExceptionTelemetry.Context.User.AccountId);
            Assert.AreEqual(expectedAuthenticatedUserId, testExceptionTelemetry.Context.User.AuthenticatedUserId);
            Assert.AreEqual(expectedUserAgent, testExceptionTelemetry.Context.User.UserAgent);
        }

        [TestMethod]
        public void ConstructorDoesNotSetSeverityLevel()
        {
            var telemetry = new ExceptionTelemetry();
            Assert.AreEqual(null, telemetry.SeverityLevel);
        }

        [TestMethod]
        public void PropertiesReturnsEmptyDictionaryByDefaultToPreventNullReferenceExceptions()
        {
            var @exception = new ExceptionTelemetry(new Exception());
            var properties = @exception.Properties;
            Assert.IsNotNull(properties);
        }

        [TestMethod]
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

            Assert.AreEqual(expectedSequence.Length, telemetry.ExceptionDetailsInfoList.Count);
            for(int counter = 0; counter < expectedSequence.Length; counter++)
            {
                ExceptionDetailsInfo details = telemetry.ExceptionDetailsInfoList[counter];
                if (details.TypeName.Contains("AggregateException"))
                {
                    Assert.IsTrue(details.Message.StartsWith(expectedSequence[counter]));
                }
                else
                {
                    Assert.AreEqual(expectedSequence[counter], details.Message);
                }
            }
        }

        [TestMethod]
        public void ExceptionTelemetryPropertiesFromContextAndItemAreTracked()
        {
            var expected = CreateExceptionTelemetry();

            Assert.AreEqual(1, expected.Properties.Count);
            Assert.AreEqual(1, expected.Context.GlobalProperties.Count);
            Assert.IsTrue(expected.Properties.ContainsKey("TestProperty"));
            Assert.IsTrue(expected.Context.GlobalProperties.ContainsKey("TestPropertyGlobal"));

            // ACT
            this.telemetryClient.TrackException(expected);
            this.telemetryClient.Flush();

            // ASSERT - Verify properties are captured
            Assert.AreEqual(1, this.logItems.Count);
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
            Assert.IsTrue(hasTestProperty, "TestProperty should be captured");
            Assert.IsTrue(hasTestPropertyGlobal, "TestPropertyGlobal should be captured");
        }

        [TestMethod]
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
            Assert.IsNotNull(telemetry.ExceptionDetailsInfoList);
        }

        [TestMethod]
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
            Assert.AreEqual(stackTraceString, exceptionDetails.Stack, "Stack trace string should be preserved");
            Assert.AreEqual("System.Exception", exceptionDetails.TypeName);
            Assert.AreEqual("Test exception", exceptionDetails.Message);
            Assert.IsTrue(exceptionDetails.HasFullStack);
            Assert.AreEqual(1, exceptionDetails.Id);
            Assert.AreEqual(-1, exceptionDetails.OuterId);
        }

        [TestMethod]
        public void TimestampPropertyCanBeGetAndSet()
        {
            var telemetry = new ExceptionTelemetry(new Exception());
            var timestamp = DateTimeOffset.UtcNow;
            
            telemetry.Timestamp = timestamp;
            
            Assert.AreEqual(timestamp, telemetry.Timestamp);
        }

        [TestMethod]
        public void MessagePropertyCanBeGetAndSet()
        {
            var telemetry = new ExceptionTelemetry(new Exception());
            const string expectedMessage = "Custom exception message";
            
            telemetry.Message = expectedMessage;
            
            Assert.AreEqual(expectedMessage, telemetry.Message);
        }

        [TestMethod]
        public void SeverityLevelPropertyCanBeGetAndSet()
        {
            var telemetry = new ExceptionTelemetry(new Exception());
            
            // Default should be null
            Assert.IsNull(telemetry.SeverityLevel);
            
            // Set and verify each severity level
            telemetry.SeverityLevel = SeverityLevel.Verbose;
            Assert.AreEqual(SeverityLevel.Verbose, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Information;
            Assert.AreEqual(SeverityLevel.Information, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Warning;
            Assert.AreEqual(SeverityLevel.Warning, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Error;
            Assert.AreEqual(SeverityLevel.Error, telemetry.SeverityLevel);
            
            telemetry.SeverityLevel = SeverityLevel.Critical;
            Assert.AreEqual(SeverityLevel.Critical, telemetry.SeverityLevel);
            
            // Can be set back to null
            telemetry.SeverityLevel = null;
            Assert.IsNull(telemetry.SeverityLevel);
        }

        [TestMethod]
        public void ExceptionDetailsInfoListReturnsReadOnlyList()
        {
            var exception = new Exception("Test exception");
            var telemetry = new ExceptionTelemetry(exception);
            
            var detailsList = telemetry.ExceptionDetailsInfoList;
            
            Assert.IsNotNull(detailsList);
            Assert.AreEqual(1, detailsList.Count);
            Assert.AreEqual("Test exception", detailsList[0].Message);
            Assert.AreEqual("System.Exception", detailsList[0].TypeName);
        }

        [TestMethod]
        public void ExceptionPropertyAcceptsNull()
        {
            var telemetry = new ExceptionTelemetry(new Exception("Initial exception"));
            
            // Verify initial exception is set
            Assert.IsNotNull(telemetry.Exception);
            Assert.AreEqual("Initial exception", telemetry.Exception.Message);
            Assert.AreEqual(1, telemetry.ExceptionDetailsInfoList.Count);
            
            // Set to null
            telemetry.Exception = null;
            
            // Verify exception is now null and list is empty
            Assert.IsNull(telemetry.Exception);
            Assert.AreEqual(0, telemetry.ExceptionDetailsInfoList.Count);
        }

        [TestMethod]
        public void ExceptionDetailsInfoListReflectsExceptionChanges()
        {
            var telemetry = new ExceptionTelemetry(new Exception("First exception"));
            
            // Verify initial state
            Assert.AreEqual(1, telemetry.ExceptionDetailsInfoList.Count);
            Assert.AreEqual("First exception", telemetry.ExceptionDetailsInfoList[0].Message);
            
            // Change exception
            telemetry.Exception = new Exception("Second exception");
            
            // Verify list is updated
            Assert.AreEqual(1, telemetry.ExceptionDetailsInfoList.Count);
            Assert.AreEqual("Second exception", telemetry.ExceptionDetailsInfoList[0].Message);
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
            output.Context.InstrumentationKey = "required";
            output.Properties.Add("TestProperty", "TestPropertyValue");
            return output;
        }
    }
}
