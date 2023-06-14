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

    public StockItem Copy()
    {
        return new StockItem(this.Product, this.Price, this.Quantity);  // Product is a struct
                                                                        // therefore it's copied by value
    }

    private readonly object _PriceLock = new object();
    private readonly object _QuantityLock = new object();

    public Product Product { get; }
    public float Price 
    { 
        get 
        {
            lock (_PriceLock)
            {
                return this._Price;
            }
        } 
        set 
        {
            lock (_PriceLock)
            {
                this._Price = value;
            }
        } 
    }
    private float _Price;
    public uint Quantity
    {
        get
        {
            lock (_QuantityLock)
            {
                return this._Quantity;
            }
        }
        set
        {
            lock (_QuantityLock)
            {
                this._Quantity = value;
            }
        }
    }
    private uint _Quantity;
}
