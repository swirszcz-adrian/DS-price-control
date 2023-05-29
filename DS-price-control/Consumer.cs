using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PriceControl.Producer;
using System.Threading;
using System.Diagnostics.Tracing;

namespace PriceControl;

class Consumer
{
    /// <summary>
    /// Represents a consumer order for a specific product.
    /// </summary>
    class Order
    {
        /// <summary>
        /// Initializes a new instance of the Order class.
        /// </summary>
        /// <param name="productID">The unique ID of the product to be ordered.</param>
        /// <param name="product">The product to be ordered. Defaults to null.</param>
        /// <param name="quantity">The amount of product to be ordered. Defaults to null.</param>
        /// <param name="preferredUnitPrice">The preferred unit price for the product. Defaults to null.</param>
        /// <param name="potentialSellers">A list of potential sellers for the product. Defaults to null.</param>
        public Order(uint productId, Product? product = null, uint? quantity = null, uint? preferredUnitPrice = null, Dictionary<uint, StockItem>? potentialSellersOffers = null, uint? bestSellerId = null) 
        {
            this.ProductId = productId;
            this.Product = product;
            this.Quantity = quantity;
            this.MaxUnitPrice = preferredUnitPrice;
            this.PotentialSellersOffers = potentialSellersOffers;
            this.BestSellerId = bestSellerId;
            this.IsRealised = false;
        }

        public uint ProductId { get; set; }
        public Product? Product { get; set; }
        public uint? Quantity { get; set; }
        public float? MaxUnitPrice { get; set; }
        public Dictionary<uint, StockItem>? PotentialSellersOffers { get; set; }
        public uint? BestSellerId { get; set; }
        public bool IsRealised { get; set; }
    }

    public enum SaleStage
    {
        PRODUCT_SELECTION, DETAILS_SELECTION, PRODUCER_SEARCHING, CONTACT_BEST_PRODUCER, WAITI, DO_NOTHING
    }

    public Consumer(uint id, float priceFluctuationFactor = 0.05f, float quantityFluctuationFactor = 0.05f)
    {
        this.Id = id;
        _Timer = new System.Timers.Timer(1000); // Wywołuje metodę co 1 sekundę (1000 milisekund)
        _Timer.Elapsed += EventManager;
        _Timer.AutoReset = true;
        _Timer.Enabled = true;
        this._Producers = AddressBook.GetProducers();
        this.PriceFluctuationFactor = priceFluctuationFactor;
        this.QuantityFluctuationFactor = quantityFluctuationFactor;
        Stage = SaleStage.PRODUCT_SELECTION;
        Console.WriteLine("\n[INFO] : Consumer #{0} entered the market!\n", this.Id);
    }

    private void EventManager(Object source, ElapsedEventArgs e)
    {
        Console.WriteLine("\n - - - - - - - - - - Consumer #{0} turn start (t = {1}) - - - - - - - - - -", this.Id, e.SignalTime);
        StageManager();
        GenerateMoney(10);
        Console.WriteLine(" - - - - - - - - - - - - - - - - - - - - Turn end - - - - - - - - - - - - - - - - - - - -\n");
    }

    private void StageManager()
    {
        switch (Stage)
        {
            case SaleStage.PRODUCT_SELECTION:
            {
                UpdateProductsList();
                ChooseProductToBuy();
                break;
            }
            case SaleStage.DETAILS_SELECTION:
            {
                FillOrderDetails();
                break;
            }
            case SaleStage.WAITI:
            {
                this.TurnsToWait --;
                Console.WriteLine("[INFO] : Waiting... Turns left = {0}.", this.TurnsToWait);
                if (this.TurnsToWait == 0)
                {
                    this.TurnsToWait = null;
                    this.Stage = SaleStage.CONTACT_BEST_PRODUCER;
                }
                break;
            }
            case SaleStage.PRODUCER_SEARCHING:
            {
                GetProductSellers();
                GetBestSeller();
                break;
            }
            case SaleStage.CONTACT_BEST_PRODUCER:
            {
                CheckCurrentOfferStatus();
                break;
            }
            case SaleStage.DO_NOTHING:
            {
                Console.WriteLine("Jejjjjj");
                break;
            }

            default: break;
        }
    }

