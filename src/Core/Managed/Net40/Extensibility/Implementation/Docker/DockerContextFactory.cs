using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    class DockerContextFactory
    {
        public DockerContext CreateDockerContext(string contextFilePath)
        {
            string fileContent = File.ReadAllText(contextFilePath);

            return new DockerContext(fileContent);
        }
    }
}
