using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

class StockItem
{
    public StockItem(Product product, float price , uint quantity = 0) 
    { 
        this.Product = product;
        this.Price = price;
        this.Quantity = quantity;
    }

    public StockItem(uint id, string name, float price, string description = "", List<string>? tags = null, uint quantity = 0)
    {
        this.Product = new Product(id, name, description, tags);
        this.Price = price;
        this.Quantity = quantity;
    }

    public Product Product { get; }
    public float Price { get; set; }
    public uint Quantity { get; set; }
}