    public void BuyGoods()
    {
        uint bestProducerId = this.CurrentOrder.BestSellerId.Value;
        Producer producer = _Producers.FirstOrDefault(prod => prod.Id == bestProducerId);

        StockItem? purchasedItem = producer.SellProduct(this.CurrentOrder.ProductId, this.CurrentOrder.Quantity.Value);
        if (purchasedItem != null)
        {
            Console.WriteLine("[INFO] : Consumer #{0} obtained the product.", this.Id);
            this._Money -= (float)this.CurrentOrder.Quantity.Value * purchasedItem.Price;
            this.Stage = SaleStage.PRODUCT_SELECTION;
        }
        else
        {
            Console.WriteLine("[INFO] : Consumer #{0} did not receive the product.", this.Id);
            this.Stage = SaleStage.DO_NOTHING;
        }
    }

    /// <summary>
    /// Asynchronously updates the list of products and stock items available on the market from all the producers.
    /// </summary>
    /// <remarks>
    /// This method initiates an asynchronous task for each producer in the _Producers list to retrieve a list of stock items. 
    /// The resulting lists of stock items are consolidated into a single list, which is then assigned to the StockOnTheMarket property. 
    /// In addition, each stock item's associated product is added to the ProductOnTheMarket property, which is a list of unique products.
    /// </remarks>
    private void UpdateProductsList()
    {
        List<StockItem> allStockOnTheMarket = new List<StockItem>();
        List<Product> allProductOnTheMarket = new List<Product>();

        foreach (Producer producer in _Producers)
        {
            List<StockItem> producerStockItemList = producer.GetItemList();
            foreach (StockItem stockItem in producerStockItemList)
            {
                allStockOnTheMarket.Add(stockItem);
                allProductOnTheMarket.Add(stockItem.Product);
            }
        }

        this.StockOnTheMarket = allStockOnTheMarket;
        this.ProductOnTheMarket = new HashSet<Product>(allProductOnTheMarket).ToList();

        Console.WriteLine("[INFO] : Consumer #{0} updated products and stock items database.", this.Id);
    }

    private void GenerateMoney(uint amount)
    {
        this._Money += amount;
        Console.WriteLine("[INFO] : Consumer #{0} added {1} money units to account (total amount = {2}).", this.Id, amount, this._Money);
    }

    private void ChooseProductToBuy()
    {
        int rndIndex = this.Rng.Next(ProductOnTheMarket.Count);
        Product rndProduct = ProductOnTheMarket[rndIndex];

        Console.WriteLine("[INFO] : Product #{0} ('{1}') has been selected for purchase.", rndProduct.Id, rndProduct.Name);
        this.CurrentOrder = new Order(rndProduct.Id);
        this.Stage = SaleStage.DETAILS_SELECTION;
    }

    private void FillOrderDetails()
    {
        float averagePrice = 0.0f;
        uint averageQuantity = 0;
        uint potentialSellersNum = 0;

        foreach (StockItem stockItem in StockOnTheMarket)
        {
            if (stockItem.Product.Id == this.CurrentOrder.ProductId)
            {
                averagePrice += stockItem.Price;
                averageQuantity += stockItem.Quantity;
                potentialSellersNum ++;
            }
        }

        if (potentialSellersNum != 0)
        {
            averagePrice /= potentialSellersNum;
            averageQuantity /= potentialSellersNum;

            float maxUnitPrice = averagePrice + (float)(((this.Rng.NextDouble() * 2) - 1.0) * this.PriceFluctuationFactor * averagePrice);
            uint preferedQuantity = (uint)this.Rng.Next(1, this.Rng.Next((int)((1 - this.QuantityFluctuationFactor) * averageQuantity), (int)((1 + this.QuantityFluctuationFactor) * averageQuantity)));

            this.CurrentOrder.MaxUnitPrice = maxUnitPrice;
            this.CurrentOrder.Quantity = preferedQuantity;
            Console.WriteLine("[INFO] : Purchase details have been predetermined (max price for unit = {0}, preferred quantity = {1})", maxUnitPrice, preferedQuantity);
            this.Stage = SaleStage.PRODUCER_SEARCHING;
        }
        else
        {
            Console.WriteLine("[INFO] : Product #{0} was not found in the producers stock. New product selection...", this.CurrentOrder.ProductId);
            this.Stage = SaleStage.PRODUCT_SELECTION;
        }
    }

    private void GetProductSellers()
    {
        Dictionary<uint, StockItem> producersItem = new Dictionary<uint, StockItem>();

        foreach (Producer producer in _Producers)
        {
            StockItem? item = producer.GetItemInfo(this.CurrentOrder.ProductId);
            if (item != null)
            {
                producersItem.Add(producer.Id, item);
            }
        }

        if (producersItem.Count > 0)
        {
            this.CurrentOrder.PotentialSellersOffers = producersItem;
            Console.WriteLine("[INFO] : {0} producers found with wanted product #{1}.", producersItem.Count, this.CurrentOrder.ProductId);
        }
        else
        {
            this.Stage = SaleStage.PRODUCT_SELECTION;
        }
    }

