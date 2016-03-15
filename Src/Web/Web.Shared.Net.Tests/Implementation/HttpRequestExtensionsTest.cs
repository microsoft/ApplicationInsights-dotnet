namespace Microsoft.ApplicationInsights.Web.Implementation
{
    using System;
    using System.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpRequestExtensionsTest
    {
        [TestMethod]
        public void GetUserHostAddressReturnsNullIfRequestIsNull()
        {
            HttpRequestWrapper wr = null;
            Assert.IsNull(wr.GetUserHostAddress());
        }

        [TestMethod]
        public void GetUserHostAddressReturnsNullInCaseOfArgumentException()
        {
            var hr = new TestableHttpRequest { OnUserHostAddress = () => { throw new ArgumentException(); } };

            Assert.IsNull(hr.GetUserHostAddress());
        }

        [TestMethod]
        public void GetUserHostAddressReturnsUserHostAddress()
        {
            var hr = new TestableHttpRequest { OnUserHostAddress = () => { return "123"; } };

            Assert.AreEqual("123", hr.GetUserHostAddress());
        }

        internal class TestableHttpRequest : HttpRequestBase
        {
            public Func<string> OnUserHostAddress { get; set; }

            public override string UserHostAddress
            {
                get { return this.OnUserHostAddress(); }
            }
        }
    }
}
