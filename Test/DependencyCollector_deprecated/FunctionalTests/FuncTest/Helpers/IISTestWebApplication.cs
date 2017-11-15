// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TestWebApplication.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   The test web application.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FuncTest.Helpers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using FuncTest.IIS;

    /// <summary>The test web application.</summary>
    internal class IISTestWebApplication : TestWebApplication
    {
        private IisApplicationPool pool;
        private IisWebSite website;
        private bool isFirstTest = true;
        
        internal bool EnableWin32Mode { get; set; } = false;

        /// <summary>The deploy.</summary>
        /// <param name="enableWin32Mode">The enable Win 32 Mode.</param>
        internal override void Deploy()
        {
            try
            {
                this.pool = new IisApplicationPool(this.AppName + "TestPool", enable32BitAppOnWin64: this.EnableWin32Mode);

                this.website = new IisWebSite(this.AppName + "TestSite", this.AppFolder, this.Port, this.pool);

                if (Directory.Exists(this.AppFolder))
                {
                    ACLTools.GetEveryoneAccessToPath(this.AppFolder);
                }
                else
                {
                    Trace.TraceWarning("AppFolder {0} do not exist.", this.AppFolder);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while attempting to deploy application {0}: {1}. Tests will not continue as they are guaranteed to fail.", this.AppName, ex);
                throw;
            }
        }

        /// <summary>The do test.</summary>
        /// <param name="action">The action.</param>
        /// <param name="instrumentRedApp">Whether red app needs to be instrumented or not.</param>
        internal override void DoTest(Action<TestWebApplication> action)
        {
            if (this.isFirstTest)
            {
                this.isFirstTest = false;
                this.ExecuteAnonymousRequest("?type=invalid&count=1");
            }

            action(this);
        }

        /// <summary>The remove.</summary>
        internal override void Remove()
        {
            this.website.Remove();
            this.pool.Remove();
        }
    }
}