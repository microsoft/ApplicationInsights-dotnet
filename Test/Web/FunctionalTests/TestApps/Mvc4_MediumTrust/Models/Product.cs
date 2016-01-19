using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mvc4_MediumTrust.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
    }
}