// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IIS.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   Defines the IIS type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FuncTest.IIS
{
    using System.Diagnostics;

    /// <summary>
    /// The IIS.
    /// </summary>
    public static class Iis
    {
        /// <summary>
        /// IIS Start Command Line.
        /// </summary>
        private static readonly ProcessStartInfo StartIis = new ProcessStartInfo("cmd", "/c iisreset /start");

        /// <summary>
        /// W3SVC Start Command Line.
        /// </summary>
        private static readonly ProcessStartInfo StartW3Svc = new ProcessStartInfo("cmd", "/c net start w3svc");

        /// <summary>
        /// IIS Stop Command Line.
        /// </summary>
        private static readonly ProcessStartInfo StopIis = new ProcessStartInfo("cmd", "/c iisreset /stop");

        /// <summary>
        /// Resets IIS on the box
        /// </summary>
        public static void Reset()
        {
            Stop();
            Start();
        }

        /// <summary>
        /// Starts IIS on the box.
        /// </summary>
        public static void Start()
        {
            Process process = new Process { StartInfo = StartIis };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();

            process = new Process { StartInfo = StartW3Svc };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
        }

        /// <summary>
        /// Stops IIS on the box.
        /// </summary>
        public static void Stop()
        {
            Process process = new Process { StartInfo = StopIis };
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            
            process.WaitForExit();
        }
    }
}
