using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Mvc4_MediumTrust.Controllers
{
    using Models;
    using System.Globalization;

    public class ProductsController : Controller
    {
        private IList<Product> products = new List<Product> 
        { 
            new Product { Id = 1, Name = "Tomato Soup", Category = "Groceries", Price = 1 }, 
            new Product { Id = 2, Name = "Yo-yo", Category = "Toys", Price = 3.75M }, 
            new Product { Id = 3, Name = "Hammer", Category = "Hardware", Price = 16.99M } 
        };

        //
        // GET: /Products/All
        [AllowAnonymous]
        public ActionResult All()
        {
            return Json(products, JsonRequestBehavior.AllowGet);
        }

        

        //
        // GET: /Products/Product?id=
        [AllowAnonymous]
        public ActionResult Product(int id)
        {
            var product = products.FirstOrDefault((p) => p.Id == id);
            if (product == null)
            {
                return HttpNotFound(string.Format(CultureInfo.InvariantCulture, "Product id {0} not found", id));
            }
            return Json(product, JsonRequestBehavior.AllowGet);
        }

        //
        // Post: /Products/Add?id=
        [HttpPost]
        [AllowAnonymous]
        public void Add(int id)
        {
            if (id != products.Count + 1)
            {
                throw new ArgumentException("Test exception to get 500");
            }
            else
            {
                this.products.Add(new Product { Id = id, Name = "myName" });
            }
        }


    }
}
