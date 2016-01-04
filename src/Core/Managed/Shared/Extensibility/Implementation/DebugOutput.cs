// <copyright file="DebugOutput.cs" company="Microsoft">
// Copyright © Microsoft. All Rights Reserved.
// </copyright>

#define DEBUG

namespace Microsoft.ApplicationInsights.Extensibility.Implementation
{
    using System;
    using System.Diagnostics;
    using Microsoft.ApplicationInsights.Extensibility;

    internal class DebugOutput : IDebugOutput
    {
        public void WriteLine(string message)
        {
#if CORE_PCL
            Debug.WriteLine(message);
#else
            Debugger.Log(0, "category", message + Environment.NewLine);
#endif
        }

        public bool IsLogging()
        {
#if CORE_PCL
            return true;
#else
            return Debugger.IsLogging();
#endif
        }

        public bool IsAttached()
        {
            return Debugger.IsAttached;
        }
    }
}
