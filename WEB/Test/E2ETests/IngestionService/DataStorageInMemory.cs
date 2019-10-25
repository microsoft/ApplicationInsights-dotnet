namespace IngestionService
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class DataStorageInMemory
    {
        internal static ConcurrentDictionary<string, List<string>> itemsDictionary = new ConcurrentDictionary<string, List<string>>();

        public DataStorageInMemory()
        {
        }

        public void SaveDataItem(
            string instrumentationKey,
            string data)
        {
            if (true == string.IsNullOrWhiteSpace(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            if (true == string.IsNullOrWhiteSpace("data"))
            {
                throw new ArgumentNullException("data");
            }

            List<string> items;
            if (itemsDictionary.TryGetValue(instrumentationKey, out items))
            {
                items.Add(data);
            }
            else
            {
                items = new List<string>();
                items.Add(data);
                itemsDictionary.TryAdd(instrumentationKey, items);
            }
        }

        public IEnumerable<string> GetItemIds(
            string instrumentationKey)
        {
            List<string> items = null;
            if (itemsDictionary.TryGetValue(instrumentationKey, out items))
            {
                return items;
            }
            else
            {
                return new List<string>();
            }
        }

        public IEnumerable<string> DeleteItems(
            string instrumentationKey)
        {
            try
            {
                var deletedItems = new List<string>();
                List<string> items;
                if (itemsDictionary.TryGetValue(instrumentationKey, out items))
                {
                    foreach (var item in items)
                    {
                        deletedItems.Add(item.ToString().Substring(76, 124));
                    }
                    items.Clear();
                }

                return deletedItems;
            }
            catch(Exception ex)
            {
                var errors = new List<string>();
                errors.Add(ex.Message);
                errors.Add(ex.ToString());
                return errors;
            }
        }
    }
}