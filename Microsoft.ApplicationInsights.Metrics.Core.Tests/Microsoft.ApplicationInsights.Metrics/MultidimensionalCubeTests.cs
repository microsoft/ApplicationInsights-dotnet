using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.ApplicationInsights.Metrics;

namespace UnitTestProject2
{
    [TestClass]
    public class MultidimensionalCubeTests
    {
        [TestMethod]
        public void DummyTest()
        {
           var cube = new MultidimensionalCube<int, int>( (dims) => 42, 5, 5, 5 );
            Assert.IsNotNull(cube);
        }
    }
}
