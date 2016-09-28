namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Shared DependencyTrackingTelemetryModuleTest class.
    /// </summary>
    [TestClass]
    public partial class HttpDependenciesParsingTelemetryInitializerTest
    {
        [TestMethod]
        public void HttpDependenciesParsingTelemetryInitializerDoesNotFailOnNull()
        {
            HttpDependenciesParsingTelemetryInitializer initializer = new HttpDependenciesParsingTelemetryInitializer();
            initializer.Initialize(null);
        }

        [TestMethod]
        public void HttpDependenciesParsingTelemetryInitializerDoesNotFailOnRequestTelemetry()
        {
            HttpDependenciesParsingTelemetryInitializer initializer = new HttpDependenciesParsingTelemetryInitializer();
            initializer.Initialize(new RequestTelemetry());
        }

        [TestMethod]
        public void HttpDependenciesParsingTelemetryInitializerDoesNotFailOnNonHttpDependencyTelemetry()
        {
            HttpDependenciesParsingTelemetryInitializer initializer = new HttpDependenciesParsingTelemetryInitializer();
            initializer.Initialize(new DependencyTelemetry("nonHttp", "blob.core.windows.net", "GET test", "http://blob.core.windows.net/t/t"));
        }

        [TestMethod]
        public void HttpDependenciesParsingTelemetryInitializerConvertsBlobs()
        {
            HttpDependenciesParsingTelemetryInitializer initializer = new HttpDependenciesParsingTelemetryInitializer();
            var d = new DependencyTelemetry("HTTP", "tofinthyperion2dets001.blob.core.windows.net", "GET /observations-v01-1410//ecaf8435-5395-434f-b1e2-f06cd5703792/K0000000102-R1623814285-T3804215385-C3804215385/16/2100", "https://tofinthyperion2dets001.blob.core.windows.net/observations-v01-1410//ecaf8435-5395-434f-b1e2-f06cd5703792/K0000000102-R1623814285-T3804215385-C3804215385/16/2100?comp=page&timeout=3");
            initializer.Initialize(d);
            Assert.AreEqual("Azure blob", d.Type);
            Assert.AreEqual("tofinthyperion2dets001.blob.core.windows.net", d.Target);
            Assert.AreEqual("GET tofinthyperion2dets001/observations-v01-1410", d.Name);
        }
    }
}