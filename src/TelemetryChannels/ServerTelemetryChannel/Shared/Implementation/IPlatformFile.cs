namespace Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.Implementation
{
    using System;
    using System.IO;
    
    internal interface IPlatformFile
    {
        string Name { get; }

        string Extension { get; }

        long Length { get; }

        DateTimeOffset DateCreated { get; }
        
        void Delete();

        void Rename(string newName);
        
        Stream Open();
    }
}
