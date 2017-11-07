namespace IngestionService
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class DataStorage
    {
        private readonly DataStoragePathMapper mapper;

        public DataStorage(DataStoragePathMapper mapper)
        {
            if (null == mapper)
            {
                throw new ArgumentNullException("mapper");
            }

            this.mapper = mapper;
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

            var dateStamp = DateTime.UtcNow.ToString("yyyy.MM.dd.HH.mm.ss.ffff");
            const int MaxAttemptCount = 50;
            for (int idx = 0; idx < MaxAttemptCount; ++idx)
            {
                try
                {
                    var filename = string.Format(
                        "{1}_{0}.{2}.json",
                        idx,
                        dateStamp,
                        instrumentationKey);

                    if(!Directory.Exists(mapper.GetDataPath()))
                    {
                        Directory.CreateDirectory(mapper.GetDataPath());
                    }

                    var target = Path.Combine(mapper.GetDataPath(), filename);

                    using (var writer = File.CreateText(target))
                    {
                        writer.Write(data);
                        writer.Flush();
                        writer.Close();
                    }

                    break;
                }
                catch (IOException)
                {
                    if (idx == (MaxAttemptCount - 1))
                    {
                        throw;
                    }
                }
            }
        }

        public IEnumerable<string> GetItemIds(
            string instrumentationKey)
        {
            if (string.IsNullOrEmpty(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            var di = new DirectoryInfo(mapper.GetDataPath());
            var files = di.EnumerateFiles(
                string.Format("*{0}*.json", instrumentationKey), SearchOption.TopDirectoryOnly);

            return files.Select(t => t.Name);
        }

        public IEnumerable<string> GetAllItemIds()
        {
            var di = new DirectoryInfo(mapper.GetDataPath());
            var files = di.EnumerateFiles();

            return files.Select(t => t.Name);
        }

        public string GetItemData(string itemId)
        {
            var di = new DirectoryInfo(mapper.GetDataPath());
            var foundFiles = di.GetFiles(itemId, SearchOption.TopDirectoryOnly);

            if (0 == foundFiles.Length)
            {
                return string.Empty;
            }

            var file = foundFiles.First();

            return file.OpenText().ReadToEnd();
        }

        public IEnumerable<string> DeleteItems(
            string instrumentationKey)
        {
            IEnumerable<string> items = null;
            try
            {
                items = GetItemIds(instrumentationKey).ToList();
            }
            catch(Exception)
            {
                //trace errors
            }

            var deletedItems = new List<string>();
            if(items != null)
            {
                foreach (var itemId in items)
                {
                    try
                    {
                        var folder = mapper.GetDataPath();
                        var fileInfo = new FileInfo(Path.Combine(folder, itemId));
                        if (true == fileInfo.Exists)
                        {
                            fileInfo.Delete();
                            deletedItems.Add(fileInfo.FullName);
                        }
                    }
                    catch (Exception)
                    {
                        // trace errors
                    }
                }
            }            

            return deletedItems;
        }
    }
}