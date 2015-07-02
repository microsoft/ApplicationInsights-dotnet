namespace System.IO
{
    using System.Threading.Tasks;

    internal static class TextWriterExtensions
    {
        public static Task WriteAsync(this TextWriter textWriter, string value)
        {
            textWriter.Write(value);
            return TaskEx.FromResult((object)null);
        }

        public static Task WriteLineAsync(this TextWriter textWriter, string value)
        {
            textWriter.WriteLine(value);
            return TaskEx.FromResult((object)null);
        }
    }
}