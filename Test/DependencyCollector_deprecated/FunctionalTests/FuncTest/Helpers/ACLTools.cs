using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FuncTest.Helpers
{
    public static class ACLTools
    {
        public static void GetEveryoneAccessToPath(string path)
        {
            DirectorySecurity directoryAccessControl = Directory.GetAccessControl(path);
            SecurityIdentifier everyone = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            directoryAccessControl.AddAccessRule(new FileSystemAccessRule(everyone, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            Directory.SetAccessControl(path, directoryAccessControl);
        }
    }
}
