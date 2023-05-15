using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

class Producer
{
    public Producer(uint id, StockItem stockItem)
    {
        this.Id = id;
        this.Stock = new List<StockItem>() { stockItem };
    }

    public Producer(uint id, List<StockItem> initialStock)
    {
        this.Id = id;
        this.Stock = initialStock;
    }

    ~Producer()
    {
        RequestRemovalFromAddressBook();
        throw new NotImplementedException();
    }

    public async Task GetProductInfoAsync(Product product)
    {
        throw new NotImplementedException();
    }

    public async Task GetProductInfoAsync(uint productId)
    {
        throw new NotImplementedException();
    }

    public async Task GetProductInfoAsync(string? productName = null, uint? minPrice = null, uint? maxPrice = null, List<string>? tags = null)
    {
        throw new NotImplementedException();
    }

    public async Task SellProductAsync(Product product, uint quantity = 1)
    {
        throw new NotImplementedException();
    }

    public async Task SellProductAsync(uint productId, uint quantity = 1)
    {
        throw new NotImplementedException();
    }

    public async Task ProduceGoodsAsync()
    {
        throw new NotImplementedException();
    }

    public async Task UpdatePriceAsyncs()
    {
        throw new NotImplementedException();
    }

    public async Task RequestAdditionToAddressBook()
    {
        throw new NotImplementedException();
    }

    public async Task RequestRemovalFromAddressBook()
    {
        throw new NotImplementedException();
    }

    public uint Id { get; }
    public List<StockItem> Stock { get; private set; }
}
