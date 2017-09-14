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

        // GET: api/Data/ListAllItems
        [HttpGet]
        [ActionName("ListAllItems")]
        public IEnumerable<string> ListItems()
        {
            return this.storage.GetAllItemIds();
        }

        // GET: api/Data/ListItems/instrumentationKey
        [HttpGet]
        [ActionName("ListItems")]
        public IEnumerable<string> ListItems(
            string p)
        {
            return this.storage.GetItemIds(p);
        }

        // GET: api/Data/GetItem/itemId
        [HttpGet]
        [ActionName("GetItem")]
        public HttpResponseMessage GetItem(string p)
        {
            var resp = new HttpResponseMessage()
            {
                Content = new StringContent(this.storage.GetItemData(p))
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
            return this.storage.DeleteItems(p);
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
            this.storage.SaveDataItem(key, p);
        }
    }
}
