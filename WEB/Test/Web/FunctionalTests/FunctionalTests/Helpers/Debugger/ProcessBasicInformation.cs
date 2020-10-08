// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessBasicInformation.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------

namespace Functional.Helpers.Debugger
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Represents PROCESS_BASIC_INFORMATION ntdll structure
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProcessBasicInformation
    {
        public IntPtr ExitStatus;
        public IntPtr PebBaseAddress;
        public IntPtr AffinityMask;
        public IntPtr BasePriority;
        public UIntPtr UniqueProcessId;
        public IntPtr InheritedFromUniqueProcessId;

        public int Size
        {
            get { return Marshal.SizeOf(typeof(ProcessBasicInformation)); }
        }
    }
}
