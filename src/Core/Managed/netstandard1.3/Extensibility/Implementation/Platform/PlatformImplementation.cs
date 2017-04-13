namespace Microsoft.ApplicationInsights.Extensibility.Implementation.Platform
{
    using System;

    /// <summary>
    /// The .Net standard 1.3 implementation of the <see cref="IPlatform"/> interface.
    /// </summary>
    internal sealed class PlatformImplementation : PlatformImplementationBase
    {
        /// <summary>
        /// The directory where the configuration file might be found.
        /// </summary>
        protected override string ConfigurationXmlDirectory
        {
            get
            {
                return AppContext.BaseDirectory;
            }
        }
    }
}
