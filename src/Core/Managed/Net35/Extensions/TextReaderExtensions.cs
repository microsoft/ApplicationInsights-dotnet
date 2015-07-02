namespace System.IO
{
    using System.Threading.Tasks;

    internal static class TextReaderExtensions
    {
        public static Task<string> ReadLineAsync(this TextReader textReader)
        {
            string output = textReader.ReadLine();
            return TaskEx.FromResult(output);
        }

        public static Task<string> ReadToEndAsync(this TextReader textReader)
        {
            string output = textReader.ReadToEnd();
            return TaskEx.FromResult(output);
        }
    }
}
