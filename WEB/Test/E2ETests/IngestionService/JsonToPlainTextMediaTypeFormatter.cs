using System;

namespace IngestionService
{
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading.Tasks;

    public class JsonToPlainTextMediaTypeFormatter : MediaTypeFormatter
    {
        public JsonToPlainTextMediaTypeFormatter()
        {
            SupportedMediaTypes.Add(
                new MediaTypeHeaderValue("application/x-json-stream"));

            SupportedEncodings.Add(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        public override bool CanReadType(Type type)
        {
            return type.Equals(typeof(string)); ;
        }

        public override bool CanWriteType(Type type)
        {
            return type.Equals(typeof(string));
        }

        public override Task<object> ReadFromStreamAsync(
            Type type,
            Stream readStream,
            HttpContent content,
            IFormatterLogger formatterLogger)
        {
            if (content.Headers.ContentEncoding.Contains("gzip"))
            {
                readStream = new GZipStream(readStream, CompressionMode.Decompress);
            }

            var encoding = SelectCharacterEncoding(content.Headers);
            using (var reader = new StreamReader(readStream, encoding))
            {
                return reader.ReadToEndAsync().ContinueWith(t => (object)t.Result);
            }
        }
    }
}