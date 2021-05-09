namespace Microsoft.ApplicationInsights.TestFramework.Extensibility.Implementation.Tracing.SelfDiagnosticsInternals
{
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using Microsoft.ApplicationInsights.Extensibility.Implementation.Tracing.SelfDiagnosticsInternals;
    using Xunit;

    public class MemoryMappedFileHandlerTest
    {
        public static readonly byte[] MessageOnNewFile = Encoding.UTF8.GetBytes("Successfully opened file.\n");

        [Fact]
        public void MemoryMappedFileHandler_Success()
        {
            var fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName) + "."
                    + Process.GetCurrentProcess().Id + ".log";
            var fileSize = 1024;
            using (var handler = new MemoryMappedFileHandler())
            {
                handler.CreateLogFile(fileName, fileSize);

                var stream = handler.GetStream();
                stream.Write(MessageOnNewFile, 0, MessageOnNewFile.Length);
            }

            var actualBytes = ReadFile(fileName, MessageOnNewFile.Length);

            Assert.Equal(MessageOnNewFile, actualBytes);
        }

        private static byte[] ReadFile(string fileName, int byteCount)
        {
            byte[] actualBytes = new byte[byteCount];
            using (var file = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                file.Read(actualBytes, 0, byteCount);
            }
            return actualBytes;
        }
    }
}
