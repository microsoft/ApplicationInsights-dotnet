using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ApplicationInsights.Metrics
{
    /// <summary />
    [TestClass]
    public class MultidimensionalCubeTests
    {
        /// <summary />
        [TestMethod]
        public void DummyTest()
        {
           var cube = new MultidimensionalCube<int, int>( (dims) => 42, 5, 5, 5 );
            Assert.IsNotNull(cube);
        }
    }
}
