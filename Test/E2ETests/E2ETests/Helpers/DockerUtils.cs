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
        public static string ExecuteDockerCommand(string command)
        {
            string dockerFullCommand = string.Format("{0} {1}", DockerBaseCommandFormat, command);
            string output = CommandLineUtils.ExecuteCommandInCmd(dockerFullCommand);
            return output;
        }

        public static void RestartDockerContainer(string containerName)
        {
            ExecuteDockerCommand("restart " + containerName);
        }

        public static string FindIpDockerContainer(string containerName, string networkName = "nat")
        {
            string commandToFindIp = string.Format("inspect -f \"{{.NetworkSettings.Networks.{0}.IPAddress}}\" {1}", networkName, containerName);            
            string ip = ExecuteDockerCommand(commandToFindIp);
            return ip;
        }
    }
}
