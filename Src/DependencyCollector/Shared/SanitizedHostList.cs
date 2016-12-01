namespace Microsoft.ApplicationInsights.DependencyCollector
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Sanitized collection on host strings.
    /// </summary>
    internal class SanitizedHostList : ICollection<string>
    {
        private List<string> hostList = new List<string>();

        #region ICollection Implemenation
        public int Count
        {
            get
            {
                return this.hostList.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// We sanitize before adding to the list. We try our best to extract the host name from the passed in item and store that in the collection.
        /// </summary>
        /// <param name="item">Item to be added.</param>
        public void Add(string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                throw new ArgumentNullException("item");
            }

            Uri uriResult;
            if (Uri.TryCreate(item, UriKind.Absolute, out uriResult))
            {
                this.hostList.Add(uriResult.Host);
                return;
            }
            else if (Uri.TryCreate("http://" + item, UriKind.Absolute, out uriResult))
            {
                // If the user specified something that doesn't start with http - Let's append and try get host value that way
                this.hostList.Add(uriResult.Host);
                return;
            }

            // todo(nizarq): Do we need to move this to a resource.
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Could not add the provided item '{0}' to the list as it does not look like a URI", item));
        }

        public void Clear()
        {
            this.hostList.Clear();
        }

        public bool Contains(string item)
        {
            return this.hostList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            this.hostList.CopyTo(array, arrayIndex);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return this.hostList.GetEnumerator();
        }

        public bool Remove(string item)
        {
            return this.hostList.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.hostList.GetEnumerator();
        }
        #endregion
    }
}