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
            EnvDTE._DTE visualStudioInstance;

            if (true == TryGetVsInstance(visualStudioProcess.Id, out visualStudioInstance))
            {
                var processToAttachTo =
                    visualStudioInstance.Debugger.LocalProcesses
                        .Cast<EnvDTE80.Process2>()
                        .FirstOrDefault(
                            process => process.ProcessID == applicationProcess.Id);

                if (null != processToAttachTo)
                {
                    processToAttachTo.Attach2(engines);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Visual Studio process cannot find specified application '" + applicationProcess.Id + "'");
                }
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
