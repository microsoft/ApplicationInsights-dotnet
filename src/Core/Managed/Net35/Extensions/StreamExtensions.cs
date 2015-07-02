namespace System
{
    using System.IO;
    using System.Threading.Tasks;

    internal static class StreamExtensions
    {
        public static Task WriteAsync(this Stream stream, byte[] buffer, int index, int count)
        {
            return Task.Factory.FromAsync(
                (asyncCallback, asyncState) => stream.BeginWrite(buffer, index, count, asyncCallback, asyncState),
                asyncResult => stream.EndWrite(asyncResult),
                null);
        }
    }
}