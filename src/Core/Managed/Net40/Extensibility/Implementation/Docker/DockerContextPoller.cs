namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// This class responsible for periodically checking the existence of a Docker context file, and parsing it when available.
    /// </summary>
    public class DockerContextPoller
    {
        private const int ThreadPollingIntervalInSeconds = 2;
        private const string ContextFileName = "docker.info";
        private volatile DockerContext dockerContext;
        private readonly string contextFilePath;
        private readonly DockerContextFactory dockerContextFactory;

        /// <summary>
        /// Constructs new DockerContextPoller instance.
        /// </summary>
        /// <param name="contextFileDirectory"></param>
        public DockerContextPoller(string contextFileDirectory)
        {
            this.contextFilePath = Path.Combine(contextFileDirectory, ContextFileName);
            this.dockerContextFactory = new DockerContextFactory();
        }

        /// <summary>
        /// The Docker context.
        /// </summary>
        public DockerContext DockerContext
        {
            get { return this.dockerContext; }
            set { this.dockerContext = value; }
        }

        /// <summary>
        /// Start polling for Docker context file.
        /// </summary>
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
            }
        }
    }
}
