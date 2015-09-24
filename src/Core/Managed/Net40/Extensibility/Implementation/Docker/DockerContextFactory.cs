namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    using System.IO;

    class DockerContextFactory
    {
        public DockerContext CreateDockerContext(string contextFilePath)
        {
            string fileContent = File.ReadAllText(contextFilePath);

            return new DockerContext(fileContent);
        }
    }
}
