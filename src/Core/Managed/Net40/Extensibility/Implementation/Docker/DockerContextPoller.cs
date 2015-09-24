using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    public class DockerContextPoller
    {
        private const int ThreadPollingIntervalInSeconds = 2;
        private const string ContextFileName = "docker.info";
        private volatile DockerContext dockerContext = null;
        private readonly string contextFilePath;
        private readonly DockerContextFactory dockerContextFactory;

        public DockerContextPoller(string contextFileDirectory)
        {
            this.contextFilePath = Path.Combine(contextFileDirectory, ContextFileName);
            this.dockerContextFactory = new DockerContextFactory();
        }

        public bool Completed { get; private set; }

        public DockerContext DockerContext
        {
            get { return this.dockerContext; }
            set { this.dockerContext = value; }
        }

        public void Start()
        {
            Task.Factory.StartNew(StartPollForContextFile);
        }

        private void StartPollForContextFile()
        {
            bool fileExists = false;
            while (!fileExists)
            {
                fileExists = File.Exists(this.contextFilePath);

                if (!fileExists)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(ThreadPollingIntervalInSeconds));

                    continue;
                }

                this.DockerContext = this.dockerContextFactory.CreateDockerContext(this.contextFilePath);
                this.Completed = true;
            }
        }
    }
}
