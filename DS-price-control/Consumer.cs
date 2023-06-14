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


/// <summary>
/// Represents an basic consumer who can interract with a producers to buy randomly selected products.
/// </summary>
class Consumer
{
    /// <summary>
    /// Represents an order for a product with details about the quantity, maximum unit price, potential sellers, and more.
    /// </summary>
    public class Order
    {
        /// <summary>
        /// Initializes a new instance of the Order class.
        /// </summary>
        /// <param name="productId">The unique identifier of the product.</param>
        /// <param name="product">The actual product to order, if available.</param>
        /// <param name="quantity">The desired quantity of the product.</param>
        /// <param name="preferredUnitPrice">The maximum unit price that the buyer is willing to pay.</param>
        /// <param name="potentialSellersOffers">A dictionary of potential sellers and their offers.</param>
        /// <param name="bestDealerId">The identifier of the best dealer for the product, if one has been selected.</param>
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

    /// <summary>
    /// Encapsulates information about the current state of a sale process.
    /// </summary>
    public class StageInformation
    {
        public SaleStage CurrentStage;
        public SaleStage NextStage;
        public uint purchaseStageRetriesNum;
        public uint TurnsToWait;
        public bool InnerStageWaitActive;
        public bool InnerStageWaitCompleted;
    }

    /// <summary>
    /// Enumerates the possible stages of a sale process that a consumer can be in.
    /// </summary>
    public enum SaleStage
    {
        /// <summary>
        /// Stage at which the consumer is selecting a product to purchase.
        /// </summary>
        PRODUCT_SELECTION,

        /// <summary>
        /// Stage at which the consumer is filling out the details of the order.
        /// </summary>
        DETAILS_SELECTION,

        /// <summary>
        /// Stage at which the consumer is searching for the best dealer.
        /// </summary>
        BEST_DEALER_SEARCHING,

        /// <summary>
        /// Stage at which the consumer is contacting the best dealer.
        /// </summary>
        BEST_DEALER_CONTACT,

        /// <summary>
        /// Stage at which the consumer is waiting.
        /// </summary>
        RAND_WAIT,

        /// <summary>
        /// Unknown stage.
        /// </summary>
        UNKNOWN
    }

    /// <summary>
    /// Initializes a new instance of the Consumer class.
    /// </summary>
    /// <param name="id">Unique identifier for the consumer.</param>
    /// <param name="priceFluctuationFactor">Factor used to calculate maximum price fluctuations during purchase. Default is 0.05f.</param>
    /// <param name="quantityFluctuationFactor">Factor used to calculate quantity fluctuations during purchase. Default is 0.05f.</param>
    /// <remarks>
    /// The constructor initializes the StageInfo structure with default values, sets up a timer to manage consumer actions, and logs the entry of the consumer to the market.
    /// </remarks>
    public Consumer(uint id, float priceFluctuationFactor = 0.05f, float quantityFluctuationFactor = 0.05f)
    {
        this.Id = id;
        this._Producers = AddressBook.GetProducers();
        this._PriceFluctuationFactor = priceFluctuationFactor;
        this._QuantityFluctuationFactor = quantityFluctuationFactor;
        this._LogCounter = 0;

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

        LogToFile("Consumer has entered the market!");
    }

    /// <summary>
    /// Handles the event generated by a timer's elapsed interval.
    /// </summary>
    /// <param name="source">The source of the event.</param>
    /// <param name="e">An ElapsedEventArgs that contains the event data.</param>
    /// <remarks>
    /// This method calls the StageManager to handle the different stages of the sales process when the timer's interval has elapsed.
    /// </remarks>
    private void EventManager(System.Object source, ElapsedEventArgs e)
    {
        StageManager();
    }

