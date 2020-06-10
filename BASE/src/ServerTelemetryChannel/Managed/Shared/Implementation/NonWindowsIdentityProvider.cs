namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Shared.Implementation
{
    using System;

    internal class NonWindowsIdentityProvider : IIdentityProvider
    {
        string IIdentityProvider.GetName()
        {
            if (Environment.UserName != null)
            {
                return Environment.UserName;
            }

            return string.Empty;
        }
    }
}
