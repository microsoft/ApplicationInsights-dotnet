namespace WebAppFW45
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web.Http;
    using ProductsApp.Models;

    public class ProductsController : ApiController
    {
        IList<Product> products = new List<Product> 
        { 
            new Product { Id = 1, Name = "Tomato Soup", Category = "Groceries", Price = 1 }, 
            new Product { Id = 2, Name = "Yo-yo", Category = "Toys", Price = 3.75M }, 
            new Product { Id = 3, Name = "Hammer", Category = "Hardware", Price = 16.99M } 
        };

        public IEnumerable<Product> GetAllProducts()
        {
            return products;
        }

        public IHttpActionResult GetProduct(int id)
        {
            var product = products.FirstOrDefault((p) => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        public void PostProduct(int id)
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