// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IisWebSite.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   Defines the IisWebSite type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace FuncTest.IIS
{
    using System;
    using System.IO;

    using Microsoft.Web.Administration;

    /// <summary>
    /// The IIS web application.
    /// </summary>
    public sealed class IisWebSite : IDisposable
    {
        /// <summary>
        /// The default web site name.
        /// </summary>
        private const string DefaultWebSiteName = "Default Web Site";

        /// <summary>
        /// Gets the IIS Web Site pool.
        /// </summary>
        private readonly ServerManager serverManager;

        /// <summary>
        /// Gets the IIS Web Site pool.
        /// </summary>
        private readonly Site site;

        /// <summary>
        /// Gets the IIS Application pool.
        /// </summary>
        private IisApplicationPool currentPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="IisWebSite"/> class.
        /// </summary>
        /// <param name="iisSite">
        /// The IIS site.
        /// </param>
        public IisWebSite(Site iisSite)
        {
            this.site = iisSite;
            this.currentPool = new IisApplicationPool(iisSite.Applications[0].ApplicationPoolName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IisWebSite"/> class. 
        /// </summary>
        /// <param name="siteName">
        /// The site Name.
        /// </param>
        /// <param name="sitePhysicalPath">
        /// The site Physical Path.
        /// </param>
        /// <param name="sitePort">
        /// The site Port.
        /// </param>
        /// <param name="pool">
        /// The pool.
        /// </param>
        public IisWebSite(string siteName, string sitePhysicalPath, int sitePort, IisApplicationPool pool)
        {
            try
            {
                this.serverManager = new ServerManager();
                string relativePath = Path.Combine(Environment.CurrentDirectory, sitePhysicalPath);
                string absolutePath = Path.GetFullPath(relativePath);

                this.Port = sitePort;
                this.Name = siteName;
                if (this.serverManager.Sites[siteName] != null)
                {
                    // if site exists we need to remove it because it may contain wrong physical path.
                    this.Remove();                    
                }

                this.site = this.serverManager.Sites.Add(siteName, absolutePath, sitePort);

                this.site.ServerAutoStart = true;
                this.site.Applications[0].ApplicationPoolName = pool.Name;
                this.currentPool = pool;

                this.serverManager.CommitChanges();

                Trace.TraceInformation(
               "IISWebSite {0} deployed successfully.", siteName);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while deploying IIS Website: {0}. The exception thrown was {1}", siteName, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the default web site.
        /// </summary>
        public static IisWebSite DefaultWebSite
        {
            get
            {
                ServerManager srvMgr = new ServerManager();
                Site defaultSite = srvMgr.Sites[DefaultWebSiteName];
                srvMgr.Dispose();
                return new IisWebSite(defaultSite);
            }
        }

        /// <summary>
        /// Gets or sets the current pool.
        /// </summary>
        public IisApplicationPool CurrentPool
        {
            get
            {
                return this.currentPool;
            }

            set
            {
                Site siteToChange = this.serverManager.Sites[this.Name];
                siteToChange.Applications[0].ApplicationPoolName = value.Name;
                this.currentPool = value;
                this.serverManager.CommitChanges();
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Gets or sets the physical path.
        /// </summary>
        public string PhysicalPath
        {
            get
            {
                return this.site.Applications[0].VirtualDirectories[0].PhysicalPath;
            }

            set
            {
                Site siteToChange = this.serverManager.Sites[this.Name];
                siteToChange.Applications[0].VirtualDirectories[0].PhysicalPath = value;
                this.serverManager.CommitChanges();
            }
        }

        /// <summary>
        /// Adds binding.
        /// </summary>
        /// <param name="bindingInfo">
        /// The binding info.
        /// </param>
        /// <param name="bindingProtocol">
        /// The binding protocol.
        /// </param>
        public void AddBinding(string bindingInfo, string bindingProtocol)
        {
            Site siteToChange = this.serverManager.Sites[this.Name];
            siteToChange.Bindings.Add(bindingInfo, bindingProtocol);
            this.serverManager.CommitChanges();
        }

        /// <summary>
        /// Clears all bindings.
        /// </summary>
        public void ClearBindings()
        {
            if (this.site.Bindings.AllowsClear)
            {
                Site siteToChange = this.serverManager.Sites[this.Name];
                siteToChange.Bindings.Clear();
                this.serverManager.CommitChanges();
            }
        }

        /// <summary>
        /// Adds site to IIS if not exists (constructor adds site by default).
        /// </summary>
        public void AddToIisIfNotExists()
        {
            if (this.serverManager.Sites[this.Name] == null)
            {
                this.serverManager.Sites.Add(this.site);
            }

            this.serverManager.CommitChanges();
        }

        /// <summary>
        /// Removes the Site from IIS.
        /// </summary>
        public void Remove()
        {
            try
            {
                Site siteToDelete = this.serverManager.Sites[this.Name];
                if (siteToDelete != null)
                {
                    siteToDelete.Delete();
                }
                this.serverManager.CommitChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while removing website: {0}. The exception thrown was {1}", this.Name, ex.Message);
            }
        }

        /// <summary>
        /// Disposes the instance of IIS Web Site and frees the underlying resources, can reduce the amount of the memory leak in Server Manager
        /// </summary>
        public void Dispose()
        {
            this.serverManager.Dispose();
            this.currentPool.Dispose();
        }
    }
}
