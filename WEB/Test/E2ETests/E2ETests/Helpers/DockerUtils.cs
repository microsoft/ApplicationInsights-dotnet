using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static string ExecuteDockerCommand(string command, bool ignoreCmdError = false)
        {
            string dockerFullCommand = string.Format("{0} {1}", DockerBaseCommandFormat, command);
            string output = CommandLineUtils.ExecuteCommandInCmd(dockerFullCommand, ignoreCmdError);
            return output;
        }

        public static string GetDockerStateStatus(string containerName)
        {
            string dockerFullCommand = "inspect -f \"{{.State.Status}}\" " + containerName;
            string output = ExecuteDockerCommand(dockerFullCommand);
            return output;
        }

        public static string GetDockerStateExitCode(string containerName)
        {
            string dockerFullCommand = "inspect -f \"{{.State.ExitCode}}\" "  + containerName;
            string output = ExecuteDockerCommand(dockerFullCommand);
            return output;
        }

        public static string GetDockerStateError(string containerName)
        {
            string dockerFullCommand = "inspect -f \"{{.State.Error}}\" " + containerName;
            string output = ExecuteDockerCommand(dockerFullCommand);
            return output;
        }


        public static void RestartDockerContainer(string containerName)
        {
            ExecuteDockerCommand("restart " + containerName);
        }

        public static void RemoveDockerContainer(string containerName, bool force)
        {
            if (force)
            {
                ExecuteDockerCommand("rm -f " + containerName, true);
            }
            else
            {
                ExecuteDockerCommand("rm " + containerName, true);
            }
        }

        public static void RemoveDockerImage(string imageName, bool force)
        {
            if (force)
            {
                ExecuteDockerCommand("rmi -f " + imageName, true);
            }
            else
            {
                ExecuteDockerCommand("rmi " + imageName, true);
            }
        }

        public static string FindIpDockerContainer(string containerName, string networkName = "nat", int retryCount = 1)
        {
            string commandToFindIp = string.Empty;
            string ip = string.Empty;
            for (int i = 0; i < retryCount; i++)
            {
                commandToFindIp = "inspect -f \"{{.NetworkSettings.Networks.nat.IPAddress}}\" " + containerName;
                ip = ExecuteDockerCommand(commandToFindIp).Trim();
                if(!string.IsNullOrWhiteSpace(ip))
                {
                    break;
                }
                Trace.WriteLine("Failed to get IP Address in attempt" + (i+1));                
            }

            if(string.IsNullOrWhiteSpace(ip))
            {
                Trace.WriteLine(string.Format("Unable to obtain ip address of container {0} after {1} attempts.", containerName, retryCount));
            }
            return ip;
        }

        public static void PrintDockerProcessStats(string message)
        {
            Trace.WriteLine("Docker PS Stats at " + message);
            ExecuteDockerCommand("ps -a");
        }
    }
}
