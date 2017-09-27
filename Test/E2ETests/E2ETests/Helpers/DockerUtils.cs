using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E2ETests.Helpers
{
    public class DockerUtils
    {
        internal static string DockerComposeBaseCommandFormat = "/c docker-compose";
        internal static string DockerBaseCommandFormat = "/c docker";

        public static void ExecuteDockerComposeCommand(string action, string dockerComposeFile)
        {
            string dockerComposeFullCommandFormat = string.Format("{0} -f {1} {2}", DockerComposeBaseCommandFormat, dockerComposeFile, action);
            CommandLineUtils.ExecuteCommandInCmd(dockerComposeFullCommandFormat);
        }
        public static void ExecuteDockerCommand(string command)
        {
            string dockerFullCommand = string.Format("{0} {1}", DockerBaseCommandFormat, command);
            CommandLineUtils.ExecuteCommandInCmd(dockerFullCommand);
        }
    }
}
