using System;
using System.Collections.Generic;
using System.Text;

namespace Incubation.Models {
    public sealed class Product {
        public required string Sku { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class ManyRandomProducts {
        public List<Product> Products { get; set; } = new();
    }
}
