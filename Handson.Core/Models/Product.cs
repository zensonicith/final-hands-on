using System;
using System.Collections.Generic;
using System.Text;

namespace Handson.Core.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public double Price { get; set; }
        public double Rating { get; set; }
        public int Stock { get; set; }
    }
}
