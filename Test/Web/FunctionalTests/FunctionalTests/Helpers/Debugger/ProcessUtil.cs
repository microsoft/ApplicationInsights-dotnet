// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessUtil.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------

namespace Functional.Helpers.Debugger
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Process utils
    /// </summary>
    public static class ProcessUtil
    {
        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="ps">The process object</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(this Process ps)
        {
            if (null == ps)
            {
                throw new ArgumentNullException("ps");
            }

            return ps.Handle.GetParentProcess();
        }

        public static Process FindParent(
            this Process ps,
            Func<Process, bool> pred)
        {
            if (null == ps)
            {
                throw new ArgumentNullException("ps");
            }

            if (null == pred)
            {
                throw new ArgumentNullException("pred");
            }

            var parent = ps;
            while (true)
            {
                parent = parent.Handle.GetParentProcess();
                if (null != parent)
                {
                    if (true != pred(parent))
                    {
                        continue;
                    }

                    return parent;
                }

                break;
            }

            return null;
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="ps">The process object</param>
        /// <param name="strProcessName">Parent process name</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process FindParentByName(
            this Process ps,
            string strProcessName)
        {
            if (null == ps)
            {
                throw new ArgumentNullException("ps");
            }

            if (true == string.IsNullOrWhiteSpace(strProcessName))
            {
                throw new ArgumentNullException("strProcessName");
            }

            return ps.FindParent(
                parent => parent.ProcessName.Equals(
                    strProcessName,
                    StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(this IntPtr handle)
        {
            var pbi = new ProcessBasicInformation();
            var returnLength = 0;

            var status = NtQueryInformationProcess(
                handle,
                0,
                ref pbi,
                pbi.Size,
                out returnLength);

            if (status != 0)
            {
                throw new Win32Exception(status);
            }

            try
            {
                return Process.GetProcessById(
                    pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }

        public static ProcessInfo Create(
            string applicationName,
            string arguments)
        {
            var startupInfo = new StartupInfo();
            var sec = new SecurityAttributes();
            sec.nLength = sec.Size;
            ProcessInformation processInfo;

            const ProcessCreationFlags CreateFlags =
                ProcessCreationFlags.CREATE_NO_WINDOW
                | ProcessCreationFlags.CREATE_SUSPENDED;

            if (true == CreateProcess(
                    applicationName,
                    arguments,
                    ref sec,
                    ref sec,
                    false,
                    (uint)CreateFlags,
                    IntPtr.Zero,
                    null,
                    ref startupInfo,
                    out processInfo))
            {

                return new ProcessInfo(
                    Process.GetProcessById(processInfo.dwProcessId),
                    processInfo);
            }

            return null;
        }

        public static void Resume(this ProcessInfo ps)
        {
            ResumeThread(ps.MainThreadHandle.DangerousGetHandle());
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref ProcessBasicInformation processInformation,
            int processInformationLength,
            out int returnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            ref SecurityAttributes lpProcessAttributes,
            ref SecurityAttributes lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint ResumeThread(IntPtr hThread);
    }
}