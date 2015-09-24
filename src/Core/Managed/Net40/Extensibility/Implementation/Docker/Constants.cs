namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    /// <summary>
    /// Provides Docker constants.
    /// 
    /// Note: changes in the properties' values require modification of the Application Insights Monitoring Container.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The directory in which the Docker context file is being searched.
        /// </summary>
        public const string AiSdkDirectory = "/usr/appinsights/docker";
        
        /// <summary>
        /// The Docker host property name.
        /// </summary>
        public const string DockerHostPropertyName = "Docker host";
        
        /// <summary>
        /// The Docker image property name.
        /// </summary>
        public const string DockerImagePropertyName = "Docker image";

        /// <summary>
        /// The Docker container name property name.
        /// </summary>
        public const string DockerContainerNamePropertyName = "Docker container name";

        /// <summary>
        /// The Docker container ID property name.
        /// </summary>
        public const string DockerContainerIdPropertyName = "Docker container id";
    }
}
