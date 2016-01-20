namespace Microsoft.ApplicationInsights.WindowsServer.Implementation
{
    using System;
    using System.Diagnostics;
    using System.Reflection;   
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TypeHelpersTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            Trace.WriteLine(Assembly.GetExecutingAssembly().FullName);
        }

        [TestMethod]
        public void GetTypeReturnsNullForUnknownAssembly()
        {
            Assert.IsNull(TypeHelpers.GetLoadedType("MyType", "MyAssembly"));
        }

        [TestMethod]
        public void GetTypeReturnsNullForUnknownType()
        {
            Assert.IsNull(TypeHelpers.GetLoadedType("MyType", Assembly.GetExecutingAssembly().GetName().Name));
        }

        [TestMethod]
        public void GetTypeReturnsTypeCorrectTypeAndAssembly()
        {
            Type expected = typeof(RoleEnvironment);
            Type actual = TypeHelpers.GetLoadedType(expected.ToString(), expected.Assembly.GetName().Name);

            Assert.AreEqual(expected, actual);
        }
    }
}
