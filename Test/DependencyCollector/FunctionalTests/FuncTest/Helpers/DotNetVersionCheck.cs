// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DotNetVersionCheck.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   Helper to determine .net version
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FuncTest.Helpers
{
    using Microsoft.Win32;

    /// <summary>
    /// The dot net version check.
    /// </summary>
    public static class DotNetVersionCheck
    {
        /// <summary>
        /// The net v 4 full registry key.
        /// </summary>
        private const string NetV4FullRegistryKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

        /// <summary>
        /// Registry Key Value for .NET 4.5.
        /// </summary>
        private const int Net45ReleaseKey = 378389;

        /// <summary>
        /// Registry Key Value for .NET 4.5.1.
        /// </summary>
        private const int Net451ReleaseKey = 378575;

        /// <summary>
        /// Registry Key Value for .NET 4.6 Preview.
        /// </summary>
        private const int Net46ReleaseKey = 381029;

        /// <summary>
        /// Gets a value indicating whether Net 4.5 is installed.
        /// </summary>
        public static bool IsNet45Installed
        {
            get
            {
                return CheckDotNetVersionPresenceOnTheBox(Net45ReleaseKey);
            }
        }

        /// <summary>
        /// Gets a value indicating whether .Net 4.5.1 is installed.
        /// </summary>
        public static bool IsNet451Installed
        {
            get
            {
                return CheckDotNetVersionPresenceOnTheBox(Net451ReleaseKey);
            }
        }

        /// <summary>
        /// Gets a value indicating whether .Net 4.6 preview is installed.
        /// </summary>
        public static bool IsNet46Installed
        {
            get
            {
                return CheckDotNetVersionPresenceOnTheBox(Net46ReleaseKey);
            }
        }

        /// <summary>
        /// Validates the .NET Framework presence on the test box (4.5 and above)
        /// </summary>
        /// <param name="requiredReleaseKey">
        /// The required Release Key.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool CheckDotNetVersionPresenceOnTheBox(int requiredReleaseKey)
        {
            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(NetV4FullRegistryKey))
            {
                if (ndpKey == null)
                {
                    return false;
                }

                object releaseKeyObject = ndpKey.GetValue("Release");
                if (releaseKeyObject == null)
                {
                    return false;
                }

                if ((int)releaseKeyObject < requiredReleaseKey)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
