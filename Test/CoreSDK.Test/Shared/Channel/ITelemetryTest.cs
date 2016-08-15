namespace Microsoft.ApplicationInsights.Channel
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
    using AI;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    internal class ITelemetryTest<TTelemetry, TEndpointData> 
        where TTelemetry : ITelemetry, new()
        where TEndpointData : Domain
    {
        public void Run()
        {
            this.ClassShouldBePublic();
            this.ClassShouldHaveDefaultConstructorToSupportTelemetryContext();
            this.ClassShouldHaveParameterizedConstructorToSimplifyCreationOfValidTelemetryInstancesInUserCode();
            this.ClassShouldImplementISupportCustomPropertiesIfItDefinesPropertiesProperty();
            this.TestProperties();
            this.SerializeWritesTimestampAsExpectedByEndpoint();
            this.SerializeWritesSequenceAsExpectedByEndpoint();
            this.SerializeWritesInstrumentationKeyAsExpectedByEndpoint();
            this.SerializeWritesTagsAsExpectedByEndpoint();
            this.SerializeWritesTelemetryNameAsExpectedByEndpoint();
            this.SerializeWritesDataBaseTypeAsExpectedByEndpoint();
        }

        private void TestProperties()
        {
            foreach (PropertyInfo property in typeof(TTelemetry).GetRuntimeProperties())
            {
                this.TestProperty(property);
            }
        }

        private void TestProperty(PropertyInfo property)
        {
            if (property.PropertyType == typeof(string))
            {
                this.TestStringProperty(property);
            }
            else if (property.PropertyType == typeof(int))
            {
                this.TestIntProperty(property);
            }
            else if (property.PropertyType == typeof(double))
            {
                this.TestDoubleProperty(property);
            }
            else if (property.PropertyType == typeof(DateTimeOffset))
            {
                this.TestDateTimeOffsetProperty(property);
            }
            else if (property.PropertyType == typeof(TelemetryContext))
            {
                this.TestTelemetryContextProperty(property);
            }
        }
        
        private void PropertyShouldNotBeNullByDefaultToPreventNullReferenceExceptions(PropertyInfo property)
        {
            try
            {
                var instance = new TTelemetry();
                var actualValue = property.GetValue(instance, null);
                Assert.IsNotNull(actualValue, typeof(TTelemetry).Name + "." + property.Name + " should not be null by default to prevent NullReferenceException.");
            }
            catch (TargetInvocationException e)
            {
                Assert.Fail(typeof(TTelemetry).Name + "." + property.Name + " should not be null by default to prevent NullReferenceException." + e.InnerException.Message);
            }
        }

        private void PropertySetterShouldThrowException(PropertyInfo property, object invalidValue, Type expectedException)
        {
            if (property.CanWrite)
            {
                var instance = new TTelemetry();
                try
                {
                    property.SetValue(instance, null, null);
                    Assert.Fail(typeof(TTelemetry).Name + "." + property.Name + " setter should throw " + expectedException.Name + " when value is " + (invalidValue ?? "null") + ".");
                }
                catch (TargetInvocationException e)
                {
                    Assert.AreEqual(expectedException, e.InnerException.GetType(), typeof(TTelemetry).Name + "." + property.Name + " setter should throw " + expectedException.Name + " when value is " + (invalidValue ?? "null") + ".");
                }
            }
        }

        private void PropertySetterShouldChangePropertyValue(PropertyInfo property, object value)
        {
            if (property.CanWrite)
            {
                var instance = new TTelemetry();
                property.SetValue(instance, value, null);
                Assert.AreEqual(value, property.GetValue(instance, null), typeof(TTelemetry).Name + "." + property.Name + " setter should change property value.");
            }
        }

        private void TestTelemetryContextProperty(PropertyInfo property)
        {
            this.PropertyShouldNotBeNullByDefaultToPreventNullReferenceExceptions(property);
        }

        private void TestDateTimeOffsetProperty(PropertyInfo property)
        {
            this.PropertySetterShouldChangePropertyValue(property, DateTimeOffset.Now);
        }

        private void TestDoubleProperty(PropertyInfo property)
        {
            this.PropertySetterShouldChangePropertyValue(property, 4.2);
        }

        private void TestIntProperty(PropertyInfo property)
        {
            this.PropertySetterShouldChangePropertyValue(property, 42);
        }

        private void TestStringProperty(PropertyInfo property)
        {
            this.PropertySetterShouldChangePropertyValue(property, "TestValue");
        }

        private void ClassShouldBePublic()
        {
            Assert.IsTrue(typeof(TTelemetry).GetTypeInfo().IsPublic, typeof(TTelemetry).Name + " should be public to allow instantiation in user code.");
        }

        private void ClassShouldHaveDefaultConstructorToSupportTelemetryContext()
        {
            Assert.IsNotNull(
                typeof(TTelemetry).GetTypeInfo().DeclaredConstructors.SingleOrDefault(c => c.GetParameters().Length == 0),
                typeof(TTelemetry).Name + " should have default constructor to support TelemetryContext.");
        }

        private void ClassShouldHaveParameterizedConstructorToSimplifyCreationOfValidTelemetryInstancesInUserCode()
        {
            Assert.IsTrue(
                typeof(TTelemetry).GetTypeInfo().DeclaredConstructors.Any(c => c.GetParameters().Length > 0),
                typeof(TTelemetry).Name + " should have a parameterized constructor to simplify creation of valid telemetry in user code.");
        }

        private void ClassShouldImplementISupportCustomPropertiesIfItDefinesPropertiesProperty()
        {            
            if (typeof(TTelemetry).GetRuntimeProperties().Any(p => p.Name == "Properties"))
            {
                Assert.IsTrue(typeof(ISupportProperties).GetTypeInfo().IsAssignableFrom(typeof(TTelemetry).GetTypeInfo()));
            }
        }

        private void SerializeWritesTimestampAsExpectedByEndpoint()
        {
            var expected = new TTelemetry { Timestamp = DateTimeOffset.UtcNow };
            expected.Sanitize();

            TelemetryItem<TEndpointData> actual = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<TTelemetry, TEndpointData>(expected);


            Assert.AreEqual<DateTimeOffset>(expected.Timestamp, DateTimeOffset.Parse(actual.time, null, System.Globalization.DateTimeStyles.AssumeUniversal));
        }

        private void SerializeWritesSequenceAsExpectedByEndpoint()
        {
            var expected = new TTelemetry { Sequence = "4:2" };
            expected.Sanitize();
            
            TelemetryItem<TEndpointData> actual = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<TTelemetry, TEndpointData>(expected);
            
            Assert.AreEqual(expected.Sequence, actual.seq);
        }

        private void SerializeWritesInstrumentationKeyAsExpectedByEndpoint()
        {
            var expected = new TTelemetry();
            expected.Context.InstrumentationKey = Guid.NewGuid().ToString();
            expected.Sanitize();

            TelemetryItem<TEndpointData> actual = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<TTelemetry, TEndpointData>(expected);
            
            Assert.AreEqual(expected.Context.InstrumentationKey, actual.iKey);
        }

        private void SerializeWritesDataBaseTypeAsExpectedByEndpoint()
        {
            var telemetry = new TTelemetry();
            telemetry.Sanitize();

            TelemetryItem<TEndpointData> envelope = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<TTelemetry, TEndpointData>(telemetry);

            string expectedBaseType = ExtractTelemetryNameFromType(typeof(TTelemetry)) + "Data";
            Assert.AreEqual(expectedBaseType, envelope.data.baseType);
        }

        private void SerializeWritesTagsAsExpectedByEndpoint()
        {
            var expected = new TTelemetry();
            expected.Context.Tags["Test Key"] = "Test Value";
            expected.Sanitize();

            TelemetryItem<TEndpointData> actual = TelemetryItemTestHelper.SerializeDeserializeTelemetryItem<TTelemetry, TEndpointData>(expected);
            
            Assert.AreEqual("Test Value", actual.tags["Test Key"]);
        }

        private void SerializeWritesTelemetryNameAsExpectedByEndpoint()
        {
            var expected = new TTelemetry();
            expected.Context.InstrumentationKey = "312CBD79-9DBB-4C48-A7DA-3CC2A931CB71";
            expected.Sanitize();

            TelemetryItem<TEndpointData> actual = TelemetryItemTestHelper
                .SerializeDeserializeTelemetryItem<TTelemetry, TEndpointData>(expected);

            Assert.AreEqual(
                Constants.TelemetryNamePrefix + "312cbd799dbb4c48a7da3cc2a931cb71." + this.ExtractTelemetryNameFromType(typeof(TTelemetry)),
                actual.name);
        }

        private string ExtractTelemetryNameFromType(Type telemetryType)
        {
            string result;
            if (telemetryType == typeof(TraceTelemetry))
            {
                // handle TraceTelemetry separately
                result = "Message";
            }
#pragma warning disable 618
            else if (telemetryType == typeof(SessionStateTelemetry))
            {
                // handle TraceTelemetry separately
                result = "Event";
            }
#pragma warning restore 618
            else
            {
                // common logic is to strip out "Telemetry" suffix from the telemetry type
                string typeName = telemetryType.Name;
                StringAssert.EndsWith(typeName, "Telemetry", "Unknown Telemetry object");
                result = typeName.Substring(0, typeName.LastIndexOf("Telemetry", StringComparison.Ordinal));
            }

            return result;
        }
    }
}