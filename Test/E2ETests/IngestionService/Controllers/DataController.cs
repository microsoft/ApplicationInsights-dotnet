namespace IngestionService.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    public class DataController : ApiController
    {
        private readonly DataStorage storage = new DataStorage(
            new DataStoragePathMapper());

        private readonly DataStorageInMemory storageInMemory = new DataStorageInMemory();

        // GET: api/Data/ListAllItems
        [HttpGet]
        [ActionName("ListAllItems")]
        public IEnumerable<string> ListItems()
        {
            return this.storage.GetAllItemIds();
        }

        // GET: api/Data/HealthCheck/name
        [HttpGet]
        [ActionName("HealthCheck")]
        public HttpResponseMessage HealthCheck(string name)
        {            
            var resp = new HttpResponseMessage()
            {
                Content = new StringContent("Hello" + name + "!")
            };
            return resp;
        }

        // GET: api/Data/ListItems/instrumentationKey
        [HttpGet]
        [ActionName("ListItems")]
        public IEnumerable<string> ListItems(
            string p)
        {
            return this.storageInMemory.GetItemIds(p);
        }

        // GET: api/Data/GetItem/itemId
        [HttpGet]
        [ActionName("GetItem")]
        public HttpResponseMessage GetItem(string p)
        {
            var allItems = this.storageInMemory.GetItemIds(p);
            string output="";
            foreach (var item in allItems)
            {
                output = output + "\n\n" + item;
            }

            var resp = new HttpResponseMessage()
            {
                
                Content = new StringContent(output)
            };

            resp.Content.Headers.ContentType =
                new MediaTypeHeaderValue("application/json");

            return resp;
        }

        // GET: api/Data/DeleteItems/instrumentationKey
        [HttpGet]
        [ActionName("DeleteItems")]
        public IEnumerable<string> DeleteItems(string p)
        {
            return this.storageInMemory.DeleteItems(p);
        }

        private string ReadIkeyFromContent(string content)
        {
            if (true == string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentNullException("content");
            }

            const string Pattern = "\"iKey\":\"";
            var ikeyStartIndex = content.IndexOf(
                Pattern, 0, StringComparison.OrdinalIgnoreCase);
            if (-1 != ikeyStartIndex)
            {
                ikeyStartIndex += Pattern.Length;

                var iKeyEndIndex = content.IndexOf('\"', ikeyStartIndex);
                if (-1 != iKeyEndIndex)
                {
                    return content.Substring(ikeyStartIndex, iKeyEndIndex - ikeyStartIndex);
                }
            }

            throw new InvalidOperationException("iKey not found");
        }

        //api/Data/PushItem
        [HttpPost]
        [ActionName("PushItem")]
        public void PushItem([FromBody]string p)
        {            
            var key = ReadIkeyFromContent(p);
            this.storageInMemory.SaveDataItem(key, p);
        }
    }
}
