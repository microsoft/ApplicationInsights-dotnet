using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Common.Extensions;

namespace Microsoft.ApplicationInsights.Common
{
    [TestClass]
    public class EnumExtensionsTests
    {
        [TestMethod]
        public void TestEnumGetAttribute()
        {
            var test1 = TestEnum.One.GetAttribute<TestEnumAttribute>();
            Assert.AreEqual("111", test1.Value);

            var test2 = TestEnum.Two.GetAttribute<TestEnumAttribute>();
            Assert.AreEqual("222", test2.Value);
        }

        private enum TestEnum
        {
            [TestEnum(Value = "111")]
            One,

            [TestEnum(Value = "222")]
            Two,
        }

        private class TestEnumAttribute : Attribute
        {
            public string Value { get; set; }
        }
    }
}