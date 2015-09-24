namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the Docker context, which includes the host name, image name, container name and container ID.
    /// The Docker context file is written in the following structure:
    ///     Docker host=host_name,Docker image=image_name,Docker container id=con_id,Docker container name=con_name
    /// </summary>
    public class DockerContext
    {
        /// <summary>
        /// The host machine.
        /// </summary>
        public string HostName
        {
            get
            {
                string value = null;
                this.Properties.TryGetValue(Constants.DockerHostPropertyName, out value);

                return value;
            }
        }

        /// <summary>
        /// Properties of Docker context.
        /// </summary>
        public Dictionary<string, string> Properties { get; private set; }

        /// <summary>
        /// Constucts new DockerContext instance.
        /// </summary>
        /// <param name="context">The context to parse.</param>
        public DockerContext(string context)
        {
            this.Properties = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(context))
            {
                Extract(context);
            }
        }

        private void Extract(string context)
        {
            string[] properties = context.Split(',');

            foreach (string kv in properties)
            {
                string[] split = kv.Split('=');
                string key = split[0];
                string value = split[1];
                
                this.Properties.Add(key, value);
            }
        }
    }
}
