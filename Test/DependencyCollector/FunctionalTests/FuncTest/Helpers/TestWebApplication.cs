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
    using System.Threading;

    using FuncTest.IIS;
    using System.Reflection;
    using System.IO;

    /// <summary>The test web application.</summary>
    internal class TestWebApplication
    {
        #region Properties

        private string baseExecutingDir = string.Empty;

        /// <summary>Gets the app folder.</summary>
        internal string AppFolder
        {
            get
            {
                if (string.IsNullOrEmpty(baseExecutingDir))
                { 
                    string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                    UriBuilder uri = new UriBuilder(codeBase);
                    string path = Uri.UnescapeDataString(uri.Path).Replace('/', Path.DirectorySeparatorChar);
                    baseExecutingDir = path.Substring(0, path.LastIndexOf(Path.DirectorySeparatorChar));
                }
                return string.Join(Path.DirectorySeparatorChar.ToString(), new string[2]{baseExecutingDir, this.AppName});
            }
        }

        /// <summary>Gets or sets the app name.</summary>
        internal string AppName { get; set; }

        /// <summary>Gets the external call.</summary>
        internal string ExternalCall { get; private set; }

        /// <summary>Gets or sets a value indicating whether is first test.</summary>
        internal bool IsFirstTest { get; set; }

        /// <summary>Gets or sets a value indicating whether is red field app.</summary>
        internal bool IsRedFieldApp { get; set; }

        /// <summary>Gets the pool.</summary>
        internal IisApplicationPool Pool { get; private set; }

        /// <summary>Gets or sets the port.</summary>
        internal int Port { get; set; }

        /// <summary>Gets the web application.</summary>
        internal IisWebApplication WebApplication { get; private set; }

        /// <summary>Gets the web site.</summary>
        internal IisWebSite WebSite { get; private set; }

        /// <summary>Gets the web site name.</summary>
        internal string WebSiteName
        {
            get
            {
                return this.AppName + "TestSite";
            }
        }

        /// <summary>Gets the pool name.</summary>
        private string PoolName
        {
            get
            {
                return this.AppName + "TestPool";
            }
        }

        /// <summary>Gets the web app name.</summary>
        private string WebAppName
        {
            get
            {
                return this.AppName + "App";
            }
        }

        #endregion

        #region Methods

        /// <summary>The deploy.</summary>
        /// <param name="enableWin32Mode">The enable Win 32 Mode.</param>
        internal void Deploy(bool enableWin32Mode = false)
        {
            try
            {
                this.Pool = new IisApplicationPool(this.PoolName, enable32BitAppOnWin64: enableWin32Mode);
                this.WebSite = new IisWebSite(this.WebSiteName, this.AppFolder, this.Port, this.Pool);
                this.ExternalCall = string.Format("http://localhost:{0}/ExternalCalls.aspx", this.Port);
                this.IsFirstTest = true;
                if (Directory.Exists(this.AppFolder))
                    ACLTools.GetEveryoneAccessToPath(this.AppFolder);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while attempting to deploy application {0}: {1}. Tests will not continue as they are guaranteed to fail.", this.AppName, ex);
                throw ex;
            }
        }

        /// <summary>The do test.</summary>
        /// <param name="action">The action.</param>
        /// <param name="instrumentRedApp">Whether red app needs to be instrumented or not.</param>
        internal void DoTest(Action<TestWebApplication> action, bool instrumentRedApp = true)
        {
            if (this.IsFirstTest)
            {
                this.IsFirstTest = false;
                this.ExecuteAnonymousRequest("?type=invalid&count=1");
            }

            action(this);
        }

        /// <summary>The execute anonymous request.</summary>
        /// <param name="pageName">Page to request</param>
        /// <param name="queryString">The query string.</param>
        internal string ExecuteAnonymousRequest(string pageName, string queryString)
        {
            string url = string.Format("http://localhost:{0}/{1}?{2}", this.Port, pageName, queryString);

            string response;
            RequestHelper.ExecuteAnonymousRequest(url, out response);
            return response;
        }

        /// <summary>The execute anonymous request.</summary>
        /// <param name="queryString">The query string.</param>
        internal string ExecuteAnonymousRequest(string queryString)
        {
            string url = this.ExternalCall + queryString;

            string response;
            RequestHelper.ExecuteAnonymousRequest(url, out response);
            return response;
        }

        /// <summary>The remove.</summary>
        internal void Remove()
        {
            // this.WebApplication.Remove();
            this.WebSite.Remove();
            this.Pool.Remove();
        }

        #endregion
    }
}