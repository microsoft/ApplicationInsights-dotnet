// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IisApplicationPool.cs" company="Microsoft">
//   Copyright (c) Microsoft Corporation.  All rights reserved
// </copyright>
// <summary>
//   Defines the IisApplicationPool type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace FuncTest.IIS
{
    using System;

    using Microsoft.Web.Administration;

    /// <summary>
    /// The IIS web application.
    /// </summary>
    public sealed class IisApplicationPool : IDisposable
    {
        /// <summary>
        /// The default managed runtime version.
        /// </summary>
        private const string DefaultManagedRuntimeVersion = "v4.0";

        /// <summary>
        /// The default pipeline version.
        /// </summary>
        private const string DefaultPipeline = "Integrated";

        /// <summary>
        /// The server manager.
        /// </summary>
        private readonly ServerManager serverManager;

        /// <summary>
        /// Gets the IIS Application pool.
        /// </summary>
        private readonly ApplicationPool iisPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="IisApplicationPool"/> class.
        /// </summary>
        /// <param name="pool">
        /// The pool.
        /// </param>
        public IisApplicationPool(ApplicationPool pool)
        {
            this.serverManager = new ServerManager();
            this.iisPool = pool;
            this.Name = pool.Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IisApplicationPool"/> class. 
        /// </summary>
        /// <param name="poolName">
        /// The pool name.
        /// </param>
        /// <param name="managedRuntimeVersion">
        /// The managed Runtime Version.
        /// </param>
        /// <param name="pipeline">
        /// The pipeline.
        /// </param>
        /// <param name="enable32BitAppOnWin64">determines if enable32BitAppOnWin64 is required for the pool</param>
        public IisApplicationPool(string poolName, string managedRuntimeVersion = DefaultManagedRuntimeVersion, string pipeline = DefaultPipeline, bool enable32BitAppOnWin64 = false)
        {
            try
            {
                this.serverManager = new ServerManager();
                this.Name = poolName;

                if (this.serverManager.ApplicationPools[poolName] != null)
                {
                    // Removing the existing pool and recreating
                    // as existing pool sometimes cause issues 
                    this.Remove();
                }
                
                    this.iisPool = this.serverManager.ApplicationPools.Add(poolName);
                    this.iisPool.ManagedRuntimeVersion = managedRuntimeVersion;
                    this.iisPool.ManagedPipelineMode = pipeline.Equals(DefaultPipeline, StringComparison.OrdinalIgnoreCase)
                                                           ? ManagedPipelineMode.Integrated
                                                           : ManagedPipelineMode.Classic;
                    this.iisPool.Enable32BitAppOnWin64 = enable32BitAppOnWin64;
                    this.iisPool.ProcessModel.IdentityType = ProcessModelIdentityType.NetworkService;

                
                this.serverManager.CommitChanges();
                Trace.TraceInformation(
            "IISApplication Pool {0} created successfully.", poolName);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while creating IIS Application Pool: {0}. The exception thrown was {1}", poolName, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the managed runtime version.
        /// </summary>
        public string ManagedRuntimeVersion
        {
            get
            {
                return this.iisPool.ManagedRuntimeVersion;
            }
        }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        public string Pipeline
        {
            get
            {
                switch (this.iisPool.ManagedPipelineMode)
                {
                    case ManagedPipelineMode.Integrated:
                        return "Integrated";
                    case ManagedPipelineMode.Classic:
                        return "Classic";
                    default:
                        return "Undefined";
                }
            }
        }

        /// <summary>
        /// Adds pool to IIS if not exists.
        /// </summary>
        public void AddToIisIfNotExists()
        {
            if (this.serverManager.ApplicationPools[this.Name] == null)
            {
                this.serverManager.ApplicationPools.Add(this.iisPool);
            }

            this.serverManager.CommitChanges();
        }

        /// <summary>
        /// Removes the pool from IIS
        /// </summary>
        public void Remove()
        {
            try
            {
                ApplicationPool poolToDelete = this.serverManager.ApplicationPools[this.Name];
                poolToDelete.Delete();
                this.serverManager.CommitChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Exception occured while removing application pool: {0}. The exception thrown was {1}", this.Name, ex.Message);
            }
        }

        /// <summary>
        /// Recycles application pool in IIS.
        /// </summary>
        public void Recycle()
        {
            ApplicationPool poolToRecycle = this.serverManager.ApplicationPools[this.Name];
            poolToRecycle.Recycle();
            this.serverManager.CommitChanges();
        }

        /// <summary>
        /// Disposes the instance of IIS Application Pool and frees the underlying resources, can reduce the amount of the memory leak in Server Manager
        /// </summary>
        public void Dispose()
        {
            this.serverManager.Dispose();
        }
    }
}
