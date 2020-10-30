// -----------------------------------------------------------------------
// <copyright file="ApplicationInsightsTarget.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation. 
// All rights reserved.  2013
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.ApplicationInsights.NLogTarget
{
    /// <summary>
    /// Converts from NLog Object-properties to ApplicationInsight String-properties
    /// </summary>
    class StringDictionaryConverter : IDictionary<string, object>
    {
        private readonly IDictionary<string, string> _wrapped;

        public StringDictionaryConverter(IDictionary<string, string> wrapped)
        {
            _wrapped = wrapped;
        }

        public object this[string key] { get => _wrapped[key]; set => _wrapped[key] = SafeValueConverter(value); }

        public ICollection<string> Keys => _wrapped.Keys;

        public ICollection<object> Values => new List<object>(_wrapped.Values);

        public int Count => _wrapped.Count;

        public bool IsReadOnly => _wrapped.IsReadOnly;

        public void Add(string key, object value)
        {
            _wrapped.Add(key, SafeValueConverter(value));
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _wrapped.Add(new KeyValuePair<string, string>(item.Key, SafeValueConverter(item.Value)));
        }

        public void Clear()
        {
            _wrapped.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _wrapped.Contains(new KeyValuePair<string, string>(item.Key, SafeValueConverter(item.Value)));
        }

        public bool ContainsKey(string key)
        {
            return _wrapped.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            foreach (var item in _wrapped)
            {
                array[arrayIndex++] = new KeyValuePair<string, object>(item.Key, item.Value);
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return AsEnumerable().GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _wrapped.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return _wrapped.Remove(new KeyValuePair<string, string>(item.Key, SafeValueConverter(item.Value)));
        }

        public bool TryGetValue(string key, out object value)
        {
            if (_wrapped.TryGetValue(key, out var stringValue))
            {
                value = stringValue;
                return true;
            }
            
            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_wrapped).GetEnumerator();
        }

        private IEnumerable<KeyValuePair<string, object>> AsEnumerable()
        {
            foreach (var item in _wrapped)
                yield return new KeyValuePair<string, object>(item.Key, item.Value);
        }

        private static string SafeValueConverter(object value)
        {
            try
            {
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
