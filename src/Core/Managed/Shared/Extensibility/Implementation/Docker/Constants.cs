using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    public static class Constants
    {
        public const string AiSdkDirectory = "/usr/appinsights/docker";
        
        public const string DockerHostPropertyName = "Docker host";
        public const string DockerImagePropertyName = "Docker image";
        public const string DockerContainerNamePropertyName = "Docker container name";
        public const string DockerContainerIdPropertyName = "Docker container id";
    }
}
