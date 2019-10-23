namespace Functional.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public class AppIdRequestHttpListener : HttpListenerObservableBase<AppIdResult>
    {
        public AppIdRequestHttpListener(string url) : base(url)
        {
        }

        protected override IEnumerable<AppIdResult> CreateNewItemsFromContext(HttpListenerContext context)
        {
            try
            {
                // We expect a get app id context request here. Let's return a random guid as an appId.
                return new[] { new AppIdResult { AppId = Guid.NewGuid().ToString() } };
            }
            finally
            {
                context.Response.Close();
            }
        }
    }
}
