using System;
using System.Timers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PriceControl.Producer;
using System.Threading;
using System.Diagnostics.Tracing;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.ComponentModel;

namespace PriceControl;

class Consumer
{
    /// <summary>
    /// Represents a consumer order for a specific product.
    /// </summary>
    public class Order
    {
        public Order(uint productId, Product? product = null, uint? quantity = null, uint? preferredUnitPrice = null, Dictionary<uint, StockItem>? potentialSellersOffers = null, uint? bestDealerId = null) 
        {
            this.ProductId = productId;
            this.Product = product;
            this.Quantity = quantity;
            this.MaxUnitPrice = preferredUnitPrice;
            this.PotentialSellersOffers = potentialSellersOffers;
            this.BestDealerId = bestDealerId;
            this.IsRealised = false;
        }

        public uint ProductId { get; set; }
        public Product? Product { get; set; }
        public uint? Quantity { get; set; }
        public float? MaxUnitPrice { get; set; }
        public Dictionary<uint, StockItem>? PotentialSellersOffers { get; set; }
        public uint? BestDealerId { get; set; }
        public bool IsRealised { get; set; }
    }

    public static class StageInfo
    {
        public static SaleStage CurrentStage;
        public static SaleStage NextStage;
        public static uint purchaseStageRetriesNum;
        public static uint TurnsToWait;
        public static bool InnerStageWaitActive;
        public static bool InnerStageWaitCompleted;
    }

    public enum SaleStage
    {
        PRODUCT_SELECTION, DETAILS_SELECTION, BEST_DEALER_SEARCHING, BEST_DEALER_CONTACT, RAND_WAIT, UNKNOWN
    }

    public Consumer(uint id, float priceFluctuationFactor = 0.05f, float quantityFluctuationFactor = 0.05f)
    {
        this.Id = id;
        this._Producers = AddressBook.GetProducers();
        this._PriceFluctuationFactor = priceFluctuationFactor;
        this._QuantityFluctuationFactor = quantityFluctuationFactor;

        StageInfo.CurrentStage = SaleStage.PRODUCT_SELECTION;
        StageInfo.NextStage = SaleStage.UNKNOWN;
        StageInfo.purchaseStageRetriesNum = 0;
        StageInfo.TurnsToWait = 0;
        StageInfo.InnerStageWaitActive = false;
        StageInfo.InnerStageWaitCompleted = false;

        this._ConsumerActionTimer = new System.Timers.Timer(1000);
        this._ConsumerActionTimer.Elapsed += EventManager;
        this._ConsumerActionTimer.AutoReset = true;
        this._ConsumerActionTimer.Enabled = true;

        Console.WriteLine("\n[INFO] : Consumer #{0} entered the market!\n", this.Id);
    }

    private void EventManager(System.Object source, ElapsedEventArgs e)
    {
        //Console.WriteLine("\n - - - - - - - - - - Consumer #{0} turn start (t = {1}) - - - - - - - - - -", this.Id, e.SignalTime);
        StageManager();
        //Console.WriteLine(" - - - - - - - - - - - - - - - - - - - - Turn end - - - - - - - - - - - - - - - - - - - -\n");
        // TODO: Wywalić znacziki początka i końca tury 
    }

