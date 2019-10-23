// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SingleWebHostTestBase.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// --------------------------------------------------------------------------------------------------------------------

namespace Functional.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Xml.Linq;
    using IisExpress;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Base class for single host functional test
    /// </summary>
    public abstract class SingleWebHostTestBase
    {
        protected IisExpress Server { get; private set; }

        protected HttpClient HttpClient { get; private set; }

        protected TelemetryHttpListenerObservable Listener { get; private set; }

        protected AppIdRequestHttpListener AppIdRequestListener { get; private set; }

        protected SingleWebHostTestConfiguration Config { get; private set; }

        protected EtwEventSession EtwSession { get; private set; }

        protected void StartWebAppHost(
            SingleWebHostTestConfiguration configuration)
        {
            if (null == configuration)
            {
                throw new ArgumentNullException("configuration");
            }

            this.Config = configuration;

            this.Server = IisExpress.Start(
                configuration.WebHostConfig,
                configuration.AttachDebugger);

            this.HttpClient = new HttpClient
            {
                BaseAddress = new Uri(configuration.ApplicationUri)
            };

            this.Listener = new TelemetryHttpListenerObservable(configuration.TelemetryListenerUri);
            this.Listener.Start();

            this.AppIdRequestListener = new AppIdRequestHttpListener(configuration.AppIdListenerUri);
            this.AppIdRequestListener.Start();

            this.EtwSession = new EtwEventSession();
            this.EtwSession.Start();
        }
   
        protected void StopWebAppHost(bool treatTraceErrorsAsFailures = true)
        {
            this.Listener.Stop();
            this.AppIdRequestListener.Stop();
            this.Server.Stop();
            this.HttpClient.Dispose();
            this.AppIdRequestListener.Dispose();

            if (treatTraceErrorsAsFailures && this.EtwSession.FailureDetected || this.Listener.FailureDetected)
            {
                Assert.Fail("Read test output. There are errors found in application trace.");
            }

            this.EtwSession.Stop();
        }

        protected static void UpdateAppConfigSettings(
            IEnumerable<KeyValuePair<string, string>> settings,
            string destAppConfiguration)
        {
            var configDom = XDocument.Load(destAppConfiguration);
            var configuraitonNode = configDom.Element("configuration");
            if (null == configuraitonNode)
            {
                throw new InvalidOperationException("configuration node does not exist");
            }

            var appSettings = configuraitonNode.Element("appSettings");
            if (null == appSettings)
            {
                appSettings = new XElement("appSettings");
                configuraitonNode.Add(appSettings);
            }

            foreach (var setting in settings)
            {
                appSettings.Add(
                    new XElement("add",
                    new XAttribute("key", setting.Key),
                    new XAttribute("value", setting.Value)));
            }

            configDom.Save(destAppConfiguration);
        }
    }
}
