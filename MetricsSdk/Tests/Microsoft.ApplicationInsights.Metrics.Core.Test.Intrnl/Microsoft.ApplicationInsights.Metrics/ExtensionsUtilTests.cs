using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights.Metrics.Extensions
{
    /// <summary />
    [TestClass]
    public class ExtensionsUtilTests
    {
        /// <summary />
        [TestMethod]
        public void ValidateNotNull()
        {
            Util.ValidateNotNull("foo", "specified name");
            Assert.ThrowsException<ArgumentNullException>( () => Util.ValidateNotNull(null, "specified name") );
        }

        /// <summary />
        [TestMethod]
        public void EnsureConcreteValue()
        {
            Assert.AreEqual(-1.7976931348623157E+308, Util.EnsureConcreteValue(Double.MinValue));
            Assert.AreEqual(Double.MinValue, Util.EnsureConcreteValue(-1.7976931348623157E+308));

            Assert.AreEqual(1.7976931348623157E+308, Util.EnsureConcreteValue(Double.MaxValue));
            Assert.AreEqual(Double.MaxValue, Util.EnsureConcreteValue(1.7976931348623157E+308));

            Assert.AreEqual(4.94065645841247E-324, Util.EnsureConcreteValue(Double.Epsilon));
            Assert.AreEqual(Double.Epsilon, Util.EnsureConcreteValue(4.94065645841247E-324));

            Assert.AreEqual(0.0, Util.EnsureConcreteValue(Double.NaN));
            Assert.AreEqual(Double.MinValue, Util.EnsureConcreteValue(Double.NegativeInfinity));
            Assert.AreEqual(Double.MaxValue, Util.EnsureConcreteValue(Double.PositiveInfinity));
        }
    }
}