    /// <summary>
    /// Manages the current stage of the sales process.
    /// </summary>
    /// <remarks>
    /// This method works as a state machine for the various stages of the sales process.
    /// Depending on the current value of 'StageInfo.CurrentStage', it executes different methods to handle the sales process.
    /// Stages include product selection, order detail filling, best dealer searching, best dealer contact, and random waiting. 
    /// It throws an exception if the stage is 'UNKNOWN'.
    /// </remarks>
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
    /// Buys goods from the best producer as determined by the 'CurrentOrder' details.
    /// </summary>
    /// <remarks>
    /// This method attempts to purchase a product from the producer identified by 'CurrentOrder.BestDealerId'.
    /// If the product is successfully purchased, it logs a success message, deducts the product cost from the consumer's money, 
    /// marks 'CurrentOrder' as realised, resets the purchasing stage and selects a new product.
    /// If the product is not successfully purchased, it logs a failure message, resets the purchasing stage, and selects a new product.
    /// Exceptions are thrown if 'CurrentOrder.BestDealerId' is null or if no producer can be found with the id 'bestProducerId'.
    /// </remarks>
    public void BuyGoods()
    {
        uint bestProducerId = CurrentOrder.BestDealerId ?? throw new Exception("The best producer has not been selected");
        Producer producer = _Producers.FirstOrDefault(prod => prod.Id == bestProducerId) ?? throw new Exception($"There is no producer with id = {bestProducerId}");
        StockItem? purchasedStockItem = producer.SellProduct(CurrentOrder.ProductId, CurrentOrder.Quantity ?? 0);

        if (purchasedStockItem != null)
        {
            LogToFile($"Consumer successfully received {purchasedStockItem.Quantity} unit(s) of product #{purchasedStockItem.Product.Id} from producer #{bestProducerId}.");
            _Money -= (float)(CurrentOrder.Quantity ?? 0) * purchasedStockItem.Price;
            CurrentOrder.IsRealised = true;
            ChangeStage(SaleStage.PRODUCT_SELECTION);
            StageInfo.purchaseStageRetriesNum = 0;
            StageInfo.InnerStageWaitActive = false;
            StageInfo.InnerStageWaitCompleted = false;
        }
        else
        {
            LogToFile($"Consumer did not received the product from producer #{bestProducerId}.");
            ChangeStage(SaleStage.PRODUCT_SELECTION);
            StageInfo.purchaseStageRetriesNum = 0;
            StageInfo.InnerStageWaitActive = false;
            StageInfo.InnerStageWaitCompleted = false;
        }
    }

    /// <summary>
    /// Updates the list of all products and stock items on the market.
    /// </summary>
    /// <remarks>
    /// This method iterates over all producers to gather their stock items. It populates the 'allStockOnTheMarket' and 'allProductOnTheMarket' 
    /// lists with these items and their associated products respectively. 
    /// It then updates the 'StockOnTheMarket' and 'ProductOnTheMarket' fields with these lists, ensuring that 'ProductOnTheMarket' contains unique items only.
    /// Lastly, it logs a message indicating the count of stock items and products currently available on the market.
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

        LogToFile($"Consumer updated products and stock items database (stock num = {allStockOnTheMarket.Count()}, items num = {allProductOnTheMarket.Count()}).");
    }

    /// <summary>
    /// Chooses a random product from the market to buy and initializes a new order with that product.
    /// </summary>
    /// <remarks>
    /// This method first generates a random index within the range of the list of available products on the market.
    /// It then retrieves the product at that index and creates a new order with the product's ID.
    /// After initializing the order, it logs a message indicating the selected product's ID and name and changes the sale stage to DETAILS_SELECTION.
    /// </remarks>
    private void ChooseProductToBuy()
    {
        int rndIndex = _Rng.Next(ProductOnTheMarket.Count);
        Product rndProduct = ProductOnTheMarket[rndIndex];
        CurrentOrder = new Order(rndProduct.Id);
        LogToFile($"Product #{rndProduct.Id} ('{rndProduct.Name}') has been selected for purchase.");
        ChangeStage(SaleStage.DETAILS_SELECTION);
    }

