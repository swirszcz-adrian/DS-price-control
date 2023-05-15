using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

struct StockItem
{
    public StockItem(Product product, uint quantity = 0, List<string>? tags = null) 
    { 
        this.Product = product;
        this.Quantity = quantity;
        this._Tags = tags ?? new List<string>();
        for (int i = 0; i < this._Tags.Count; i++)
        {
            this._Tags[i].ToLower();
        }
    }

    public StockItem(uint id, string name, float price, string description = "", uint quantity = 0, List<string>? tags = null)
    {
        this.Product = new Product(id, name, price, description);
        this.Quantity = quantity;
        this._Tags = tags ?? new List<string>();
        for (int i = 0; i < this._Tags.Count; i++)
        {
            this._Tags[i].ToLower();
        }
    }

    public Product Product { get; }
    public uint Quantity { get; set; }
    private List<string> _Tags;
    public List<string> Tags
    {
        get
        {
            return this._Tags;
        }
        set
        {
            this._Tags = value;
            for (int i = 0; i < this._Tags.Count; i++)
            {
                this._Tags[i].ToLower();
            }
        }
    }
    public bool ContainsTag(List<string> tags)
    {
        foreach (string tag in tags)
        {
            if (this._Tags.Contains(tag.ToLower())) return true;
        }

        return false;
    }

    public bool ContainsTag(string tag)
    {
        if (this._Tags.Contains(tag.ToLower())) return true;

        return false;
    }
}
