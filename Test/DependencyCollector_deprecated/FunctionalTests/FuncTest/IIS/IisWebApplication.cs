// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IISWebApplication.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   Defines the IISWebApplication type.
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
    public sealed class IisWebApplication : IDisposable
    {
        /// <summary>
        /// The server manager.
        /// </summary>
        private readonly ServerManager serverManager;

        /// <summary>
        /// The app.
        /// </summary>
        private readonly Application app;

        /// <summary>
        /// The IIS site.
        /// </summary>
        private readonly IisWebSite iisSite;

        /// <summary>
        /// Gets the IIS Application pool.
        /// </summary>
        private IisApplicationPool currentPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="IisWebApplication"/> class.
        /// </summary>
        /// <param name="appName">
        /// The app name.
        /// </param>
        /// <param name="physicalPath">
        /// The physical Path.
        /// </param>
        /// <param name="site">
        /// The site.
        /// </param>
        /// <param name="pool">
        /// The pool.
        /// </param>
        public IisWebApplication(string appName, string physicalPath, IisWebSite site, IisApplicationPool pool)
        {
            try
            {
                this.Name = appName;
                this.IisPath = site.Name + this.Name;
                this.currentPool = pool;
                this.iisSite = site;

                this.serverManager = new ServerManager();
                string relativePath = Path.Combine(Environment.CurrentDirectory, physicalPath);
                string absolutePath = Path.GetFullPath(relativePath);

                Site targetSite = this.serverManager.Sites[site.Name];
                if (targetSite != null)
                {
                    if (targetSite.Applications[this.Name] != null)
                    {
                        // removing if application already exists - because it can exists with the wrong physical path.
                        this.Remove();

                        // refreshing target site.
                        targetSite = this.serverManager.Sites[site.Name];
                    }

                    this.app = targetSite.Applications.Add(this.Name, absolutePath);
                    this.app.ApplicationPoolName = pool.Name;
                }

                this.serverManager.CommitChanges();

                Trace.TraceInformation(
               "IISWebApplication {0} created successfully.", appName);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while creating application: {0}. The exception thrown was {1}", this.iisSite.Name, ex.Message);
            }
        }

        /// <summary>
        /// Gets the Name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the Display Name (Name without the heading '/')
        /// </summary>
        public string DisplayName
        {
            get
            {
                return this.Name.TrimStart('/');
            }
        }

        /// <summary>
        /// Gets the App path.
        /// </summary>
        public string AppPath
        {
            get
            {
                return this.app.Path;
            }
        }

        /// <summary>
        /// Gets the port.
        /// </summary>
        public int Port
        {
            get
            {
                return this.iisSite.Port;
            }
        }

        /// <summary>
        /// Gets the IIS path.
        /// </summary>
        public string IisPath { get; private set; }

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
                Application appToChange = this.serverManager.Sites[this.iisSite.Name].Applications[this.Name];
                appToChange.ApplicationPoolName = value.Name;
                this.currentPool = value;
                this.serverManager.CommitChanges();
            }
        }

        /// <summary>
        /// Adds app to IIS if not exists (constructor adds site by default).
        /// </summary>
        public void AddToIisIfNotExists()
        {
            Site currentSite = this.serverManager.Sites[this.iisSite.Name];
            if (currentSite != null && currentSite.Applications[this.Name] == null)
            {
                this.serverManager.Sites[this.iisSite.Name].Applications.Add(this.app);
            }

            this.serverManager.CommitChanges();
        }

        /// <summary>
        /// Removes the App from IIS.
        /// </summary>
        public void Remove()
        {
            try
            {
                Application appToDelete = this.serverManager.Sites[this.iisSite.Name].Applications[this.Name];
                appToDelete.Delete();
                this.serverManager.CommitChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while removing application: {0}. The exception thrown was {1}", this.iisSite.Name, ex.Message);
            }

        }

        /// <summary>
        /// Disposes the instance of IIS Web Application and frees the underlying resources, can reduce the amount of the memory leak in Server Manager
        /// </summary>
        public void Dispose()
        {
            this.serverManager.Dispose();
        }
    }
}
