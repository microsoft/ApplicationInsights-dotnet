namespace Microsoft.ApplicationInsights.Extensibility.Web
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TelemetryModulesTestsNet45 : TelemetryModulesTestsNet40
    {
        [TestInitialize]
        public void Initialize()
        {
            ConfigurationHelpers.ConfigureNet45();
        }
    }
}