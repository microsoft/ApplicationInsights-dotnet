namespace Microsoft.ApplicationInsights
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CommonInitialize
    {
        [AssemblyInitialize]
        public static void MyTestInitialize(TestContext testContext)
        {
            Microsoft.ApplicationInsights.Extensibility.Web.Implementation.Transmitter.TestHookShouldStartRunner = false;
        }
    }
}
