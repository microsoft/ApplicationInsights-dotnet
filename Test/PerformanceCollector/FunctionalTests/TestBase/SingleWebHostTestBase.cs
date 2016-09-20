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
    using System.Net.Http;
    using System.Threading.Tasks;
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

        protected HttpListenerObservable Listener { get; private set; }

        internal QuickPulseHttpListenerObservable QuickPulseListener { get; private set; }

        protected SingleWebHostTestConfiguration Config { get; private set; }

        protected EtwEventSession EtwSession { get; private set; }

        public Task<string> SendRequest(string requestPath, bool wait = true)
        {
            const int TimeoutInMs = 15000;

            string expectedRequestUrl = this.Config.ApplicationUri + "/" + requestPath;

            // spin up the application
            var client = new HttpClient();
            var requestMessage = new HttpRequestMessage { RequestUri = new Uri(expectedRequestUrl), Method = HttpMethod.Get, };

            var responseTask = client.SendAsync(requestMessage);

            if (wait)
            {
                responseTask.Wait(TimeoutInMs);

                var responseTextTask = responseTask.Result.Content.ReadAsStringAsync();
                responseTextTask.Wait(TimeoutInMs);

                return responseTextTask;
            }
            else
            {
                return null;
            }
        }

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

            this.Listener = new HttpListenerObservable(configuration.TelemetryListenerUri);
            this.Listener.Start();

            this.QuickPulseListener = new QuickPulseHttpListenerObservable(configuration.QuickPulseListenerUri);
            this.QuickPulseListener.Start();

            this.EtwSession = new EtwEventSession();
            this.EtwSession.Start();
        }
   
        protected void StopWebAppHost(bool treatTraceErrorsAsFailures = false)
        {
            this.Listener.Stop();
            this.QuickPulseListener.Stop();
            this.Server.Stop();
            this.HttpClient.Dispose();

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

        protected void LaunchAndVerifyApplication()
        {
            const string RequestPath = "aspx/TestWebForm.aspx";
            var responseTextTask = this.SendRequest(RequestPath);

            // make sure it's the correct application
            Assert.AreEqual("PerformanceCollector application", responseTextTask.Result);
        }
    }
}
