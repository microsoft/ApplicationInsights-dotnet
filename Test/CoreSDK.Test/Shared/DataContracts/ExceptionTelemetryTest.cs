namespace Microsoft.ApplicationInsights.DataContracts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Microsoft.ApplicationInsights.Channel;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.ApplicationInsights.Extensibility.Implementation;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
    using Microsoft.ApplicationInsights.TestFramework;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using DataPlatformModel = Microsoft.Developer.Analytics.DataCollection.Model.v2;

    [TestClass]
    public class ExceptionTelemetryTest
    {
        [TestMethod]
        public void ClassIsPublicAndCanBeUsedByCustomersDirectly()
        {
            Assert.True(typeof(ExceptionTelemetry).GetTypeInfo().IsPublic);
        }

        [TestMethod]
        public void ExceptionTelemetryImplementsITelemetryContract()
        {
            var test = new ITelemetryTest<ExceptionTelemetry, DataPlatformModel.ExceptionData>();
            test.Run();
        }

        [TestMethod]
        public void ExceptionTelemetryReturnsNonNullContext()
        {
            ExceptionTelemetry item = new ExceptionTelemetry();
            Assert.NotNull(item.Context);
        }

        [TestMethod]
        public void ExceptionsPropertyIsInternalUntilWeSortOutPublicInterface()
        {
#if NET35
            Assert.False(typeof(ExceptionTelemetry).GetTypeInfo().GetDeclaredProperty("Exceptions").GetGetMethod(true).IsPublic);
#else
            Assert.False(typeof(ExceptionTelemetry).GetTypeInfo().GetDeclaredProperty("Exceptions").GetMethod.IsPublic);
#endif
        }

        [TestMethod]
        public void ConstructorAddsExceptionToExceptionPropertyAndExceptionsCollectionProperty()
        {
            Exception constructorException = new Exception("ConstructorException");
            var testExceptionTelemetry = new ExceptionTelemetry(constructorException);

            Assert.Same(constructorException, testExceptionTelemetry.Exception);
            Assert.Equal(constructorException.Message, testExceptionTelemetry.Exceptions.First().message);
        }

        [TestMethod]
        public void ExceptionPropertySetterReplacesExceptionDetailsInExceptionsCollectionProperty()
        {
            Exception constructorException = new Exception("ConstructorException");
            var testExceptionTelemetry = new ExceptionTelemetry(constructorException);

            Exception nextException = new Exception("NextException");
            testExceptionTelemetry.Exception = nextException;

            Assert.Same(nextException, testExceptionTelemetry.Exception);
            Assert.Equal(nextException.Message, testExceptionTelemetry.Exceptions.First().message);
        }

        [TestMethod]
        public void HandledAtReturnsUnhandledByDefault()
        {
            var telemetry = new ExceptionTelemetry();
            Assert.Equal(ExceptionHandledAt.Unhandled, telemetry.HandledAt);
        }

        [TestMethod]
        public void ConstructorDoesNotSetSeverityLevel()
        {
            var telemetry = new ExceptionTelemetry();
            Assert.Equal(null, telemetry.SeverityLevel);
        }

        [TestMethod]
        public void MetricsReturnsEmptyDictionaryByDefaultToPreventNullReferenceExceptions()
        {
            var @exception = new ExceptionTelemetry(new Exception());
            var measurements = @exception.Metrics;
            Assert.NotNull(measurements);
        }

        [TestMethod]
        public void SerializeWritesNullValuesAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = new ExceptionTelemetry();
            original.Exception = null;
            original.SeverityLevel = null;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
        }

        [TestMethod]
        public void SerializeWritesItemVersionAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(original);

            Assert.Equal(2, item.Data.BaseData.Ver);
        }

        [TestMethod]
        public void SerializeWritesItemHandledAtAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry();
            original.HandledAt = ExceptionHandledAt.Platform;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(original);

            Assert.Equal(ExceptionHandledAt.Platform.ToString(), item.Data.BaseData.HandledAt);
        }

        [TestMethod]
        public void SerializeWritesItemSeverityLevelAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry();
            original.SeverityLevel = SeverityLevel.Information;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(original);

            Assert.Equal(Developer.Analytics.DataCollection.Model.v2.SeverityLevel.Information, item.Data.BaseData.SeverityLevel.Value);
        }

        [TestMethod]
        public void SerializeWritesExceptionTypeNameAsExpectedByEndpoint()
        {
            var exception = new Exception();
            ExceptionTelemetry original = CreateExceptionTelemetry(exception);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(original);

            Assert.Equal(exception.GetType().FullName, item.Data.BaseData.Exceptions[0].TypeName);
        }

        [TestMethod]
        public void SerializeWritesExceptionMessageAsExpectedByEndpoint()
        {
            var exception = new Exception("Test Message");
            ExceptionTelemetry original = CreateExceptionTelemetry(exception);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(original);

            Assert.Equal(exception.Message, item.Data.BaseData.Exceptions[0].Message);
        }

        [TestMethod]
        public void GetExceptionDetailsInvokesPlatformToGetExceptionDetails()
        {
            Exception exception = null;

            Extensibility.Implementation.Platform.PlatformSingleton.Current = new StubPlatform
            {
                OnGetExceptionDetails = (e, p) =>
                {
                    exception = e;

                    return new Extensibility.Implementation.External.ExceptionDetails();
                }
            };
            try
            {
                var expectedException = new Exception();
                var expectedParentDetails = new Extensibility.Implementation.External.ExceptionDetails();

                ExceptionTelemetry original = CreateExceptionTelemetry(expectedException);

                Assert.Same(expectedException, exception);
                Assert.Same(expectedException, original.Exception);
            }
            finally
            {
                Microsoft.ApplicationInsights.Extensibility.Implementation.Platform.PlatformSingleton.Current = null;
            }
        }

        [TestMethod]
        public void SerializeWritesDataBaseTypeAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(original);
            Assert.Equal(typeof(DataPlatformModel.ExceptionData).Name, item.Data.BaseType);
        }

        [TestMethod]
        public void SerializeWritesRootExceptionWithoutOuterId()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var exception = new Exception();
                ExceptionTelemetry original = CreateExceptionTelemetry(exception);
                byte[] serializedTelemetryAsBytes = JsonSerializer.Serialize(original, compress: false);
                string serializedTelemetry = Encoding.UTF8.GetString(serializedTelemetryAsBytes, 0, serializedTelemetryAsBytes.Length);

                Assert.DoesNotContain("\"outerId\":", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void SerializeWritesInnerExceptionAsAdditionalItemInExceptionsArrayExpectedByEndpoint()
        {
            var innerException = new Exception("Inner Message");
            var exception = new Exception("Root Message", innerException);
            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

            Assert.Equal(innerException.Message, item.Data.BaseData.Exceptions[1].Message);
        }

        [TestMethod]
        public void SerializeWritesInnerExceptionWithOuterIdLinkingItToItsParentException()
        {
            var innerException = new Exception();
            var exception = new Exception("Test Exception", innerException);
            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

            Assert.Equal(exception.GetHashCode(), item.Data.BaseData.Exceptions[1].OuterId);
        }

        [TestMethod]
        public void SerializeWritesAggregateExceptionAsFirstItemInExceptionsArrayExpectedByEndpoint()
        {
            var exception = new AggregateException();
            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

            Assert.Equal(1, item.Data.BaseData.Exceptions.Count);
        }

        [TestMethod]
        public void SerializeWritesInnerExceptionsOfAggregateExceptionAsAdditionalItemsInExceptionsArrayExpectedByEndpoint()
        {
            var exception = new AggregateException("Test Exception", new[] { new Exception(), new Exception() });
            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

            Assert.Equal(exception.GetHashCode(), item.Data.BaseData.Exceptions[1].OuterId);
            Assert.Equal(exception.GetHashCode(), item.Data.BaseData.Exceptions[2].OuterId);
        }

        [TestMethod]
        public void SerializeWritesHasFullStackPropertyAsItIsExpectedByEndpoint()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var exception = CreateExceptionWithStackTrace();
                ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

                var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

                Assert.True(item.Data.BaseData.Exceptions[0].HasFullStack);
            }
        }

        [TestMethod]
        public void SerializeWritesSingleInnerExceptionOfAggregateExceptionOnlyOnce()
        {
            var exception = new AggregateException("Test Exception", new Exception());

            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

            Assert.Equal(2, item.Data.BaseData.Exceptions.Count);
        }

        [TestMethod]
        public void SerializeWritesPropertiesAsExpectedByEndpoint()
        {
            ExceptionTelemetry expected = CreateExceptionTelemetry();
            expected.Properties.Add("TestProperty", "TestValue");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

            Assert.Equal(expected.Properties.ToArray(), item.Data.BaseData.Properties.ToArray());
        }

        [TestMethod]
        public void SerializeWritesMetricsAsExpectedByEndpoint()
        {
            ExceptionTelemetry expected = CreateExceptionTelemetry();
            expected.Metrics.Add("TestMetric", 4.2);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(expected);

            Assert.Equal(expected.Metrics.ToArray(), item.Data.BaseData.Measurements.ToArray());
        }

        [TestMethod]
        public void SerializePopulatesRequiredFieldsOfExceptionTelemetry()
        {
            var exceptionTelemetry = new ExceptionTelemetry();
            exceptionTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, DataPlatformModel.ExceptionData>(exceptionTelemetry);

            Assert.Equal(2, item.Data.BaseData.Ver);
            Assert.NotNull(item.Data.BaseData.HandledAt);
            Assert.NotNull(item.Data.BaseData.Exceptions);
            Assert.Equal(0, item.Data.BaseData.Exceptions.Count); // constructor without parameters does not initialize exception object
        }

        [TestMethod]
        public void ExceptionPropertySetterHandlesAggregateExceptionsWithMultipleNestedExceptionsCorrectly()
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

            Assert.Equal(expectedSequence.Length, telemetry.Exceptions.Count);
            int counter = 0;
            foreach (ExceptionDetails details in telemetry.Exceptions)
            {
                Assert.Equal(expectedSequence[counter], details.message);
                counter++;
            }
        }

        [TestMethod]
        public void ExceptionPropertySetterHandlesAggregateExceptionsWithMultipleNestedExceptionsAndTrimsAfterReachingMaxCount()
        {
            const int Overage = 5;
            List<Exception> innerExceptions = new List<Exception>();
            for (int i = 0; i < Constants.MaxExceptionCountToSave + Overage; i++)
            {
                innerExceptions.Add(new Exception((i + 1).ToString(CultureInfo.InvariantCulture)));
            }

            AggregateException rootLevelException = new AggregateException("0", innerExceptions);

            ExceptionTelemetry telemetry = new ExceptionTelemetry { Exception = rootLevelException };

            Assert.Equal(Constants.MaxExceptionCountToSave + 1, telemetry.Exceptions.Count);
            int counter = 0;
            foreach (ExceptionDetails details in telemetry.Exceptions.Take(Constants.MaxExceptionCountToSave))
            {
                Assert.Equal(counter.ToString(CultureInfo.InvariantCulture), details.message);
                counter++;
            }

            ExceptionDetails first = telemetry.Exceptions.First();
            ExceptionDetails last = telemetry.Exceptions.Last();
            Assert.Equal(first.id, last.outerId);
            Assert.Equal(typeof(InnerExceptionCountExceededException).FullName, last.typeName);
            Assert.Equal(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The number of inner exceptions was {0} which is larger than {1}, the maximum number allowed during transmission. All but the first {1} have been dropped.",
                    1 + Constants.MaxExceptionCountToSave + Overage,
                    Constants.MaxExceptionCountToSave),
                last.message);
        }

        [TestMethod]
        public void SanitizeWillTrimPropertiesKeyAndValueInExceptionTelemetry()
        {
            ExceptionTelemetry telemetry = new ExceptionTelemetry();
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'X', new string('X', Property.MaxValueLength + 1));
            telemetry.Properties.Add(new string('X', Property.MaxDictionaryNameLength) + 'Y', new string('X', Property.MaxValueLength + 1));

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(2, telemetry.Properties.Count);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength), telemetry.Properties.Keys.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[0]);
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Properties.Keys.ToArray()[1]);
            Assert.Equal(new string('X', Property.MaxValueLength), telemetry.Properties.Values.ToArray()[1]);
        }

        [TestMethod]
        public void SanitizeWillTrimMetricsNameAndValueInExceptionTelemetry()
        {
            ExceptionTelemetry telemetry = new ExceptionTelemetry();
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'X', 42.0);
            telemetry.Metrics.Add(new string('Y', Property.MaxDictionaryNameLength) + 'Y', 42.0);

            ((ITelemetry)telemetry).Sanitize();

            Assert.Equal(2, telemetry.Metrics.Count);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength), telemetry.Metrics.Keys.ToArray()[0]);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength - 3) + "001", telemetry.Metrics.Keys.ToArray()[1]);
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
            output.Context.InstrumentationKey = "required";
            return output;
        }
    }
}
