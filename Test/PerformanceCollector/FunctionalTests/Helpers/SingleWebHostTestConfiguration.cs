// -----------------------------------------------------------------------
// <copyright file="SingleWebHostTestConfiguration.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2014
// </copyright>
// <author>Sergei Nikitin: sergeyni@microsoft.com</author>
// <summary></summary>
// -----------------------------------------------------------------------

namespace Functional.Helpers
{
    using System;
    using IisExpress;

    /// <summary>
    /// Single Web Host test configuration class
    /// </summary>
    public class SingleWebHostTestConfiguration
    {
        private const string LocalhostUriTemplate = "http://localhost:{0}";
        private readonly IisExpressConfiguration webHostConfiguration;

        public SingleWebHostTestConfiguration(
            IisExpressConfiguration webHostConfiguration)
        {
            if (null == webHostConfiguration)
            {
                throw new ArgumentNullException("webHostConfiguration");
            }

            this.webHostConfiguration = webHostConfiguration;
        }

        public IisExpressConfiguration WebHostConfig
        {
            get { return webHostConfiguration; }
        }

        public bool AttachDebugger { get; set; }

        public string ApplicationUri
        {
            get { return string.Format(LocalhostUriTemplate, webHostConfiguration.Port); }
        }

        public int TelemetryListenerPort { get; set; }

        public int QuickPulseListenerPort { get; set; }

        public string TelemetryListenerUri
        {
            get { return string.Format(LocalhostUriTemplate, TelemetryListenerPort) + "/v2/track/"; }
        }

        public string QuickPulseListenerUri
        {
            get { return string.Format(LocalhostUriTemplate, QuickPulseListenerPort) + "/QuickPulseService.svc/"; }
        }

        public string IKey { get; set; }
    }
}