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
            string fileContent = String.Empty;
            #if NET40 || NET45
                fileContent = File.ReadAllText(contextFilePath);
            #endif

            return new DockerContext(fileContent);
        }
    }
}
