using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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

        public override string ToString()
        {
            string nameStr = this.Product.Name.Length < 10 ? this.Product.Name : this.Product.Name.Substring(0, 10 - 3) + "...";
            string descriptionStr = this.Product.Description.Length < 20 ? this.Product.Description : this.Product.Description.Substring(0, 20 - 3) + "...";

            string tagsStr = string.Empty;
            foreach (string tag in this.Product.Tags) { tagsStr += "<" + tag + "> "; }
            tagsStr = tagsStr.Length < 20 ? tagsStr : tagsStr.Substring(0, 20 - 3) + "...";

            string str = string.Format("{0,4} | {1, 10} | {2, 6} | {3, 6} | {4, 20} | {5, 20}\n", this.Product.Id, nameStr, this.Price, this.Quantity, descriptionStr, tagsStr);
            return str;
        }

        public string ToCsv(string timestamp)
        {
            return $"{timestamp},{this.Product.Id},{this.Product.Name},{this.Price:0.00},{this.BasePrice:0.00},{this.Quantity},{this.MaxStorageSpace}\n";
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

    public Producer(uint id, uint ProductionTimeMS, ProducerItem producerItem)
    {
        this.Id = id;
        this._ProductionTimer = new System.Timers.Timer(ProductionTimeMS);
        this._ProductionTimer.Elapsed +=  this.ProductionTimerCallback;
        this._ProductionTimer.AutoReset = true;
        this._ProductionTimer.Enabled = true;
        this._Magazine = new List<ProducerItem>() { producerItem };
        this.RequestAdditionToAddressBook();
        using (StreamWriter w = File.CreateText($"../../../logs/producer{this.Id}.csv"))
        {
            w.WriteLine("timestamp,product_id,product_name,current_price,base_price,curent_quantity,max_quantity");
        }
    }

    public Producer(uint id, uint ProductionTimeMS, List<ProducerItem> initialMagazineState)
    {
        this.Id = id;
        this._ProductionTimer = new System.Timers.Timer(ProductionTimeMS);
        this._ProductionTimer.Elapsed += this.ProductionTimerCallback;
        this._ProductionTimer.AutoReset = true;
        this._ProductionTimer.Enabled = true;
        this._Magazine = new List<ProducerItem>();
        foreach (ProducerItem item in initialMagazineState)
        {
            this.AddItemToMagazine(item);
        }
        this.RequestAdditionToAddressBook();
        using (StreamWriter w = File.CreateText($"../../../logs/producer{this.Id}.csv"))
        {
            w.WriteLine("timestamp,product_id,product_name,current_price,base_price,curent_quantity,max_quantity");
        }
    }

    ~Producer()
    {
        this._ProductionTimer.Stop();
        this._ProductionTimer.Dispose();
        this.RequestRemovalFromAddressBook();
    }

    public override string ToString()
    {
        string str = "Magazine state of producer #" + this.Id.ToString() + ":\n";
        str += "ID   | NAME       | DESCRIPTION          | TAGS                \n";
        foreach (ProducerItem item in this._Magazine) { str += item.Product.ToString(); }
        return str;
    }

    public string ToFullString()
    {
        string str = "Magazine state of producer #" + this.Id.ToString() + ":\n";
        str += "ID   | NAME       | PRICE  | NUMBER | DESCRIPTION          | TAGS                \n";
        foreach (ProducerItem item in this._Magazine) { str += item.ToString(); }
        return str;
    }

    protected void RemoveItemFromMagazine(ProducerItem item)
    {
        this.RemoveItemFromMagazine(item.Product.Id);
    }

    protected void RemoveItemFromMagazine(uint id)
    {
        this._Magazine.RemoveAll(item => item.Product.Id == id);
    }

    protected void AddItemToMagazine(ProducerItem item)
    {
        if (_Magazine.Any(it => it.Product.Id == item.Product.Id))
        {
            throw new InvalidOperationException("Producer cannot have two items with the same id!");
        }
        this._Magazine.Add(item);
    }

    private void ProductionTimerCallback(Object? source, ElapsedEventArgs e)
    {
        this.ProduceGoods();
        this.UpdatePrices();
        this.LogStatus(e);
    }

    private void ProduceGoods()
    {
        foreach (ProducerItem Item in this._Magazine)
        {
            Item.Produce();
        }
    }

    private void UpdatePrices()
    {
        foreach (ProducerItem Item in this._Magazine)
        {
            Item.UpdatePrice();
        }
    }

    private void LogStatus(ElapsedEventArgs e)
    {
        string timestamp = e.SignalTime.ToString("HH:mm:ss.ffffff");
        string output = string.Empty;
        foreach (ProducerItem Item in this._Magazine)
        {
            output += Item.ToCsv(timestamp);
        }
        using (StreamWriter w = File.AppendText($"../../../logs/producer{this.Id}.csv"))
        {
            w.Write(output);
        }

    }


    public StockItem? GetItemInfo(Product product)
    {
        return this.GetItemInfo(product.Id);
    }

    public StockItem? GetItemInfo(uint productId)
    {
        ProducerItem? item = this._Magazine.FirstOrDefault(it => it.Product.Id == productId);
        return item != null ? item.Copy() : null;
    }

    public List<StockItem> GetItemList(string? productName = null, uint? minPrice = null, uint? maxPrice = null, List<string>? tags = null)
    {
        List<ProducerItem> refList = new List<ProducerItem>(this._Magazine);
        if (refList.Any() && productName != null)
        {
            refList.RemoveAll(item => item.Product.Name != productName);
        }
        if (refList.Any() && minPrice != null)
        {
            refList.RemoveAll(item => item.Price < minPrice);
        }
        if (refList.Any() && maxPrice != null)
        {
            refList.RemoveAll(item => item.Price > maxPrice);
        }
        if (refList.Any() && tags != null)
        {
            refList.RemoveAll(item => !item.Product.ContainsTag(tags));
        }

        List<StockItem> returnList = new List<StockItem>();
        foreach (var reference in refList)
        {
            returnList.Add(reference.Copy());
        }
        return returnList;
    }

    public StockItem? SellProduct(uint productId, uint quantity = 1)
    {
        ProducerItem? item = this._Magazine.FirstOrDefault(item => item.Product.Id == productId);
        string timeStamp = DateTime.Now.ToString("HH:mm:ss.ffffff");
        if (item == null)
        {
            Console.WriteLine($"{timeStamp} | Producer #{this.Id} failed to sell product #{productId} -> this producer does not produce product with this id!");
            return null;
        }
        else if (item.Quantity < quantity) 
        {
            Console.WriteLine($"{timeStamp} | Producer #{this.Id} failed to sell {quantity} unit(s) of product #{productId} -> only {item.Quantity} left in the storage!");
            return null;
        }
        else
        {
            item.Quantity -= quantity;
            Console.WriteLine($"{timeStamp} | Producer #{this.Id} sold {quantity} unit(s) of product #{productId} for {item.Price:0.00}$ per unit -> {item.Quantity}/{item.MaxStorageSpace} remaining.");
            return new StockItem(item.Product, item.Price, quantity);
            
        }
    }


    public void RequestAdditionToAddressBook()
    {
        AddressBook.AddProducer(this);
        string timeStamp = DateTime.Now.ToString("HH:mm:ss.ffffff");
        Console.WriteLine($"{timeStamp} | Producer #{this.Id} has been added to AddressBook");
    }

    public void RequestRemovalFromAddressBook()
    {
        AddressBook.RemoveProducer(this);
        string timeStamp = DateTime.Now.ToString("HH:mm:ss.ffffff");
        Console.WriteLine($"{timeStamp} | Producer #{this.Id} has been removed from AddressBook");
    }

    public uint Id { get; }
    private System.Timers.Timer _ProductionTimer;
    private List<ProducerItem> _Magazine { get; set; }
}
