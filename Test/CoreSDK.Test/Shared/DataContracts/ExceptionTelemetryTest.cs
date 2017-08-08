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
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Assert = Xunit.Assert;
#if !NETCOREAPP1_1
    using CompareLogic = KellermanSoftware.CompareNetObjects.CompareLogic;
#endif

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
            var test = new ITelemetryTest<ExceptionTelemetry, AI.ExceptionData>();
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
            Assert.False(typeof(ExceptionTelemetry).GetTypeInfo().GetDeclaredProperty("Exceptions").GetGetMethod(true).IsPublic);
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

#pragma warning disable 618
        [TestMethod]
        public void HandledAtReturnsUnhandledByDefault()
        {
            var telemetry = new ExceptionTelemetry();
            Assert.Equal(ExceptionHandledAt.Unhandled, telemetry.HandledAt);
        }
#pragma warning restore 618

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
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void SerializeWritesItemVersionAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal(2, item.data.baseData.ver);
        }

        [TestMethod]
        public void SerializeUsesExceptionMessageIfTelemetryMessageNotProvided()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry(new ArgumentException("Test"));
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal("Test", item.data.baseData.exceptions[0].message);
        }

        [TestMethod]
        public void SerializeTelemetryMessageAsOuterExceptionMessage()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry(new ArgumentException("Test"));
            original.Message = "Custom";
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal("Custom", item.data.baseData.exceptions[0].message);
        }

        [TestMethod]
        public void SerializeUsesExceptionMessageForInnerExceptions()
        {
            Exception outerException = new ArgumentException("Outer", new Exception("Inner"));
            ExceptionTelemetry original = CreateExceptionTelemetry(outerException);

            original.Message = "Custom";
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal("Custom", item.data.baseData.exceptions[0].message);
            Assert.Equal("Inner", item.data.baseData.exceptions[1].message);
        }

        [TestMethod]
        public void SerializeUsesExceptionMessageForInnerAggregateExceptions()
        {
            Exception innerException1 = new ArgumentException("Inner1");
            Exception innerException2 = new ArgumentException("Inner2");

            AggregateException aggregateException = new AggregateException("AggregateException", new [] {innerException1, innerException2});

            ExceptionTelemetry original = CreateExceptionTelemetry(aggregateException);

            original.Message = "Custom";
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal("Custom", item.data.baseData.exceptions[0].message);
            Assert.Equal("Inner1", item.data.baseData.exceptions[1].message);
            Assert.Equal("Inner2", item.data.baseData.exceptions[2].message);
        }

        [TestMethod]
        public void SerializeWritesItemSeverityLevelAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry();
            original.SeverityLevel = SeverityLevel.Information;
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal(AI.SeverityLevel.Information, item.data.baseData.severityLevel.Value);
        }

        [TestMethod]
        public void SerializeWritesExceptionTypeNameAsExpectedByEndpoint()
        {
            var exception = new Exception();
            ExceptionTelemetry original = CreateExceptionTelemetry(exception);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal(exception.GetType().FullName, item.data.baseData.exceptions[0].typeName);
        }

        [TestMethod]
        public void SerializeWritesExceptionMessageAsExpectedByEndpoint()
        {
            var exception = new Exception("Test Message");
            ExceptionTelemetry original = CreateExceptionTelemetry(exception);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);

            Assert.Equal(exception.Message, item.data.baseData.exceptions[0].message);
        }

        [TestMethod]
        public void SerializeWritesDataBaseTypeAsExpectedByEndpoint()
        {
            ExceptionTelemetry original = CreateExceptionTelemetry();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(original);
            Assert.Equal(typeof(AI.ExceptionData).Name, item.data.baseType);
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

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

            Assert.Equal(innerException.Message, item.data.baseData.exceptions[1].message);
        }

        [TestMethod]
        public void SerializeWritesInnerExceptionWithOuterIdLinkingItToItsParentException()
        {
            var innerException = new Exception();
            var exception = new Exception("Test Exception", innerException);
            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

            Assert.Equal(exception.GetHashCode(), item.data.baseData.exceptions[1].outerId);
        }

        [TestMethod]
        public void SerializeWritesAggregateExceptionAsFirstItemInExceptionsArrayExpectedByEndpoint()
        {
            var exception = new AggregateException();
            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

            Assert.Equal(1, item.data.baseData.exceptions.Count);
        }

        [TestMethod]
        public void SerializeWritesInnerExceptionsOfAggregateExceptionAsAdditionalItemsInExceptionsArrayExpectedByEndpoint()
        {
            var exception = new AggregateException("Test Exception", new[] { new Exception(), new Exception() });
            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

            Assert.Equal(exception.GetHashCode(), item.data.baseData.exceptions[1].outerId);
            Assert.Equal(exception.GetHashCode(), item.data.baseData.exceptions[2].outerId);
        }

        [TestMethod]
        public void SerializeWritesHasFullStackPropertyAsItIsExpectedByEndpoint()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var exception = CreateExceptionWithStackTrace();
                ExceptionTelemetry expected = CreateExceptionTelemetry(exception);

                var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

                Assert.True(item.data.baseData.exceptions[0].hasFullStack);
            }
        }

        [TestMethod]
        public void SerializeWritesSingleInnerExceptionOfAggregateExceptionOnlyOnce()
        {
            var exception = new AggregateException("Test Exception", new Exception());

            ExceptionTelemetry expected = CreateExceptionTelemetry(exception);
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

            Assert.Equal(2, item.data.baseData.exceptions.Count);
        }

        [TestMethod]
        public void SerializeWritesPropertiesAsExpectedByEndpoint()
        {
            ExceptionTelemetry expected = CreateExceptionTelemetry();
            expected.Properties.Add("TestProperty", "TestValue");

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

            Assert.Equal(expected.Properties.ToArray(), item.data.baseData.properties.ToArray());
        }

        [TestMethod]
        public void SerializeWritesMetricsAsExpectedByEndpoint()
        {
            ExceptionTelemetry expected = CreateExceptionTelemetry();
            expected.Metrics.Add("TestMetric", 4.2);

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(expected);

            Assert.Equal(expected.Metrics.ToArray(), item.data.baseData.measurements.ToArray());
        }

        [TestMethod]
        public void SerializePopulatesRequiredFieldsOfExceptionTelemetry()
        {
            var exceptionTelemetry = new ExceptionTelemetry();
            exceptionTelemetry.Context.InstrumentationKey = Guid.NewGuid().ToString();
            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(exceptionTelemetry);

            Assert.Equal(2, item.data.baseData.ver);
            Assert.NotNull(item.data.baseData.exceptions);
            Assert.Equal(0, item.data.baseData.exceptions.Count); // constructor without parameters does not initialize exception object
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
            Assert.Equal(new string('X', Property.MaxDictionaryNameLength - 3) + "1", telemetry.Properties.Keys.ToArray()[1]);
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
            string[] keys = telemetry.Metrics.Keys.OrderBy(s => s).ToArray();
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength), keys[1]);
            Assert.Equal(new string('Y', Property.MaxDictionaryNameLength - 3) + "1", keys[0]);
        }

        [TestMethod]
        public void ExceptionTelemetryImplementsISupportSamplingContract()
        {
            var telemetry = new ExceptionTelemetry();

            Assert.NotNull(telemetry as ISupportSampling);
        }

        [TestMethod]
        public void ExceptionTelemetryHasCorrectValueOfSamplingPercentageAfterSerialization()
        {
            var telemetry = new ExceptionTelemetry();
            ((ISupportSampling)telemetry).SamplingPercentage = 10;

            var item = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<ExceptionTelemetry, AI.ExceptionData>(telemetry);

            Assert.Equal(10, item.sampleRate);
        }

#if !NETCOREAPP1_1
        [TestMethod]
        public void ExceptionTelemetryDeepCloneCopiesAllProperties()
        {
            var telemetry = CreateExceptionTelemetry(CreateExceptionWithStackTrace());
            var other = telemetry.DeepClone();

            CompareLogic deepComparator = new CompareLogic();

            var result = deepComparator.Compare(telemetry, other);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
#endif

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
