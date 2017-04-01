namespace Microsoft.ApplicationInsights
{
    using Microsoft.ApplicationInsights.Extensibility.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryInitailizersTestsNet45 : TelemetryInitailizersTestsNet40
    {
        [TestInitialize]
        public void Initialize()
        {
            ConfigurationHelpers.ConfigureNet45();
        }
    }
}
