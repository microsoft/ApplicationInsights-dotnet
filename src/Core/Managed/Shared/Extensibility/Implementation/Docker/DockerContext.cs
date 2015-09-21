using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Docker
{
    /**
     * Represents the Docker context, which includes the host name, image name, container name and container ID.
     * The Docker context file is written in the following structure:
     *      Docker host=host_name,Docker image=image_name,Docker container id=con_id,Docker container name=con_name
     */
    public class DockerContext
    {
        public string HostName { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }

        public DockerContext(string context)
        {
            this.Properties = new Dictionary<string, string>();
            Extract(context);
        }

        private void Extract(string context)
        {
            string[] properties = context.Split(',');

            foreach (string kv in properties)
            {
                string[] split = kv.Split('=');
                string key = split[0];
                string value = split[1];

                if (key.Equals(Constants.DockerHostPropertyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.HostName = value;
                }
                else
                {
                    this.Properties.Add(key, value);
                }
            }
        }
    }
}