    private void GetBestSeller()
    {
        Dictionary<uint, float> sellersScore = new Dictionary<uint, float>();

        if (this.CurrentOrder.PotentialSellersOffers != null && this.CurrentOrder.PotentialSellersOffers.Count() > 0)
        {
            foreach (KeyValuePair<uint, StockItem> entry in this.CurrentOrder.PotentialSellersOffers)
            {
                float sellerPrice = entry.Value.Price;
                uint sellerQuantity = entry.Value.Quantity;
                float orderMaxPrice = this.CurrentOrder.MaxUnitPrice.Value;
                uint orderQuantity = this.CurrentOrder.Quantity.Value;

                float sellerScore = 3 / 4 * orderMaxPrice / sellerPrice + 1 / 4 * sellerQuantity / orderQuantity;

                sellersScore.Add(entry.Key, sellerScore);
            }

            var randPair = sellersScore.First();
            uint bestSellerId = randPair.Key;
            float bestSellerScore = randPair.Value;

            foreach (KeyValuePair<uint, float> entry in sellersScore)
            {
                float tempSellerScore = entry.Value;
                if (tempSellerScore > bestSellerScore)
                {
                    bestSellerScore = tempSellerScore;
                    bestSellerId = entry.Key;
                }
            }

            Console.WriteLine("[INFO] : Consumer #{0} has chosen producer #{1} to fill an order.", this.Id, bestSellerId);
            this.CurrentOrder.BestSellerId = bestSellerId;
            this.Stage = SaleStage.CONTACT_BEST_PRODUCER;
        }
        else
        {
            Console.WriteLine("[WARN] : Consumer #{0} could not choose producer to fill an order.", this.Id);
            return;
        }
    }

    private void CheckCurrentOfferStatus()
    {
        if (this.CurrentOrder.BestSellerId != null)
        {
            uint bestProducerId = this.CurrentOrder.BestSellerId.Value;
            Producer producer = _Producers.FirstOrDefault(prod => prod.Id == bestProducerId);
            StockItem item = producer.GetItemInfo(this.CurrentOrder.ProductId);

            if (item.Quantity < this.CurrentOrder.Quantity) 
            {
                Console.WriteLine("[INFO] : Producer #{0} does not have the required quantity of the product (required = {1}, current = {2}).\nStarted waiting for delivery.",
                    bestProducerId, this.CurrentOrder.Quantity, item.Quantity);
                if (this.TurnsToWait != null)
                {
                    PickWaitingTurnsNum();
                    this.Stage = SaleStage.WAITI;
                }
            }
            else if (item.Price * this.CurrentOrder.Quantity > this._Money)
            {
                Console.WriteLine("[INFO] : Consumer #{0} does not have the required money to pay for the entire order (required = {1}, current = {2}).\nStarted waiting for proceeds.",
                    this.Id, item.Price * this.CurrentOrder.Quantity, this._Money);
                if (this.TurnsToWait != null)
                {
                    PickWaitingTurnsNum();
                    this.Stage = SaleStage.WAITI;
                }
            }
            else
            {
                Console.WriteLine("[INFO] : Consumer #{0} is ready to buy {1} units of product #{2} from the producer #{3}.",
                    this.Id, this.CurrentOrder.Quantity, item.Product.Id, bestProducerId);
                BuyGoods();
                if (this.TurnsToWait == null)
                {
                    this.TurnsToWait = 0;
                }
            }

            if (this.TurnsToWait == null)
            {
                this.TurnsToWait = 0;
                Console.WriteLine("[INFO] : Waiting will not be repeated. Order canceled.");
                this.Stage = SaleStage.PRODUCT_SELECTION;
            }
        }   
    }

    private void PickWaitingTurnsNum()
    {
        TurnsToWait = (uint)this.Rng.Next(3, 5);
    }

    public uint Id { get; }
    public List<Product> ProductOnTheMarket { get; set; }
    public List<StockItem> StockOnTheMarket { get; set; }

    public uint? TurnsToWait { get; set; } = 0;
    private Order CurrentOrder;

    private System.Timers.Timer _Timer;

    private List<Producer> _Producers;

    private float _Money = 0;

    private SaleStage Stage;

    private Random Rng = new Random();

    private float PriceFluctuationFactor;

    private float QuantityFluctuationFactor;
}
