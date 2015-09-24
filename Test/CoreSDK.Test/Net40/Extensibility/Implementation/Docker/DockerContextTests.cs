using Microsoft.ApplicationInsights.Extensibility.Implementation.External;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = Xunit.Assert;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    [TestClass]
    public class DockerContextTests
    {
        [TestMethod]
        public void TestContextStringParsedCorrectly()
        {
            DockerContext contextUnderTest = new DockerContext(DockerContextHelper.GetTestContextString());

            Assert.Matches(DockerContextHelper.HostName, contextUnderTest.HostName);
            Assert.Matches(DockerContextHelper.HostName, contextUnderTest.Properties.GetTagValueOrNull(Constants.DockerHostPropertyName));
            Assert.Matches(DockerContextHelper.ImageName, contextUnderTest.Properties.GetTagValueOrNull(Constants.DockerImagePropertyName));
            Assert.Matches(DockerContextHelper.ContainerName, contextUnderTest.Properties.GetTagValueOrNull(Constants.DockerContainerNamePropertyName));
            Assert.Matches(DockerContextHelper.ContainerId, contextUnderTest.Properties.GetTagValueOrNull(Constants.DockerContainerIdPropertyName));
        }

        [TestMethod]
        public void TestNullContextStringNotThrowException()
        {
            DockerContext contextUnderTest = new DockerContext(null);

            Assert.Null(contextUnderTest.HostName);
        }

        [TestMethod]
        public void TestEmptyContextStringNotThrowException()
        {
            DockerContext contextUnderTest = new DockerContext(string.Empty);

            Assert.Null(contextUnderTest.HostName);
        }
    }
}
