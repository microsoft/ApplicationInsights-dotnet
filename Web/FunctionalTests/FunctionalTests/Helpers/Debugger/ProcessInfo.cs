// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessInfo.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------
namespace Functional.Helpers.Debugger
{
    using System;
    using System.Diagnostics;

    using Microsoft.Win32.SafeHandles;

    /// <summary>
    /// Represents extended process information
    /// </summary>
    public class ProcessInfo
    {
        private readonly Process ps;
        private readonly SafeWaitHandle mainThreadHandle;

        public ProcessInfo(
            Process ps, 
            ProcessInformation psi)
        {
            if (null == ps)
            {
                throw new ArgumentNullException("ps");
            }

            this.ps = ps;
            this.mainThreadHandle = new SafeWaitHandle(
                psi.hThread, 
                true);
        }

        public SafeWaitHandle MainThreadHandle
        {
            get
            {
                return this.mainThreadHandle;
            }
        }

        public Process Process
        {
            get
            {
                return this.ps;
            }
        }
    }
}