    private void StageManager()
    {
        switch (StageInfo.CurrentStage)
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
            case SaleStage.BEST_DEALER_SEARCHING:
            {
                FindProductDealers();
                SelectBestDealer();
                break;
            }
            case SaleStage.BEST_DEALER_CONTACT:
            {
                CheckBestOffer();
                break;
            }
            case SaleStage.RAND_WAIT:
            {
                Wait();
                ChangeStageAuto();
                break;
            }
            case SaleStage.UNKNOWN:
            {
                throw new Exception("You shouldn't be here.");
            }

            default: break;
        }
    }

    /// <summary>
    /// Buys goods from the selected producer and handles the post-purchase stages.
    /// </summary>
    /// <remarks>
    /// This method attempts to purchase the product specified in the current order from the selected best dealer. It throws an exception 
    /// if the best dealer hasn't been selected or if there is no producer with the selected dealer's id.
    ///
    /// The purchase operation is carried out by calling the SellProduct method of the producer. If the purchase is successful (the returned 
    /// StockItem is not null), the method deducts the cost of the product from the consumer's money, resets the purchase stage retries number,
    /// deactivates the inner stage wait, sets the inner stage wait completed status to false, and changes the sale stage back to product selection. 
    /// An informational log message indicating that the consumer obtained the product is also printed.
    ///
    /// If the purchase operation is not successful (the returned StockItem is null), the method performs the same actions as for a successful 
    /// purchase but prints an informational log message indicating that the consumer did not receive the product.
    /// </remarks>
    /// <exception cref="System.Exception">Thrown when the best producer has not been selected or if there is no producer with the selected dealer's id.</exception>
    public void BuyGoods()
    {
        uint bestProducerId = CurrentOrder.BestDealerId ?? throw new Exception("The best producer has not been selected");
        Producer producer = _Producers.FirstOrDefault(prod => prod.Id == bestProducerId) ?? throw new Exception($"There is no producer with id = {bestProducerId}");
        StockItem? purchasedStockItem = producer.SellProduct(CurrentOrder.ProductId, CurrentOrder.Quantity ?? 0);

        if (purchasedStockItem != null)
        {
            Console.WriteLine("[INFO] : Consumer #{0} obtained the product.", this.Id);
            _Money -= (float)(CurrentOrder.Quantity ?? 0) * purchasedStockItem.Price;
            ChangeStage(SaleStage.PRODUCT_SELECTION);
            StageInfo.purchaseStageRetriesNum = 0;
            StageInfo.InnerStageWaitActive = false;
            StageInfo.InnerStageWaitCompleted = false;
        }
        else
        {
            Console.WriteLine("[INFO] : Consumer #{0} did not receive the product.", this.Id);
            ChangeStage(SaleStage.PRODUCT_SELECTION);
            StageInfo.purchaseStageRetriesNum = 0;
            StageInfo.InnerStageWaitActive = false;
            StageInfo.InnerStageWaitCompleted = false;
        }
    }

    /// <summary>
    /// Updates the current list of products and stock items available in the market.
    /// </summary>
    /// <remarks>
    /// This method iterates over each producer and fetches their stock items list. It appends all these stock items to the general list of 
    /// available stock items in the market and corresponding products to the list of available products in the market.
    ///
    /// The newly generated lists of stock items and products are then assigned to the corresponding properties, StockOnTheMarket and 
    /// ProductOnTheMarket. Note that the ProductOnTheMarket is a list without duplicates, hence a HashSet is used before converting it back 
    /// to a List to ensure uniqueness of products.
    ///
    /// An informational log is printed after the update indicating that the consumer's database of products and stock items has been updated.
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

        StockOnTheMarket = allStockOnTheMarket;
        ProductOnTheMarket = new HashSet<Product>(allProductOnTheMarket).ToList();

        Console.WriteLine("[INFO] : Consumer #{0} updated products and stock items database.", this.Id);
    }

    /// <summary>
    /// Chooses a random product to buy and initializes an order for it.
    /// </summary>
    /// <remarks>
    /// This method randomly selects a product from the available products in the market and creates a new order with the selected product's Id. 
    /// It then changes the sale stage to details selection and prints an informational log message with the Id and name of the selected product.
    /// </remarks>
    private void ChooseProductToBuy()
    {
        int rndIndex = _Rng.Next(ProductOnTheMarket.Count);
        Product rndProduct = ProductOnTheMarket[rndIndex];
        CurrentOrder = new Order(rndProduct.Id);
        Console.WriteLine("[INFO] : Product #{0} ('{1}') has been selected for purchase.", rndProduct.Id, rndProduct.Name);
        ChangeStage(SaleStage.DETAILS_SELECTION);
    }

    /// <summary>
    /// Fills the order details for the current order including maximum unit price and preferred quantity.
    /// </summary>
    /// <remarks>
    /// This method calculates the average price and quantity of the product of the current order available in the market. 
    /// If the product is found, it then determines the maximum unit price and preferred quantity of the order based on these average values 
    /// and fluctuation factors (PriceFluctuationFactor and QuantityFluctuationFactor). The maximum unit price is a random fluctuation around 
    /// the average price, and the preferred quantity is a random value within a range calculated using the average quantity and the 
    /// QuantityFluctuationFactor.
    ///
    /// If the product is available from at least one seller, the calculated maximum unit price and preferred quantity are set in the current 
    /// order, the sale stage is changed to best dealer searching, and an informational log is printed with the determined purchase details.
    ///
    /// If the product is not available from any seller, the sale stage is changed to product selection and an informational log is printed 
    /// notifying that the product was not found in the producer's stock and a new product selection is needed.
    /// </remarks>
    private void FillOrderDetails()
    {
        float averagePrice = 0.0f;
        uint averageQuantity = 0;
        uint potentialSellersNum = 0;

        foreach (StockItem stockItem in StockOnTheMarket)
        {
            if (stockItem.Product.Id == CurrentOrder.ProductId)
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

            if (averageQuantity == 0) 
            {
                Console.WriteLine("[INFO] : Product #{0} average amount is 0. New product selection...", CurrentOrder.ProductId);
                ChangeStage(SaleStage.PRODUCT_SELECTION);
                return;
            }

            float maxUnitPrice = averagePrice + (float)(((_Rng.NextDouble() * 2) - 1.0) * _PriceFluctuationFactor * averagePrice);

            uint preferedQuantity = (uint)_Rng.Next(1, Math.Max(_Rng.Next((int)((1 - _QuantityFluctuationFactor) * averageQuantity), (int)((1 + _QuantityFluctuationFactor) * averageQuantity)), 2));

            CurrentOrder.MaxUnitPrice = maxUnitPrice;
            CurrentOrder.Quantity = preferedQuantity;
            Console.WriteLine("[INFO] : Purchase details have been predetermined (max price for unit = {0}, preferred quantity = {1})", maxUnitPrice, preferedQuantity);
            ChangeStage(SaleStage.BEST_DEALER_SEARCHING);
        }
        else
        {
            ChangeStage(SaleStage.PRODUCT_SELECTION);
            Console.WriteLine("[INFO] : Product #{0} was not found in the producers stock. New product selection...", CurrentOrder.ProductId);
        }
    }

    /// <summary>
    /// Finds and assigns dealers for the product of the current order.
    /// </summary>
    /// <remarks>
    /// This method iterates through all available producers and checks if they can provide the product specified in the current order. 
    /// If the producer has the product, it is added to the potential sellers' list.
    ///
    /// If at least one dealer with the wanted product is found, the potential sellers' list is updated in the current order and a log with 
    /// the count of found producers and the product id is printed.
    ///
    /// If no dealer with the wanted product is found, the sale stage changes to product selection and a log with the product id is printed.
    /// </remarks>
    private void FindProductDealers()
    {
        Dictionary<uint, StockItem> producersItem = new Dictionary<uint, StockItem>();

        foreach (Producer producer in _Producers)
        {
            StockItem? item = producer.GetItemInfo(CurrentOrder.ProductId);
            if (item != null)
            {
                producersItem.Add(producer.Id, item);
            }
        }

        if (producersItem.Count > 0)
        {
            CurrentOrder.PotentialSellersOffers = producersItem;
            Console.WriteLine("[INFO] : {0} producers found with wanted product #{1}.", producersItem.Count, CurrentOrder.ProductId);
        }
        else
        {
            ChangeStage(SaleStage.PRODUCT_SELECTION);
            Console.WriteLine("[INFO] : No producers found with wanted product #{1}.", CurrentOrder.ProductId);
        }
    }

    /// <summary>
    /// Selects the best dealer based on their offered price and quantity.
    /// </summary>
    /// <remarks>
    /// This method uses a scoring mechanism to select the best dealer. Each potential dealer is scored based on their offered price and quantity.
    /// The score is a weighted sum of the order maximum unit price divided by the dealer price (75% weight) 
    /// and the dealer quantity divided by the order quantity (25% weight). 
    /// 
    /// If no dealer meets the initial criteria of offering a price less than the maximum order price and a quantity greater than the order quantity,
    /// the method adjusts the order parameters (increasing the maximum unit price by 5% and decreasing the quantity by 10%) 
    /// and reruns the scoring process.
    /// 
    /// If there are suitable dealers based on the new criteria, the order parameters are updated and the dealer with the highest score is selected.
    /// If there are still no suitable dealers, the method logs a warning and changes the sale stage to product selection.
    /// 
    /// The method throws an exception if either the maximum unit price or the quantity of the product was not set.
    /// It also throws an exception if the list of potential sellers is empty or null.
    /// </remarks>
    /// <exception cref="System.Exception">Thrown when the maximum unit price or the quantity of the product was not set or the list of potential sellers is empty or null.</exception>
    private void SelectBestDealer()
    {
        Dictionary<uint, float> sellersScore = new Dictionary<uint, float>();
        float orderMaxPrice = CurrentOrder.MaxUnitPrice ?? throw new Exception("Max unit price of the product was not set.");
        uint orderQuantity = CurrentOrder.Quantity ?? throw new Exception("Quantity of the product was not set.");

        if (CurrentOrder.PotentialSellersOffers == null || CurrentOrder.PotentialSellersOffers.Count() == 0)
        {
            throw new Exception("List of potential sellers is empty or null.");
        }

        foreach (KeyValuePair<uint, StockItem> entry in CurrentOrder.PotentialSellersOffers)
        {
            float dealerPrice = entry.Value.Price;
            uint dealerQuantity = entry.Value.Quantity;

            if (dealerPrice < orderMaxPrice && dealerQuantity > orderQuantity)
            {
                float sellerScore = 3 / 4 * orderMaxPrice / dealerPrice + 1 / 4 * dealerQuantity / orderQuantity;
                sellersScore.Add(entry.Key, sellerScore);
            }
        }

        if (sellersScore.Count == 0)
        {
            foreach (KeyValuePair<uint, StockItem> entry in CurrentOrder.PotentialSellersOffers)
            {
                float dealerPrice = entry.Value.Price;
                uint dealerQuantity = entry.Value.Quantity;
                orderMaxPrice = 1.05f * orderMaxPrice;
                orderQuantity = (uint)(0.9 * orderQuantity);

                if (dealerPrice < orderMaxPrice && dealerQuantity > orderQuantity)
                {
                    float sellerScore = 3 / 4 * orderMaxPrice / dealerPrice + 1 / 4 * dealerQuantity / orderQuantity;
                    sellersScore.Add(entry.Key, sellerScore);
                }
            }

            if (sellersScore.Count > 0)
            {
                CurrentOrder.MaxUnitPrice = orderMaxPrice;
                CurrentOrder.Quantity = orderQuantity;
                Console.WriteLine("The price and quantity of the order has been changed due to lack of perfect offers");
            }
        }

        if (sellersScore.Count > 0)
        {
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

            Console.WriteLine("[INFO] : Consumer #{0} has chosen producer #{1} to fulfill an order.", this.Id, bestSellerId);
            CurrentOrder.BestDealerId = bestSellerId;
            ChangeStage(SaleStage.BEST_DEALER_CONTACT);
        }
        else
        {
            Console.WriteLine("[WARN] : There wasnt any producers with offers meeting the requirements.");
            foreach (KeyValuePair<uint, StockItem> entry in CurrentOrder.PotentialSellersOffers)
            {
                float dealerPrice = entry.Value.Price;
                uint dealerQuantity = entry.Value.Quantity;
                Console.WriteLine($"price = {dealerPrice}, quant = {dealerQuantity}");
            }
            ChangeStage(SaleStage.PRODUCT_SELECTION);
        }
    }

    /// <summary>
    /// Checks the best offer from the producer and performs actions based on the availability and price of the product.
    /// </summary>
    private void CheckBestOffer()
    {
        uint bestProducerId = CurrentOrder.BestDealerId ?? throw new Exception("The best producer has not been selected");
        Producer producer = _Producers.FirstOrDefault(prod => prod.Id == bestProducerId) ?? throw new Exception($"There is no producer with id = {bestProducerId}");
        StockItem prodItem = producer.GetItemInfo(CurrentOrder.ProductId) ?? throw new Exception($"Producer {bestProducerId} has no product with ID = {CurrentOrder.ProductId}");

        if (prodItem.Quantity < CurrentOrder.Quantity) 
        {
            Console.WriteLine("[INFO] : Producer #{0} does not have the required quantity of the product (required = {1}, current = {2}).\nStarted waiting for delivery.",
                bestProducerId, this.CurrentOrder.Quantity, prodItem.Quantity);

            if (StageInfo.InnerStageWaitCompleted && StageInfo.purchaseStageRetriesNum < 1)
            {
                ChangeStage(SaleStage.BEST_DEALER_SEARCHING);
                StageInfo.purchaseStageRetriesNum += 1;
                StageInfo.InnerStageWaitCompleted = false;
            }
            else if (!StageInfo.InnerStageWaitCompleted)
            {
                ChangeStage(nextMainStage: SaleStage.BEST_DEALER_CONTACT, innerStageWait: true);
            }
        }
        else if (prodItem.Price > CurrentOrder.MaxUnitPrice)
        {
            Console.WriteLine("[INFO] : Price of the product {0} is too high (max acceptable = {1}, current = {2})", prodItem.Product.Id, CurrentOrder.MaxUnitPrice, prodItem.Price);
            if (StageInfo.InnerStageWaitCompleted && StageInfo.purchaseStageRetriesNum < 1)
            {
                ChangeStage(SaleStage.BEST_DEALER_SEARCHING);
                StageInfo.purchaseStageRetriesNum += 1;
                StageInfo.InnerStageWaitCompleted = false;
            }
            else if (!StageInfo.InnerStageWaitCompleted)
            {
                ChangeStage(nextMainStage: SaleStage.BEST_DEALER_CONTACT, innerStageWait: true);
            }
        }
        else
        {
            Console.WriteLine("[INFO] : Consumer #{0} is ready to buy {1} units of product #{2} from the producer #{3}.",
                this.Id, CurrentOrder.Quantity, prodItem.Product.Id, bestProducerId);
            BuyGoods();
        }
    }

    /// <summary>
    /// Performs a wait operation during the sale process.
    /// </summary>
    private void Wait()
    {
        if (StageInfo.TurnsToWait > 0)
        {
            StageInfo.TurnsToWait--;
        }
        Console.WriteLine("[INFO] : Waiting... Turns left = {0}.", StageInfo.TurnsToWait);
    }

    /// <summary>
    /// Changes the stage of the sale process.
    /// </summary>
    /// <param name="nextMainStage">The next main stage to transition to.</param>
    /// <param name="waitingDuringTransition">Indicates whether to wait during the transition.</param>
    /// <param name="minTurnsToWait">The minimum number of turns to wait during the transition (default is 1).</param>
    /// <param name="maxTurnsToWait">The maximum number of turns to wait during the transition (default is 3).</param>
    /// <param name="innerStageWait">Indicates whether stage transition is related to inner-stage waiting.</param>
    private void ChangeStage(SaleStage nextMainStage, bool waitingDuringTransition = true, uint minTurnsToWait = 1, uint maxTurnsToWait = 3, bool innerStageWait = false)
    {
        if (waitingDuringTransition)
        {
            StageInfo.CurrentStage = SaleStage.RAND_WAIT;
            StageInfo.NextStage = nextMainStage;
            StageInfo.TurnsToWait = (uint)_Rng.Next((int)minTurnsToWait, (int)maxTurnsToWait + 1);
            StageInfo.InnerStageWaitActive = innerStageWait;
            Console.WriteLine("[INFO] : Consumer will transiti to stage {0} after {1} waiting turns.", StageInfo.NextStage, StageInfo.TurnsToWait);
        }
        else
        {
            StageInfo.CurrentStage = nextMainStage;
            StageInfo.NextStage = SaleStage.UNKNOWN;
            Console.WriteLine("[INFO] : Consumer has changed the stage to {0} without waiting.", StageInfo.CurrentStage);
        }
    }

    /// <summary>
    /// Automatically changes the stage of the sale process when the waiting turns have completed.
    /// </summary>
    private void ChangeStageAuto()
    {
        if (StageInfo.TurnsToWait > 0)
        {
            return;
        }
        else
        {
            StageInfo.InnerStageWaitCompleted = true ? StageInfo.InnerStageWaitActive : StageInfo.InnerStageWaitCompleted;
            StageInfo.InnerStageWaitActive = false;
            StageInfo.CurrentStage = StageInfo.NextStage;
            StageInfo.NextStage = SaleStage.UNKNOWN;
            Console.WriteLine("[INFO] : Consumer changed the current stage to {0} automatically after waiting.", StageInfo.CurrentStage);
        }
    }


    public uint Id { get; }
    public List<Product> ProductOnTheMarket { get; set; } = new List<Product>();
    public List<StockItem> StockOnTheMarket { get; set; } = new List<StockItem>();

    public Order CurrentOrder { get; set; }

    private System.Timers.Timer _ConsumerActionTimer;

    private List<Producer> _Producers;

    private float _Money = float.MaxValue;

    private Random _Rng = new Random();

    private float _PriceFluctuationFactor;

    private float _QuantityFluctuationFactor;
}
