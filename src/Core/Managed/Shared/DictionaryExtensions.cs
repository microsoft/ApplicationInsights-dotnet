namespace Microsoft.ApplicationInsights
{
    using System.Collections.Generic;
    using System.Diagnostics;

    internal static class DictionaryExtensions
    {
        /// <summary>
        /// Adds elements of the second IDictionary into the first one, 
        /// if there are any duplicate keys in the second dictionary, 
        /// ignores them and does not overwrite value.
        /// </summary>
        /// <param name="first">IDictionary to merge second into.</param>
        /// <param name="second">IDictionary to add into the first.</param>
        public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            Debug.Assert(first != null, "First IDictionary must not be null");
            Debug.Assert(second != null, "Second IDictionary must not be null");

            foreach (var item in second)
            {
                if (!first.ContainsKey(item.Key))
                {
                    first.Add(item);
                }
            }
        }
    }
}
