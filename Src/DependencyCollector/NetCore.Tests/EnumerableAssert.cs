namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A static class collection of assertions that operate on IEnumerables.
    /// </summary>
    public static class EnumerableAssert
    {
        /// <summary>
        /// Determine if the contents of the provided IEnumerables are equal and in the same order.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        public static void AreEqual<T>(IEnumerable<T> lhs, IEnumerable<T> rhs)
        {
            if (lhs != rhs)
            {
                Assert.IsNotNull(lhs);
                Assert.IsNotNull(rhs);
                CollectionAssert.AreEqual(lhs.ToArray(), rhs.ToArray());
            }
        }
    }
}
