namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    public static class DockerContextHelper
    {
        private const string ContextFileTemplate = "Docker host={0},Docker image={1},Docker container name={2},Docker container id={3}";
        public const string HostName = "host name";
        public const string ImageName = "image name";
        public const string ContainerName = "container name";
        public const string ContainerId = "container id";

        public static string GetTestContextString()
        {
            return string.Format(ContextFileTemplate, HostName, ImageName, ContainerName, ContainerId);
        }
    }
}
