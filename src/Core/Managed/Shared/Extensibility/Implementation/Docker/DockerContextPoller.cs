using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    public class DockerContextPoller
    {
        private const int ThreadPollingIntervalInSeconds = 2;
        private const string ContextFileName = "docker.info";
        private readonly string contextFilePath;
        private readonly DockerContextFactory dockerContextFactory;

        public DockerContextPoller(string contextFileDirectory)
        {
            this.contextFilePath = Path.Combine(contextFileDirectory, ContextFileName);
            this.dockerContextFactory = new DockerContextFactory();
        }

        public bool Completed { get; private set; }

        public DockerContext DockerContext { get; private set; }

        public void Start()
        {
            throw new NotImplementedException();
        }

        private void StartPollForContextFile()
        {
            do
            {
                var isFileExists = File.Exists(this.contextFilePath);

                if (!isFileExists)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(ThreadPollingIntervalInSeconds));

                    continue;
                }

                this.DockerContext = this.dockerContextFactory.CreateDockerContext(this.contextFilePath);
            } while (!File.Exists(this.contextFilePath));
        }
    }
}
