using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation.Docker;
using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Xunit.Assert;

namespace Microsoft.ApplicationInsights.Extensibility
{
    [TestClass]
    public class DockerContextInitializerTests
    {
        private static DockerContext dockerContext = new DockerContext(DockerContextHelper.GetTestContextString());
        private static DockerContextInitializer initializerUnderTest;
        
        [TestMethod]
        public void WhenPollerCompletedTelemetryInitializedWithContext()
        {
            DockerContextPoller poller = new DockerContextPoller(string.Empty)
            {
                DockerContext = dockerContext
            };

            initializerUnderTest = new DockerContextInitializer(poller);

            TraceTelemetry trace = new TraceTelemetry();
            initializerUnderTest.Initialize(trace);

            Assert.Matches(DockerContextHelper.HostName, trace.Context.Device.Id);
            Assert.Matches(DockerContextHelper.HostName, trace.Properties.GetTagValueOrNull(Implementation.Docker.Constants.DockerHostPropertyName));
            Assert.Matches(DockerContextHelper.ImageName, trace.Properties.GetTagValueOrNull(Implementation.Docker.Constants.DockerImagePropertyName));
            Assert.Matches(DockerContextHelper.ContainerName, trace.Properties.GetTagValueOrNull(Implementation.Docker.Constants.DockerContainerNamePropertyName));
            Assert.Matches(DockerContextHelper.ContainerId, trace.Properties.GetTagValueOrNull(Implementation.Docker.Constants.DockerContainerIdPropertyName));
        }
    }
}
