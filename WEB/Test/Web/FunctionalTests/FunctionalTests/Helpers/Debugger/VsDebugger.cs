// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VsDebugger.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------

namespace Functional.Helpers.Debugger
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Threading;


    public static class VsDebugger
    {
        /// <summary>
        /// Attaches managed Visual Studio debugger to the target process
        /// </summary>
        /// <param name="visualStudioProcess">Visual studio process object</param>
        /// <param name="applicationProcess">Application process object</param>
        public static void AttachManagedTo(
            this Process visualStudioProcess,
            Process applicationProcess)
        {
            var engines = new[] { "Managed" };
            AttachTo(visualStudioProcess, applicationProcess, engines);
        }

        /// <summary>
        /// Attaches managed and native Visual Studio debuggers to the target process
        /// </summary>
        /// <param name="visualStudioProcess">Visual studio process object</param>
        /// <param name="applicationProcess">Application process object</param>
        public static void AttachMixedTo(
            this Process visualStudioProcess,
            Process applicationProcess)
        {
            var engines = new[] { "Managed", "Native" };
            AttachTo(visualStudioProcess, applicationProcess, engines);
        }

        /// <summary>
        /// Attaches Visual Studio debugger to the target process
        /// </summary>
        /// <param name="visualStudioProcess">Visual studio process object</param>
        /// <param name="applicationProcess">Application process object</param>
        /// <param name="engines">List of debug engines to use</param>
        private static void AttachTo(
            this Process visualStudioProcess,
            Process applicationProcess,
            string[] engines)
        {
            MessageFilter.Register();

            EnvDTE._DTE visualStudioInstance;

            if (true == TryGetVsInstance(visualStudioProcess.Id, out visualStudioInstance))
            {
                EnvDTE.Processes procs = visualStudioInstance.Debugger.LocalProcesses;

                EnvDTE80.Process2 processToAttachTo = null;
                foreach (EnvDTE80.Process2 process in procs)
                {
                    if (process.ProcessID == applicationProcess.Id)
                    {
                        processToAttachTo = process;
                    }
                }
                
                if (null != processToAttachTo)
                {
                    for (int i = 0; i < 6; ++i)
                    {
                        if (TryAttach(processToAttachTo, engines))
                        {
                            break;
                        }

                        if (i < 5)
                        {
                            Thread.Sleep(5000);
                        }
                        else
                        {
                            // NB! We could not attach automatically. This is the right moment to do it.
                            // Do this manually now. You want to attach to iisexpess.
                            Debugger.Break();
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException(
                        "Visual Studio process cannot find specified application '" + applicationProcess.Id + "'");
                }
            }

            MessageFilter.Revoke();
        }

        private static bool TryAttach(EnvDTE80.Process2 process, string[] engines)
        {
            try
            {
                process.Attach2(engines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryGetVsInstance(int processId, out EnvDTE._DTE instance)
        {
            var numFetched = IntPtr.Zero;
            IRunningObjectTable runningObjectTable;
            IEnumMoniker monikerEnumerator;
            var monikers = new IMoniker[1];

            GetRunningObjectTable(0, out runningObjectTable);
            runningObjectTable.EnumRunning(out monikerEnumerator);
            monikerEnumerator.Reset();

            while (0 == monikerEnumerator.Next(1, monikers, numFetched))
            {
                IBindCtx ctx;
                CreateBindCtx(0, out ctx);

                string runningObjectName;
                monikers[0].GetDisplayName(ctx, null, out runningObjectName);

                object runningObjectVal;
                runningObjectTable.GetObject(monikers[0], out runningObjectVal);

                if (runningObjectVal is EnvDTE._DTE && runningObjectName.StartsWith("!VisualStudio"))
                {
                    int currentProcessId = int.Parse(runningObjectName.Split(':')[1]);

                    if (currentProcessId == processId)
                    {
                        instance = (EnvDTE._DTE)runningObjectVal;
                        return true;
                    }
                }
            }

            instance = null;
            return false;
        }

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);
    }
}