    /// <summary>
    /// Fills the current order with details including the max unit price and preferred quantity based on market averages.
    /// </summary>
    /// <remarks>
    /// The method first calculates the average price and quantity of the desired product based on all available stock on the market.
    /// If the average quantity is zero or if there are no potential sellers, it changes the sale stage to PRODUCT_SELECTION and prints a corresponding log message.
    /// Otherwise, it sets the max unit price to be the average price plus a random fluctuation, and the preferred quantity as a random value 
    /// fluctuating around the average quantity (both within predefined fluctuation factor bounds). 
    /// It then updates the current order with the max unit price and preferred quantity, and changes the sale stage to BEST_DEALER_SEARCHING.
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
                LogToFile($"Product #{CurrentOrder.ProductId} average amount is 0! New product selection...");
                ChangeStage(SaleStage.PRODUCT_SELECTION);
                return;
            }

            float maxUnitPrice = averagePrice + (float)(((_Rng.NextDouble() * 2) - 1.0) * _PriceFluctuationFactor * averagePrice);

            uint preferedQuantity = (uint)_Rng.Next(1, Math.Max(_Rng.Next((int)((1 - _QuantityFluctuationFactor) * averageQuantity), (int)((1 + _QuantityFluctuationFactor) * averageQuantity)), 2));

            CurrentOrder.MaxUnitPrice = maxUnitPrice;
            CurrentOrder.Quantity = preferedQuantity;
            LogToFile($"Purchase details have been predetermined (max price for unit = {maxUnitPrice}, preferred quantity = {preferedQuantity})");
            ChangeStage(SaleStage.BEST_DEALER_SEARCHING);
        }
        else
        {
            LogToFile($"Product #{CurrentOrder.ProductId} was not found in the producers stock. New product selection...");
            ChangeStage(SaleStage.PRODUCT_SELECTION);
        }
    }

    /// <summary>
    /// Finds potential dealers for the product specified in the current order.
    /// </summary>
    /// <remarks>
    /// This method iterates through the list of producers and checks if they have the desired product.
    /// If a producer has the product, the producer's ID and stock item information are added to a dictionary.
    /// If one or more producers are found, the dictionary of potential sellers' offers is updated in the current order and a log message is printed.
    /// If no producers are found for the product, a log message is printed and the sale stage is changed to PRODUCT_SELECTION.
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
            LogToFile($"{producersItem.Count} producer(s) with desired product #{CurrentOrder.ProductId} has been found.");
        }
        else
        {
            LogToFile($"No producers has a desired product #{CurrentOrder.ProductId} for sale. New product selection...");
            ChangeStage(SaleStage.PRODUCT_SELECTION);
        }
    }

    /// <summary>
    /// Selects the best dealer based on the price and quantity available from potential sellers for the current order.
    /// </summary>
    /// <remarks>
    /// Initially, it calculates a score for each potential seller and stores it in the sellersScore dictionary if the seller's price is less than the order's max unit price and 
    /// the seller's quantity is more than the order's quantity.
    /// If no sellers satisfy these conditions, it increases the max unit price and decreases the order quantity by 10%, then recalculates the score for each seller again.
    /// If still no sellers satisfy these conditions, it changes the sale stage to PRODUCT_SELECTION.
    /// If there are one or more sellers that satisfy the conditions, it selects the seller with the highest score as the best dealer, changes the sale stage to BEST_DEALER_CONTACT,
    /// and updates the best dealer ID in the current order.
    /// Exceptions are thrown if the max unit price or quantity for the current order is not set, or if the list of potential sellers is empty or null.
    /// </remarks>
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
            orderQuantity = orderQuantity == 0 ? 1 : orderQuantity;

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
                orderQuantity = orderQuantity == 0 ? 1 : orderQuantity;

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
                LogToFile($"The price and quantity of the order has been changed due to lack of perfect offers (current price = {orderMaxPrice}, current quantity = {orderQuantity}).");
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

            LogToFile($"Consumer has chosen producer #{bestSellerId} to fulfill an order (score = {bestSellerScore}).");
            CurrentOrder.BestDealerId = bestSellerId;
            ChangeStage(SaleStage.BEST_DEALER_CONTACT);
        }
        else
        {
            LogToFile("There wasnt any producers with offers meeting the requirements.");
            ChangeStage(SaleStage.PRODUCT_SELECTION);
        }
    }

    /// <summary>
    /// Checks the best offer for the current order based on product availability and unit price.
    /// </summary>
    /// <remarks>
    /// Retrieves the ID of the best producer and the corresponding producer instance.
    /// Checks if the selected producer has enough quantity of the product and if the price is not higher than the maximum acceptable unit price.
    /// If the producer does not have enough quantity or the price is too high and if the inner stage wait has completed with less than one purchase stage retry,
    /// the method changes the stage to BEST_DEALER_SEARCHING and increments the retry counter.
    /// If the inner stage wait is not completed, it changes the stage to BEST_DEALER_CONTACT and sets the inner stage wait.
    /// If the product's quantity and price satisfy the conditions, the consumer is ready to buy the goods.
    /// Exceptions are thrown if the best producer is not selected, the producer does not exist, or the producer does not have the product.
    /// </remarks>
    private void CheckBestOffer()
    {
        uint bestProducerId = CurrentOrder.BestDealerId ?? throw new Exception("The best producer has not been selected");
        Producer producer = _Producers.FirstOrDefault(prod => prod.Id == bestProducerId) ?? throw new Exception($"There is no producer with id = {bestProducerId}");
        StockItem prodItem = producer.GetItemInfo(CurrentOrder.ProductId) ?? throw new Exception($"Producer {bestProducerId} has no product with ID = {CurrentOrder.ProductId}");

        if (prodItem.Quantity < CurrentOrder.Quantity) 
        {
            LogToFile($"Producer #{bestProducerId} does not have the required quantity of the product (required = {CurrentOrder.Quantity}, current = {prodItem.Quantity}). Waiting for delivery...");

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
            LogToFile($"Price of the product {prodItem.Product.Id} is too high (max acceptable = {CurrentOrder.MaxUnitPrice}, current = {prodItem.Price}).");
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
            LogToFile($"Consumer is ready to buy {CurrentOrder.Quantity} units of product #{prodItem.Product.Id} from the producer #{bestProducerId}.");
            BuyGoods();
        }
    }

    /// <summary>
    /// Executes a waiting turn, reducing the number of turns to wait by one if there are any remaining.
    /// </summary>
    /// <remarks>
    /// If the number of turns to wait (<see cref="StageInfo.TurnsToWait"/>) is greater than zero, it's reduced by one.
    /// The remaining number of turns to wait is logged after each execution of this method.
    /// </remarks>
    private void Wait()
    {
        if (StageInfo.TurnsToWait > 0)
        {
            StageInfo.TurnsToWait--;
        }
        LogToFile($"Waiting... Turns left = {StageInfo.TurnsToWait}.");
    }

    /// <summary>
    /// Changes the current sale stage to the provided sale stage.
    /// </summary>
    /// <param name="nextMainStage">The next main stage to transition to.</param>
    /// <param name="waitingDuringTransition">Determines if there should be a waiting period during the transition. Default is true.</param>
    /// <param name="minTurnsToWait">The minimum number of turns to wait if waiting during the transition. Default is 1.</param>
    /// <param name="maxTurnsToWait">The maximum number of turns to wait if waiting during the transition. Default is 3.</param>
    /// <param name="innerStageWait">Flag to indicate if inner stage wait is active. Default is false.</param>
    /// <remarks>
    /// If waitingDuringTransition is true, the current sale stage is set to RAND_WAIT, and the next sale stage is set to nextMainStage.
    /// A random number of turns to wait is generated between minTurnsToWait and maxTurnsToWait (inclusive), and inner stage wait is set to the value of innerStageWait.
    /// If waitingDuringTransition is false, the current sale stage is directly set to nextMainStage, and the next sale stage is set to UNKNOWN.
    /// In both cases, the transition is logged.
    /// </remarks>
    private void ChangeStage(SaleStage nextMainStage, bool waitingDuringTransition = true, uint minTurnsToWait = 1, uint maxTurnsToWait = 3, bool innerStageWait = false)
    {
        if (waitingDuringTransition)
        {
            StageInfo.CurrentStage = SaleStage.RAND_WAIT;
            StageInfo.NextStage = nextMainStage;
            StageInfo.TurnsToWait = (uint)_Rng.Next((int)minTurnsToWait, (int)maxTurnsToWait + 1);
            StageInfo.InnerStageWaitActive = innerStageWait;
            LogToFile($"Consumer will transiti to stage {StageInfo.NextStage} after {StageInfo.TurnsToWait} waiting turns.");
        }
        else
        {
            StageInfo.CurrentStage = nextMainStage;
            StageInfo.NextStage = SaleStage.UNKNOWN;
            LogToFile($"Consumer has changed the stage to {StageInfo.CurrentStage} without waiting.");
        }
    }

    /// <summary>
    /// Automatically changes the current sale stage if no more turns are left to wait.
    /// </summary>
    /// <remarks>
    /// If there are no turns left to wait (i.e., <see cref="StageInfo.TurnsToWait"/> is zero), this method changes the
    /// current sale stage to the next sale stage and logs the change. If there are turns left to wait, the method does nothing.
    /// The method sets the <see cref="StageInfo.InnerStageWaitCompleted"/> flag to the value of <see cref="StageInfo.InnerStageWaitActive"/>
    /// if it was true, otherwise keeps the original value. Also, it sets the <see cref="StageInfo.InnerStageWaitActive"/> flag to false.
    /// <see cref="StageInfo.NextStage"/> is set to <see cref="SaleStage.UNKNOWN"/> after changing the current stage.
    /// </remarks>
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
            LogToFile($"Consumer changed the current stage to {StageInfo.CurrentStage} automatically after waiting.");
        }
    }

    /// <summary>
    /// Logs a given message to a file.
    /// </summary>
    /// <param name="logMessage">The message to log.</param>
    /// <remarks>
    /// The message is appended to a log file in the "../../../logs/" directory. The filename is based on the consumer's ID.
    /// Each logged message is prepended with a counter and the current timestamp, formatted as "HH:mm:ss.ffffff".
    /// </remarks>
    private void LogToFile(string logMessage)
    {
        using (StreamWriter w = File.AppendText($"../../../logs/consumer{this.Id}.txt"))
        {
            _LogCounter++;
            string stringTime = DateTime.Now.ToString($"HH:mm:ss.ffffff");
            w.Write($"\n{_LogCounter} | {stringTime} | {logMessage}");
        }
    }

    public uint Id { get; }
    public List<Product> ProductOnTheMarket { get; set; } = new List<Product>();
    public List<StockItem> StockOnTheMarket { get; set; } = new List<StockItem>();

    public Order CurrentOrder { get; set; }
    public StageInformation StageInfo = new StageInformation();

    private System.Timers.Timer _ConsumerActionTimer;

    private UInt64 _LogCounter;

    private List<Producer> _Producers;

    private float _Money = float.MaxValue;

    private Random _Rng = new Random();

    private float _PriceFluctuationFactor;

    private float _QuantityFluctuationFactor;
}
