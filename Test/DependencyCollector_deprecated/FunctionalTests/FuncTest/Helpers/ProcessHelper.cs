// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessHelper.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   The process helper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FuncTest.Helpers
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>The process helper.</summary>
    internal static class ProcessHelper
    {
        #region Public Methods and Operators

        /// <summary>The execute process.</summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="timeout">The timeout.</param>
        public static void ExecuteProcess(string fileName, string arguments, TimeSpan timeout)
        {
            string standardOutput;
            string standardError;
            int exitCode = ExecuteProcess(fileName, arguments, timeout, out standardOutput, out standardError);

            if (exitCode != 0)
            {
                throw new Exception(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Process {0} {1} returned {2}.\r\nOutput: {3}\r\nError: {4}.", 
                        fileName, 
                        arguments, 
                        exitCode, 
                        standardOutput, 
                        standardError));
            }
        }

        /// <summary>The execute process.</summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="arguments">The arguments.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="standardOutput">The standard output.</param>
        /// <param name="standardError">The standard error.</param>
        /// <returns>The <see cref="int"/>.</returns>
        public static int ExecuteProcess(
            string fileName, 
            string arguments, 
            TimeSpan timeout, 
            out string standardOutput, 
            out string standardError)
        {
            int exitCode;

            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;

                process.Start();

                Process localProcess = process;
                using (
                    Task<bool> processWaiter =
                        Task.Factory.StartNew(() => localProcess.WaitForExit((int)timeout.TotalMilliseconds)))
                using (
                    Task<string> outputReader = Task.Factory.StartNew(
                        (Func<object, string>)ReadStream, 
                        localProcess.StandardOutput))
                using (
                    Task<string> errorReader = Task.Factory.StartNew(
                        (Func<object, string>)ReadStream, 
                        localProcess.StandardError))
                {
                    bool waitResult = processWaiter.Result;

                    if (!waitResult)
                    {
                        localProcess.Kill();
                    }

                    Task.WaitAll(outputReader, errorReader);

                    if (!waitResult)
                    {
                        throw new TimeoutException(
                            string.Format(CultureInfo.InvariantCulture, "Process wait timeout expired for {0} {1}", fileName, arguments));
                    }

                    exitCode = localProcess.ExitCode;

                    standardOutput = outputReader.Result;
                    standardError = errorReader.Result;
                }
            }

            return exitCode;
        }

        #endregion

        #region Methods

        /// <summary>The read stream.</summary>
        /// <param name="streamReader">The stream reader.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private static string ReadStream(object streamReader)
        {
            string result = ((StreamReader)streamReader).ReadToEnd();

            return result;
        }

        #endregion
    }
}