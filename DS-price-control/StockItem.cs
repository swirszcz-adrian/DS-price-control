using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS_price_control;

struct StockItem
{
    public StockItem(Product product, uint quantity, List<string>? tags = null) 
    { 
        this.Product = product;
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
    public bool ContainsTag(List<string> Tags)
    {
        foreach (string Tag in Tags)
        {
            string tag = Tag.ToLower();
            if (this._Tags.Contains(tag)) return true;
        }

        return false;
    }

    public bool ContainsTag(string Tag)
    {
        string tag = Tag.ToLower();
        if (this._Tags.Contains(tag)) return true;

        return false;
    }
}
