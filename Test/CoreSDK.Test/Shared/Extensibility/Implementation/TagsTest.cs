namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
#if NET40 || NET45 || NET35 || NET46
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif
    using Assert = Xunit.Assert;

    [TestClass]
    public class TagsTest
    {
        [TestMethod]
        public void GetTagValueOrNullReturnsCorrectStringValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "testValue";

            tags[testKey] = testValue;

            string tagValue = tags.GetTagValueOrNull(testKey);

            Assert.Equal(testValue, tagValue);
        }

        [TestMethod]
        public void GetTagValueOrNullReturnsEmpty()
        {
            var tags = new Dictionary<string, string>();

            string tagValue = tags.GetTagValueOrNull("testKey");

            Assert.Null(tagValue);
        }

        [TestMethod]
        public void GetTagBoolValueOrNullReturnsCorrectBoolValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "true";

            tags[testKey] = testValue;

            bool? tagValue = tags.GetTagBoolValueOrNull(testKey);

            Assert.True(tagValue.Value);
        }

        [TestMethod]
        public void GetTagBoolValueOrNullReturnsNull()
        {
            var tags = new Dictionary<string, string>();

            bool? tagValue = tags.GetTagBoolValueOrNull("testKey");

            Assert.Null(tagValue);
        }

        [TestMethod]
        public void GetTagIntValueOrNullReturnsCorrectIntValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "5";

            tags[testKey] = testValue;

            int? tagValue = tags.GetTagIntValueOrNull(testKey);

            Assert.Equal(5, tagValue.Value);
        }

        [TestMethod]
        public void GetTagIntValueOrNullReturnsNull()
        {
            var tags = new Dictionary<string, string>();

            int? tagValue = tags.GetTagIntValueOrNull("testKey");

            Assert.Null(tagValue);
        }

        [TestMethod]
        public void GetTagDateTimeOffsetValueOrNullReturnsCorrectDateTimeOffsetValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "2014-09-23T00:00:00.0000000-07:00";

            tags[testKey] = testValue;

            DateTimeOffset? tagValue = tags.GetTagDateTimeOffsetValueOrNull(testKey);

            DateTimeOffset expectedValue = new DateTimeOffset(new DateTime(2014, 9, 23), TimeSpan.FromHours(-7));

            Assert.Equal(expectedValue, tagValue);
        }

        [TestMethod]
        public void GetTagDateTimeOffsetValueOrNullReturnsNull()
        {
            var tags = new Dictionary<string, string>();

            DateTimeOffset? tagValue = tags.GetTagDateTimeOffsetValueOrNull("testKey");

            Assert.Null(tagValue);
        }

        [TestMethod]
        public void SetStringValueOrRemoveSetsCorrectStringValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "testValue";

            tags.SetStringValueOrRemove(testKey, testValue);

            Assert.Equal(testValue, tags[testKey]);
        }

#if NET40 || NET45
        [TestMethod]
        public void SetStringValueOrRemoveDoesNotThrowsWhenValueIsNull()
        {
            var tags = new Dictionary<string, string>();

            Assert.DoesNotThrow(() => tags.SetStringValueOrRemove("testKey", null));
        }
#endif

        [TestMethod]
        public void SetDateTimeOffsetValueOrRemoveSetsCorrectDateTimeOffsetValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            DateTimeOffset testValue = new DateTimeOffset(new DateTime(2014, 9, 23), TimeSpan.FromHours(-7));

            tags.SetDateTimeOffsetValueOrRemove(testKey, testValue);

            Assert.Equal("2014-09-23T00:00:00.0000000-07:00", tags[testKey]);
        }

        [TestMethod]
        public void SetDateTimeOffsetValueOrRemoveRemovesValue()
        {
            var tags = new Dictionary<string, string>();
            
            string testKey = "testKey";
            string testValue = "2014-09-23T00:00:00.0000000-07:00";

            tags[testKey] = testValue;

            tags.SetDateTimeOffsetValueOrRemove(testKey, null);

            Assert.False(tags.ContainsKey(testKey));
        }

        [TestMethod]
        public void SetTagValueOrRemoveRemovesValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "testValue";

            tags[testKey] = testValue;

            tags.SetTagValueOrRemove<bool?>(testKey, null);

            Assert.False(tags.ContainsKey(testKey));
        }

        [TestMethod]
        public void SetTagValueOrRemoveSetsCorrectBoolValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "True";

            tags.SetTagValueOrRemove<bool?>(testKey, true);

            Assert.Equal(testValue, tags[testKey]);
        }

        [TestMethod]
        public void SetTagValueOrRemoveSetsCorrectIntValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "5";

            tags.SetTagValueOrRemove<int?>(testKey, 5);

            Assert.Equal(testValue, tags[testKey]);
        }

        [TestMethod]
        public void SetTagValueOrRemoveSetsCorrectStringValue()
        {
            var tags = new Dictionary<string, string>();

            string testKey = "testKey";
            string testValue = "testValue";

            tags.SetTagValueOrRemove<string>(testKey, testValue);

            Assert.Equal(testValue, tags[testKey]);
        }        
    }
}
