namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using Microsoft.ApplicationInsights.DataContracts;
#if WINDOWS_PHONE || WINDOWS_STORE
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#else
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
    using Assert = Xunit.Assert;
    using AssertEx = Xunit.AssertEx;

    [TestClass]
    public class JsonWriterTest
    {
        [TestMethod]
        public void ClassIsInternalAndNotMeantToBeAccessedByCustomers()
        {
            Assert.False(typeof(JsonWriter).GetTypeInfo().IsPublic);
        }

        #region IsNullOrEmpty

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesRawObjectValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteRawValue(@"name\") };
            Assert.False(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesRawObjectValueWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteRawValue(null) };
            Assert.True(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenGivenIJsonSerializableInstanceIsNull()
        {
            var writer = new TestableJsonWriter(null);
            Assert.True(writer.IsNullOrEmpty(null));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenGivenIJsonSerializableInstanceDoesNotWriteAnyProperties()
        {
            var writer = new TestableJsonWriter(null);
            Assert.True(writer.IsNullOrEmpty(new StubIJsonSerializable()));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesStringProperty()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", "value") };
            Assert.False(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesStringPropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (string)null) };
            Assert.True(writer.IsNullOrEmpty(serializable));            
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesStringPropertyWithEmptyValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", string.Empty) };
            Assert.True(writer.IsNullOrEmpty(serializable));            
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesBoolProperty()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", true) };
            Assert.False(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesBoolPropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (bool?)null) };
            Assert.True(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesDateTimeOffsetProperty()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", DateTimeOffset.UtcNow) };
            Assert.False(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesDateTimeOffsetPropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (DateTimeOffset?)null) };
            Assert.True(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesIntProperty()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", 42) };
            Assert.False(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesIntPropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (int?)null) };
            Assert.True(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesDoubleProperty()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", 0.0) };
            Assert.False(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesDoublePropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (double?)null) };
            Assert.True(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesTimeSpanProperty()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", TimeSpan.FromSeconds(0.0)) };
            Assert.False(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesTimeSpanPropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (TimeSpan?)null) };
            Assert.True(writer.IsNullOrEmpty(serializable));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesIJsonSerializableProperty()
        {
            var writer = new TestableJsonWriter(null);
            var child = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", "value") };
            var parent = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("child", child) };
            Assert.False(writer.IsNullOrEmpty(parent));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesIJsonSerializablePropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var parent = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("child", (IJsonSerializable)null) };
            Assert.True(writer.IsNullOrEmpty(parent));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesIJsonSerializablePropertyWithEmptyValue()
        {
            var writer = new TestableJsonWriter(null);
            var child = new StubIJsonSerializable();
            var parent = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", child) };
            Assert.True(writer.IsNullOrEmpty(parent));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesIDictionaryStringDoubleProperty()
        {
            var writer = new TestableJsonWriter(null);
            var instance = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", new Dictionary<string, double> { { "key", 42 } }) };
            Assert.False(writer.IsNullOrEmpty(instance));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesDoubleDictionaryPropertyWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var instance = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (IDictionary<string, double>)null) };
            Assert.True(writer.IsNullOrEmpty(instance));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesIDictionaryStringDoubleWithEmptyValue()
        {
            var writer = new TestableJsonWriter(null);
            var instance = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", new Dictionary<string, double>()) };
            Assert.True(writer.IsNullOrEmpty(instance));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesIDictionaryStringStringWithEmptyValue()
        {
            var writer = new TestableJsonWriter(null);
            var instance = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", new Dictionary<string, string>()) };
            Assert.True(writer.IsNullOrEmpty(instance));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsTrueWhenInstanceWritesIDictionaryStringStringWithNullValue()
        {
            var writer = new TestableJsonWriter(null);
            var instance = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", (IDictionary<string, string>)null) };
            Assert.True(writer.IsNullOrEmpty(instance));
        }

        [TestMethod]
        public void IsNullOrEmptyReturnsFalseWhenInstanceWritesIDictionaryStringStringProperty()
        {
            var writer = new TestableJsonWriter(null);
            var instance = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("name", new Dictionary<string, string> { { "name", "value" } }) };
            Assert.False(writer.IsNullOrEmpty(instance));
        }

        #endregion

        [TestMethod]
        public void WriteStartArrayWritesOpeningSquareBracket()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteStartArray();
                Assert.Equal("[", stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WriteStartObjectWritesOpeningCurlyBrace()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteStartObject();
                Assert.Equal("{", stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WriteEndArrayWritesClosingSquareBracket()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteEndArray();
                Assert.Equal("]", stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WriteEndObjectWritesClosingCurlyBrace()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteEndObject();
                Assert.Equal("}", stringWriter.ToString());
            }        
        }

        [TestMethod]
        public void WriteRawValueWritesValueWithoutEscapingValue()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteRawValue(@"Test\Name");
                Assert.Equal(@"Test\Name", stringWriter.ToString());
            }
        }

        #region WriteProperty(string, int?)

        [TestMethod]
        public void WritePropertyIntWritesIntValueWithoutQuotationMark()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                const string Name = "name";
                const int Value = 42;
                new JsonWriter(stringWriter).WriteProperty(Name, Value);
                Assert.Equal("\"" + Name + "\":" + Value, stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyIntDoesNothingIfValueIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (int?)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, double?)

        [TestMethod]
        public void WritePropertyDoubleWritesDoubleValueWithoutQuotationMark()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                const string Name = "name";
                const double Value = 42.3;
                new JsonWriter(stringWriter).WriteProperty(Name, Value);
                Assert.Equal("\"" + Name + "\":" + Value, stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyDoubleDoesNothingIfValueIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (double?)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, TimeSpan?)

        [TestMethod]
        public void WritePropertyTimeSpanWritesTimeSpanValueWithQuotationMark()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                const string Name = "name";
                TimeSpan value = TimeSpan.FromSeconds(123);
                new JsonWriter(stringWriter).WriteProperty(Name, value);
                Assert.Equal("\"" + Name + "\":\"" + value + "\"", stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyTimeSpanDoesNothingIfValueIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (TimeSpan?)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, string)

        [TestMethod]
        public void WritePropertyStringWritesValueInDoubleQuotes()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                const string Name = "name";
                const string Value = "value";
                new JsonWriter(stringWriter).WriteProperty(Name, Value);
                Assert.Equal("\"" + Name + "\":\"" + Value + "\"", stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyStringThrowsArgumentNullExceptionForNameInputAsNull()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var writer = new JsonWriter(stringWriter);
                Assert.Throws<ArgumentNullException>(() => writer.WriteProperty(null, "value"));
            }
        }

        [TestMethod]
        public void WritePropertyStringDoesNothingIfValueIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (string)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyStringDoesNothingIfValueIsEmptyBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", string.Empty);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, bool?)

        [TestMethod]
        public void WritePropertyBooleanWritesValueWithoutQuotationMarks()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                const string Name = "name";
                const bool Value = true;
                new JsonWriter(stringWriter).WriteProperty(Name, Value);
                string expectedValue = Value.ToString().ToLowerInvariant();
                Assert.Equal("\"" + Name + "\":" + expectedValue, stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyBooleanDoesNothingIfValueIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (bool?)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyBooleanWritesFalseBecauseItIsExplicitlySet()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                const string Name = "name";
                const bool Value = false;
                new JsonWriter(stringWriter).WriteProperty(Name, Value);
                string expectedValue = Value.ToString().ToLowerInvariant();
                Assert.Equal("\"" + Name + "\":" + expectedValue, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, DateTimeOffset?)

        [TestMethod]
        public void WritePropertyDateTimeOffsetWritesValueInQuotationMarksAndRoundTripDateTimeFormat()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                const string Name = "name";
                DateTimeOffset value = DateTimeOffset.UtcNow;
                new JsonWriter(stringWriter).WriteProperty(Name, value);
                string expectedValue = value.ToString("o", CultureInfo.InvariantCulture);
                Assert.Equal("\"" + Name + "\":\"" + expectedValue + "\"", stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyDateTimeOffsetDoesNothingIfValueIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (DateTimeOffset?)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, IDictionary<string, double>)

        [TestMethod]
        public void WritePropertyIDictionaryDoubleWritesPropertyNameFollowedByValuesInCurlyBraces()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var writer = new JsonWriter(stringWriter);
                writer.WriteProperty("name", new Dictionary<string, double> { { "key1", 1 } });
                AssertEx.StartsWith("\"name\":{", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
                AssertEx.EndsWith("}", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WritePropertyIDictionaryDoubleWritesValuesWithoutDoubleQuotes()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var writer = new JsonWriter(stringWriter);
                writer.WriteProperty("name", new Dictionary<string, double> { { "key1", 1 } });
                Assert.Contains("\"key1\":1", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WritePropertyIDictionaryDoubleDoesNothingWhenDictionaryIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (IDictionary<string, double>)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyIDictionaryDoubleDoesNothingWhenDictionaryIsEmptyBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", new Dictionary<string, double>());
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, IDictionary<string, object>)

        [TestMethod]
        public void WritePropertyIDictionaryStringStringWritesPropertyNameFollowedByValuesInCurlyBraces()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var writer = new JsonWriter(stringWriter);
                writer.WriteProperty("name", new Dictionary<string, string> { { "key1", "1" } });
                AssertEx.StartsWith("\"name\":{", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
                AssertEx.EndsWith("}", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WritePropertyIDictionaryStringStringWritesValuesWithoutDoubleQuotes()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var writer = new JsonWriter(stringWriter);
                writer.WriteProperty("name", new Dictionary<string, string> { { "key1", "1" } });
                Assert.Contains("\"key1\":\"1\"", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WritePropertyIDictionaryStringStringDoesNothingWhenDictionaryIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (IDictionary<string, string>)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyIDictionaryStringStringDoesNothingWhenDictionaryIsEmptyBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", new Dictionary<string, string>());
                Assert.Equal(string.Empty, stringWriter.ToString());
            }
        }

        #endregion

        #region WriteProperty(string, IJsonSerializable)

        [TestMethod]
        public void WritePropertyIJsonSerializableWritesPropertyName()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var serializable = new StubIJsonSerializable { OnSerialize = w => w.WriteProperty("child", "property") };
                new JsonWriter(stringWriter).WriteProperty("name", serializable);
                AssertEx.StartsWith("\"name\":", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WritePropertyIJsonSerializableInvokesSerialize()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                bool serializeInvoked = false;
                IJsonSerializable serializable = new StubIJsonSerializable { OnSerialize = w => serializeInvoked = true };
                new JsonWriter(stringWriter).WriteProperty("name", serializable);
                Assert.True(serializeInvoked);
            }
        }

        [TestMethod]
        public void WritePropertyIJsonSerializableDoesNothingWhenValueIsNullBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", (IJsonSerializable)null);
                Assert.Equal(string.Empty, stringWriter.ToString());
            }        
        }

        [TestMethod]
        public void WritePropertyIJsonSerializableDoesNothingWhenValueIsEmptyBecauseItAssumesPropertyIsOptional()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new JsonWriter(stringWriter).WriteProperty("name", new StubIJsonSerializable());
                Assert.Equal(string.Empty, stringWriter.ToString());
            }            
        }

        #endregion

        #region WritePropertyName

        [TestMethod]
        public void WritePropertyNameWritesPropertyNameEnclosedInDoubleQuotationMarksFollowedByColon()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                new TestableJsonWriter(stringWriter).WritePropertyName("TestProperty");
                Assert.Equal("\"TestProperty\":", stringWriter.ToString());
            }
        }

        [TestMethod]
        public void WritePropertyNamePrependsPropertyNameWithComaWhenCurrentObjectAlreadyHasProperties()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WritePropertyName("Property1");
                jsonWriter.WritePropertyName("Property2");
                Assert.Contains(",\"Property2\"", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WritePropertyNameDoesNotPrependPropertyNameWithComaWhenNewObjectWasStarted()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WritePropertyName("Property1");
                jsonWriter.WriteStartObject();
                jsonWriter.WritePropertyName("Property2");
                Assert.Contains("{\"Property2\"", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WritePropertyNameThrowsArgumentExceptionWhenPropertyNameIsEmptyToPreventOurOwnErrors()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new JsonWriter(stringWriter);
                Assert.Throws<ArgumentException>(() => jsonWriter.WritePropertyName(string.Empty));
            }
        }

        #endregion

        #region WriteString

        [TestMethod]
        public void WriteStringEscapesQuotationMark()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WriteString("Test\"Value");
                Assert.Contains("Test\\\"Value", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WriteStringEscapesBackslash()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WriteString("Test\\Value");
                Assert.Contains("Test\\\\Value", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WriteStringEscapesBackspace()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WriteString("Test\bValue");
                Assert.Contains("Test\\bValue", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }            
        }

        [TestMethod]
        public void WriteStringEscapesFormFeed()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WriteString("Test\fValue");
                Assert.Contains("Test\\fValue", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }            
        }

        [TestMethod]
        public void WriteStringEscapesNewline()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WriteProperty("name", "Test\nValue");
                Assert.Contains("Test\\nValue", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WriteStringEscapesCarriageReturn()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WriteProperty("name", "Test\rValue");
                Assert.Contains("Test\\rValue", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        [TestMethod]
        public void WriteStringEscapesHorizontalTab()
        {
            using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
            {
                var jsonWriter = new TestableJsonWriter(stringWriter);
                jsonWriter.WriteProperty("name", "Test\tValue");
                Assert.Contains("Test\\tValue", stringWriter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion

        private class TestableJsonWriter : JsonWriter
        {
            public TestableJsonWriter(TextWriter textWriter)
                : base(textWriter)
            {
            }

            public new bool IsNullOrEmpty(IJsonSerializable instance)
            {
                return base.IsNullOrEmpty(instance);
            }

            public new void WriteString(string value)
            {
                base.WriteString(value);
            }
        }

        private class StubIJsonSerializable : IJsonSerializable
        {
            public Action<IJsonWriter> OnSerialize = writer => { };

            public void Serialize(IJsonWriter jsonWriter)
            {
                this.OnSerialize(jsonWriter);
            }
        }
    }
}
