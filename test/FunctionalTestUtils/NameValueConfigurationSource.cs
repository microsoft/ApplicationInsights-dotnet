namespace FunctionalTestUtils
{
    using Microsoft.Framework.ConfigurationModel;
    using System;
    using System.Collections.Generic;

    public class NameValueConfigurationSource : IConfigurationSource
    {
        private Dictionary<string, string> _config = new Dictionary<string, string>();

        public NameValueConfigurationSource()
        {
        }

        public void Load()
        {
            //should already be loaded
        }

        public IEnumerable<string> ProduceSubKeys(IEnumerable<string> earlierKeys, string prefix, string delimiter)
        {
            throw new NotImplementedException();
        }

        public void Set(string key, string value)
        {
            _config.Add(key, value);
        }

        public bool TryGet(string key, out string value)
        {
            return _config.TryGetValue(key, out value);
        }
    }
}