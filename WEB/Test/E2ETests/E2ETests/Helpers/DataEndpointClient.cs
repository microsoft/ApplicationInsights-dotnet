using AI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace E2ETests.Helpers
{
    public class DataEndpointClient
    {
        private const string DataEndpointControllerUriPart = "api/Data/";
        private const string DataEndpointPushItemActionUriPart = DataEndpointControllerUriPart + "PushItem/";
        private const string DataEndpointListAllItemsActionUriPart = DataEndpointControllerUriPart + "ListAllItems/";
        private const string DataEndpointListItemsActionUriPart = DataEndpointControllerUriPart + "ListItems/";
        private const string DataEndpointGetItemActionUriPart = DataEndpointControllerUriPart + "GetItem/";
        private const string DataEndpointDeleteItemsActionUriPart = DataEndpointControllerUriPart + "DeleteItems/";

        private readonly Uri endpointUri;
        private readonly Uri pushItemActionUri;
        private readonly Uri listAllItemsActionUri;
        private readonly Uri listItemsActionUri;
        private readonly Uri getItemActionUri;
        private readonly Uri deleteItemsActionUri;

        public DataEndpointClient(Uri endpointUri)
        {
            if (null == endpointUri)
            {
                throw new ArgumentNullException("endpointUri");
            }

            this.endpointUri = endpointUri;

            this.pushItemActionUri = new Uri(endpointUri.ToString() + DataEndpointPushItemActionUriPart);
            this.listAllItemsActionUri = new Uri(endpointUri.ToString() + DataEndpointListAllItemsActionUriPart);
            this.listItemsActionUri = new Uri(endpointUri.ToString() + DataEndpointListItemsActionUriPart);
            this.getItemActionUri = new Uri(endpointUri.ToString() + DataEndpointGetItemActionUriPart);
            this.deleteItemsActionUri = new Uri(endpointUri.ToString() + DataEndpointDeleteItemsActionUriPart);
        }

        public Uri EndpointUri
        {
            get { return this.endpointUri; }
        }

        public Uri PushItemActionUri
        {
            get { return this.pushItemActionUri; }
        }

        public Uri ListAllItemsActionUri
        {
            get { return this.listAllItemsActionUri; }
        }

        public Uri ListItemsActionUri
        {
            get { return this.listItemsActionUri; }
        }

        public Uri GetItemActionUri
        {
            get { return this.getItemActionUri; }
        }

        public Uri DeleteItemsActionUri
        {
            get { return this.deleteItemsActionUri; }
        }

        public string[] ListItemIds(string instrumentationKey)
        {
            if (true == string.IsNullOrWhiteSpace(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            var uploadedItemIdsData = this.GetDataEndpointActionResult(ListItemsActionUri + "?p=" +instrumentationKey);
           
            return JsonConvert.DeserializeObject<string[]>(uploadedItemIdsData);
        }

        public IList<Envelope> ListItems(string instrumentationKey)
        {
            if (true == string.IsNullOrWhiteSpace(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            var telemetryItems = new List<Envelope>();
            foreach (var uploadedItemId in ListItemIds(instrumentationKey))
            {
                var itemData = this.GetItem(uploadedItemId);

                Trace.TraceInformation(
                    "Received data item content saved at data endpoint, uploadedItemId:{0}, data:{1}",
                    uploadedItemId,
                    itemData);

                telemetryItems.AddRange(TelemetryItemFactory.GetTelemetryItems(itemData));
            }

            return telemetryItems;
        }

        public IList<T> GetItemsOfType<T>(string instrumentationKey)
        {
            if (true == string.IsNullOrWhiteSpace(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            var telemetryItems = new List<T>();            
            var itemData = this.GetItem(instrumentationKey);
            var allTelemetryItems = TelemetryItemFactory.GetTelemetryItems(itemData);
            var requestedTypeItems =  allTelemetryItems.Where(it => it.GetType() == typeof(T)).Cast<T>().ToArray();
            telemetryItems.AddRange(requestedTypeItems);
            
            return telemetryItems;
        }

        public string GetItem(string itemId)
        {
            if (true == string.IsNullOrWhiteSpace(itemId))
            {
                throw new ArgumentNullException("itemId");
            }

            return this.GetDataEndpointActionResult(GetItemActionUri + "?p=" + itemId);
        }

        public string[] DeleteItems(string instrumentationKey)
        {
            if (true == string.IsNullOrWhiteSpace(instrumentationKey))
            {
                throw new ArgumentNullException("instrumentationKey");
            }

            var delectedItemIdsData = this.GetDataEndpointActionResult(
                this.DeleteItemsActionUri + "?p=" + instrumentationKey);

            Trace.TraceInformation(
                "Received list of data items/errors from the data endpoint, data:{0}",
                delectedItemIdsData);

            return JsonConvert.DeserializeObject<string[]>(delectedItemIdsData);
        }

        private string GetDataEndpointActionResult(string url)
        {
            Trace.TraceInformation(DateTime.UtcNow.ToLongTimeString() + " Invoking url:" + url);
            using (var respose = WebRequest.CreateHttp(url).GetResponse())
            {                
                using (var reader = new StreamReader(respose.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
