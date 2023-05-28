﻿using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PriceControl.Producer;
using System.Threading;

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
        /// <param name="amount">The amount of product to be ordered. Defaults to null.</param>
        /// <param name="preferredUnitPrice">The preferred unit price for the product. Defaults to null.</param>
        /// <param name="potentialSellers">A list of potential sellers for the product. Defaults to null.</param>
        public Order(uint productId, Product? product = null, uint? amount = null, uint? preferredUnitPrice = null, List<Producer>? potentialSellers = null) 
        {
            this.ProductId = productId;
            this.Product = product;
            this.Amount = amount;
            this.PreferredUnitPrice = preferredUnitPrice;
            this.PotentialSellers = potentialSellers;
            this.IsRealised = false;
        }

        public uint ProductId { get; set; }
        public Product? Product { get; set; }
        public uint? Amount { get; set; }
        public uint? PreferredUnitPrice { get; set; }
        public List<Producer>? PotentialSellers { get; set; }
        public bool IsRealised { get; set; }
    }

    public enum SaleStage
    {
        PRODUCT_SELECTION, DETAILS_SELECTION, WAITING_FOR_FUNDS
    }

    public Consumer()
    {
        _Timer = new System.Timers.Timer(1000); // Wywołuje metodę co 1 sekundę (1000 milisekund)
        _Timer.Elapsed += EventManager;
        _Timer.AutoReset = true;
        _Timer.Enabled = true;
        this._Producers = AddressBook.GetProducers();
        stage = SaleStage.PRODUCT_SELECTION;
        Console.WriteLine("Koniec konstruktora");
        UpdateProductsList();
        Console.ReadKey();
    }

    public async Task RequestGoodsInfoAsync()
    {
        throw new NotImplementedException();
    }

    public async Task BuyGoodsAsync(int quantity)
    {
        /*        while (quantity > 0)
                {
                    // Wybór producenta - najpierw najtańszego, potem losowego
                    var producer = _Producers.GetProducers().OrderBy(p => p.Price).ThenBy(p => Guid.NewGuid()).FirstOrDefault();

                    if (producer != null)
                    {
                        int boughtGoods = producer.SellGoods(quantity);
                        quantity -= boughtGoods;

                        Console.WriteLine($"Bought {boughtGoods} goods from producer {producer.Id} at price {producer.Price}. Remaining goods to buy: {quantity}");
                    }
                    else
                    {
                        Console.WriteLine("No producers available at this moment. Trying again in a second.");
                    }

                    await Task.Delay(1000); // Czas na przetworzenie transakcji
                }*/
        throw new NotImplementedException();
    }

    private void StageManager()
    {
        switch (stage)
        {
            case SaleStage.PRODUCT_SELECTION:
            {
                UpdateProductsList();
                ChooseProductToBuy();
                break;
            }
            case SaleStage.DETAILS_SELECTION:
            {

                break;
            }
            case SaleStage.WAITING_FOR_FUNDS:
            {
                break;
            }
            default: break;
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
    }

    private void GenerateMoney(uint amount)
    {
        this._Money += amount;
        Console.WriteLine("Added {0} units to account funds (total amount = {1})", amount, this._Money);
    }

    private void ChooseProductToBuy()
    {
        Random rnd = new Random();
        int rndIndex = rnd.Next(ProductOnTheMarket.Count);
        Product rndProduct = ProductOnTheMarket[rndIndex];

        this.CurrentOrder = new Order(rndProduct.Id);
    }

    private void FillOrderDetails()
    {
        foreach ()
        Random rnd = new Random();
        int rndIndex = rnd.Next(ProductOnTheMarket.Count);
        Product rndProduct = ProductOnTheMarket[rndIndex];

        this.CurrentOrder = new Order(rndProduct.Id);
    }

    private void EventManager(Object source, ElapsedEventArgs e)
    {
        StageManager();
        Console.WriteLine("Wywołanie metody o godzinie: {0}", e.SignalTime);
        GenerateMoney(10);
    }

    public List<Product> ProductOnTheMarket { get; set; }
    public List<StockItem> StockOnTheMarket { get; set; }
    private Order CurrentOrder;

    private System.Timers.Timer _Timer;

    private List<Producer> _Producers;

    private uint _Money = 0;

    private SaleStage stage;
}
