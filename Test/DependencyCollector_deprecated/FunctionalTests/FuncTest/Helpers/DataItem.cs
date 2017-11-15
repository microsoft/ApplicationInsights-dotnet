using Newtonsoft.Json.Linq;

namespace FuncTest.Helpers
{    

    /// <summary>
    /// Represents data structure send to endpoint
    /// </summary>
    public class DataItem
    {
        public string Uri { get; private set; }

        public JToken Content { get; private set; }

        public DataItem(string uri, JToken content)
        {
            this.Uri = uri;
            this.Content = content;
        }

        public string GetFieldValue(string path)
        {
            var result = string.Empty;

            var token = this.Content.SelectToken(path);
            if (token != null)
            {
                result = token.ToString();
            }

            return result;
        }
    }
}
