namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
#if NET40 || NET45 || NET35
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class PropertyTest
    {
        [TestMethod]
        public void SanitizeNameTrimsLeadingAndTraliningSpaces()
        {
            const string Original = " name with spaces ";

            string sanitized = Original.SanitizeName();

            Assert.Equal(Original.Trim(), sanitized);
        }

        [TestMethod]
        public void SanitizeNameTruncatesValuesLongerThan1024Characters()
        {
            string original = new string('A', Property.MaxNameLength + 1);
            string sanitized = original.SanitizeName();

            Assert.Equal(Property.MaxNameLength, sanitized.Length);
        }

        [TestMethod]
        public void SanitizeNameDontTruncatesValuesSmallerThan1024Characters()
        {
            const int ValueLength = 512;

            string original = new string('c', ValueLength);
            string sanitized = original.SanitizeName();

            Assert.Equal(ValueLength, sanitized.Length);
        }

        [TestMethod]
        public void SanitizeValueTruncatesValuesLongerThan1024Characters()
        {
            string original = new string('A', Property.MaxValueLength + 10);
            string sanitized = original.SanitizeValue();

            Assert.Equal(Property.MaxValueLength, sanitized.Length);
        }

        [TestMethod]
        public void SanitizeValueDontTruncatesValuesSmallerThan1024Characters()
        {
            const int ValueLength = 512;

            string original = new string('c', ValueLength);
            string sanitized = original.SanitizeValue();

            Assert.Equal(ValueLength, sanitized.Length);
        }

        [TestMethod]
        public void SanitizeValueTrimsLeadingAndTraliningSpaces()
        {
            const string Original = " name with spaces ";
            string sanitized = Original.SanitizeValue();

            Assert.Equal(Original.Trim(), sanitized);
        }

        [TestMethod]
        public void SanitizeMessgaeTruncatesValuesLongerThan32768Characters()
        {
            const int MaxMessageLength = 32768;

            string original = new string('M', MaxMessageLength + 10);
            string sanitized = original.SanitizeMessage();

            Assert.Equal(MaxMessageLength, sanitized.Length);
        }

        [TestMethod]
        public void SanitizeMessageDontTruncatesValuesSmallerThan32768Characters()
        {
            const int MessageLength = 512;

            string original = new string('m', MessageLength);
            string sanitized = original.SanitizeMessage();

            Assert.Equal(MessageLength, sanitized.Length);
        }
        
        [TestMethod]
        public void SanitizeMessageTrimsLeadingAndTraliningSpaces()
        {
            const string Original = " name with   spaces    ";
            string sanitized = Original.SanitizeMessage();

            Assert.Equal(Original.Trim(), sanitized);
        }

        [TestMethod]
        public void SanitizeUriTruncatesValuesLongerThan2048Characters()
        {
            const int MaxUrlLength = 2048;

            string original = new string('M', MaxUrlLength);
            original = string.Concat("https://test.com/", original);
            
            Uri originalUri = new Uri(original);
            Uri sanitized = originalUri.SanitizeUri();

            Assert.Equal(MaxUrlLength, sanitized.ToString().Length);
        }

        [TestMethod]
        public void SanitizeUriDontTruncatesValuesSmallerThan2048Characters()
        {
            const int UriLength = 512;

            string original = new string('m', UriLength);
            original = string.Concat("https://m.com/", original);
            int originalUriLength = original.Length;

            Uri originalUri = new Uri(original);
            Uri sanitized = originalUri.SanitizeUri();

            Assert.Equal(originalUriLength, sanitized.ToString().Length);
        }

        [TestMethod]
        public void SanitizePropertiesTrimsLeadingAndTrailingSpaceInKeyNames()
        {
            const string OriginalKey = " key with spaces ";
            const string OriginalValue = "Test Value";
            var original = new Dictionary<string, string> { { OriginalKey, OriginalValue } };

            original.SanitizeProperties();

            string sanitizedKey = OriginalKey.Trim();
            Assert.Equal(new[] { new KeyValuePair<string, string>(sanitizedKey, OriginalValue) }, original);
        }

        [TestMethod]
        public void SanitizePropertiesReplacesEmptyStringWithEmptyWordToEnsurePropertyValueWillBeSerializedWithoutExceptions()
        {
            var dictionary = new Dictionary<string, string> { { string.Empty, string.Empty } };
            dictionary.SanitizeProperties();
            Assert.Equal("(required property name is empty)", dictionary.Single().Key);
        }

        [TestMethod]
        public void SanitizePropertiesReplacesSpecialCharactersWithUnderscores()
        {
            foreach (char invalidCharacter in GetInvalidNameCharacters())
            {
                string originalKey = "test" + invalidCharacter + "key";
                const string OriginalValue = "Test Value";
                var original = new Dictionary<string, string> { { originalKey, OriginalValue } };

                original.SanitizeProperties();

                string sanitizedKey = originalKey.Replace(invalidCharacter, '_');
                Assert.Equal(new[] { new KeyValuePair<string, string>(sanitizedKey, OriginalValue) }, original);
            }
        }

        [TestMethod]
        public void SanitizePropertiesMakesKeyUniqueAfterReplacingSpecialCharactersWithUnderscores()
        {
            string originalKey = "test#key";
            var dictionary = new Dictionary<string, string> 
            {
                { originalKey, string.Empty },
                { originalKey.Replace("#", "_"), string.Empty },
            };

            dictionary.SanitizeProperties();

            Assert.Contains("test_key001", dictionary.Keys);
        }

        [TestMethod]
        public void SanitizePropertiesTruncatesKeysLongerThan150Characters()
        {
            string originalKey = new string('A', Property.MaxNameLength + 1);
            const string OriginalValue = "Test Value";
            var original = new Dictionary<string, string> { { originalKey, OriginalValue } };

            original.SanitizeProperties();

            Assert.Equal(Property.MaxDictionaryNameLength, original.First().Key.Length);
        }

        [TestMethod]
        public void SanitizePropertiesMakesKeysUniqueAfterTruncation()
        {
            string originalKey = new string('A', Property.MaxDictionaryNameLength + 1);
            const string OriginalValue = "Test Value";
            var original = new Dictionary<string, string> 
            { 
                { originalKey + "1", OriginalValue },
                { originalKey + "2", OriginalValue },
                { originalKey + "3", OriginalValue },
            };

            original.SanitizeProperties();

            Assert.Equal(3, original.Count);
            Assert.Equal(Property.MaxDictionaryNameLength, original.Keys.Max(key => key.Length));
        }

        [TestMethod]
        public void SanitizePropertiesTruncatesValuesLongerThan1024Characters()
        {
            const string OriginalKey = "test";
            string originalValue = new string('A', Property.MaxValueLength + 10);
            var original = new Dictionary<string, string> { { OriginalKey, originalValue } };

            original.SanitizeProperties();

            string sanitizedValue = originalValue.Substring(0, Property.MaxValueLength);
            Assert.Equal(new[] { new KeyValuePair<string, string>(OriginalKey, sanitizedValue) }, original);
        }

        [TestMethod]
        public void SanitizePropertiesTrimsLeadingAndTraliningSpacesFromValues()
        {
            const string OriginalKey = "test";
            const string OriginalValue = " name with spaces ";
            var original = new Dictionary<string, string> { { OriginalKey, OriginalValue } };

            original.SanitizeProperties();

            string sanitizedValue = OriginalValue.Trim();
            Assert.Equal(new[] { new KeyValuePair<string, string>(OriginalKey, sanitizedValue) }, original);
        }

        [TestMethod]
        public void SanitizeMeasurementsTrimsLeadingAndTrailingSpaceInKeyNames()
        {
            const string OriginalKey = " key with spaces ";
            const double OriginalValue = 42.0;
            var original = new Dictionary<string, double> { { OriginalKey, OriginalValue } };

            original.SanitizeMeasurements();

            string sanitizedKey = OriginalKey.Trim();
            Assert.Equal(new[] { new KeyValuePair<string, double>(sanitizedKey, OriginalValue) }, original);
        }

        [TestMethod]
        public void SanitizeMeasurementsReplacesSpecialCharactersWithUnderscores()
        {
            foreach (char invalidCharacter in GetInvalidNameCharacters())
            {
                string originalKey = "test" + invalidCharacter + "key";
                const double OriginalValue = 42.0;
                var original = new Dictionary<string, double> { { originalKey, OriginalValue } };

                original.SanitizeMeasurements();

                string sanitizedKey = originalKey.Replace(invalidCharacter, '_');
                Assert.Equal(new[] { new KeyValuePair<string, double>(sanitizedKey, OriginalValue) }, original);
            }
        }

        [TestMethod]
        public void SanitizeMeasurementsTruncatesKeysLongerThan150Characters()
        {
            string originalKey = new string('A', Property.MaxNameLength + 1);
            const double OriginalValue = 42.0;
            var original = new Dictionary<string, double> { { originalKey, OriginalValue } };

            original.SanitizeMeasurements();

            Assert.Equal(Property.MaxDictionaryNameLength, original.First().Key.Length);
        }

        [TestMethod]
        public void SanitizeMeasurementsMakesKeysUniqueAfterTruncation()
        {
            string originalKey = new string('A', Property.MaxNameLength + 1);
            const double OriginalValue = 42.0;
            var original = new Dictionary<string, double> 
            { 
                { originalKey + "1", OriginalValue },
                { originalKey + "2", OriginalValue },
                { originalKey + "3", OriginalValue },
            };

            original.SanitizeMeasurements();

            Assert.Equal(3, original.Count);
            Assert.Equal(Property.MaxDictionaryNameLength, original.Keys.Max(key => key.Length));
        }
        
        private static IEnumerable<char> GetInvalidNameCharacters()
        {
            var invalidCharacters = new List<char>();
            for (int i = 0; i < 128; i++)
            {
                char c = Convert.ToChar(i);
                if (!IsValidNameCharacter(c))
                {
                    invalidCharacters.Add(c);
                }
            }

            return invalidCharacters;
        }

        private static bool IsValidNameCharacter(char c)
        {
            // Valid Characters:  a-z, A-Z, 0-9, /, \, (, ), _, -, ., sp
            const string ValidSymbols = @"/\()_-. ";
            return char.IsLetterOrDigit(c) || ValidSymbols.Contains(c.ToString());
        }
    }
}