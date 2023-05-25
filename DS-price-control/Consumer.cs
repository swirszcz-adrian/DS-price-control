using System;
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
        /// <param name="product">The product to be ordered.</param>
        /// <param name="amount">The amount of product to be ordered. Defaults to null.</param>
        /// <param name="preferredUnitPrice">The preferred unit price for the product. Defaults to null.</param>
        /// <param name="potentialSellers">A list of potential sellers for the product. Defaults to null.</param>
        public Order(Product product, uint? amount = null, uint? preferredUnitPrice = null, List<Producer>? potentialSellers = null) 
        {
            this.Product = product;
            this.Amount = amount;
            this.PreferredUnitPrice = preferredUnitPrice;
            this.PotentialSellers = potentialSellers;
            this.IsRealised = false;
        }

        public Product Product { get; set; }
        public uint? Amount { get; set; }
        public uint? PreferredUnitPrice { get; set; }
        public List<Producer>? PotentialSellers { get; set; }
        public bool IsRealised { get; set; }
    }

    public Consumer(AddressBook addressBook)
    {
        _Timer = new System.Timers.Timer(1000); // Wywołuje metodę co 1 sekundę (1000 milisekund)
        _Timer.Elapsed += EventManager;
        _Timer.AutoReset = true;
        _Timer.Enabled = true;
        Console.WriteLine("Koniec konstruktora");
        //GetCurrentAddressBookAsync();
        //UpdateWishListAsync();
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

    private void MakeDecision()
    {
        throw new NotImplementedException();
    }

    private async Task GetCurrentAddressBookAsync()
    {
        throw new NotImplementedException();
    }

    private async Task UpdateProductsListAsync()
    {
        throw new NotImplementedException();
    }

    private void GenerateMoney(uint amount)
    {
        _Money += amount;
        Console.WriteLine("Dodano środki");
    }

    private void ChooseProductToBuy()
    {
        UpdateProductsListAsync();

        Random rnd = new Random();
        int rndIndex = rnd.Next(ProductOnTheMarket.Count);
        Product rndProduct = ProductOnTheMarket[rndIndex];

        this.CurrentOrder = new Order(rndProduct);
    }

    private void EventManager(Object source, ElapsedEventArgs e)
    {
        MakeDecision();
        Console.WriteLine("Wywołanie metody o godzinie: {0}", e.SignalTime);
        GenerateMoney(10);
    }

    public List<Product> ProductOnTheMarket { get; }
    private Order CurrentOrder;

    private System.Timers.Timer _Timer;

    private List<Producer> _Producers;

    private uint _Money = 0;
}
