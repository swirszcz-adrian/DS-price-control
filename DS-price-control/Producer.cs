using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

public delegate float PriceUpdateDelegate(float magazineFillFactor, float basePrice, float currentPrice);

class Producer
{
    public static float BasicFiveSegmentPriceUpdate(float magazineFillFactor, float basePrice, float currentPrice)
    {
        float modifiedPrice;
        float lowerLimit = 0.5f * basePrice;
        float upperLimit = 2.0f * basePrice;

        if (magazineFillFactor > 0.8f) { modifiedPrice = 0.9f * currentPrice; }
        else if (magazineFillFactor > 0.6f) { modifiedPrice = 0.95f * currentPrice; }
        else if (magazineFillFactor >  0.4f) { modifiedPrice = currentPrice; }
        else if (magazineFillFactor > 0.2f) { modifiedPrice = 1.05f * currentPrice; }
        else { modifiedPrice = 1.1f * currentPrice; }

        return (modifiedPrice < lowerLimit) ? lowerLimit : (modifiedPrice > upperLimit) ? upperLimit : modifiedPrice;
    }

    public static float StableFiveSegmentPriceUpdate(float magazineFillFactor, float basePrice, float _)
    {
        float modifiedPrice;

        if (magazineFillFactor > 0.8f) { modifiedPrice = 0.70f * basePrice; }
        else if (magazineFillFactor > 0.6f) { modifiedPrice = 0.85f * basePrice; }
        else if (magazineFillFactor > 0.4f) { modifiedPrice = basePrice; }
        else if (magazineFillFactor > 0.2f) { modifiedPrice = 1.15f * basePrice; }
        else { modifiedPrice = 1.30f * basePrice; }

        return modifiedPrice;
    }

    public class ProducerItem : StockItem
    {
        public ProducerItem(Product product, float basePrice, uint maxStorageSpace, uint batchSize = 1, uint turnsToProduceBatch = 1, uint startingQuantity = 0, PriceUpdateDelegate? priceUpdateFunction = null) 
            : base(product, basePrice, startingQuantity)
        {
            this.BasePrice = basePrice;
            this.MaxStorageSpace = maxStorageSpace;
            this.BatchSize = batchSize;
            this.TurnsToProduceBatch = turnsToProduceBatch;
            this.PriceUpdateFunction = priceUpdateFunction ?? BasicFiveSegmentPriceUpdate;
        }

        public ProducerItem(uint id, string name, float basePrice, uint maxStorageSpace, uint batchSize = 1, uint turnsToProduceBatch = 1, uint startingQuantity = 0, string description = "", List<string>? tags = null, PriceUpdateDelegate? priceUpdateFunction = null) 
            : base(id, name, basePrice, description, tags, startingQuantity)
        {
            this.BasePrice = basePrice;
            this.MaxStorageSpace = maxStorageSpace;
            this.BatchSize = batchSize;
            this.TurnsToProduceBatch = turnsToProduceBatch;
            this.PriceUpdateFunction = priceUpdateFunction ?? BasicFiveSegmentPriceUpdate;
        }

        public void Produce()
        {
            if (this.Quantity < this.MaxStorageSpace)
            {
                this._CurrentTurn++;
                if (this._CurrentTurn >= this.TurnsToProduceBatch)
                {
                    this._CurrentTurn = 0;
                    this.Quantity = Math.Min(this.MaxStorageSpace, this.Quantity + this.BatchSize);
                }
            }

        }

        public void UpdatePrice()
        {
            this.Price = this.PriceUpdateFunction(this.Quantity / this.MaxStorageSpace, this.BasePrice, this.Price);
        }

        private uint _CurrentTurn = 0;
        public float BasePrice { get; set; }
        public uint BatchSize { get; set; }
        public uint TurnsToProduceBatch { get; set; }
        public uint MaxStorageSpace { get; set; }
        PriceUpdateDelegate PriceUpdateFunction { get; set; }
    }

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
