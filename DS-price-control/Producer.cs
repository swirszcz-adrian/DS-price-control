using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

class Producer
{
    private static Random rand = new Random();

    public int Id { get; }
    public int Stock { get; private set; }
    public int Price { get; }

    public Producer(int id, int initialStock, int price)
    {
        Id = id;
        Stock = initialStock;
        Price = price;
    }

    public int SellGoods(int quantity)
    {
        int sellableQuantity = Math.Min(quantity, Stock);
        Stock -= sellableQuantity;
        return sellableQuantity;
    }

    public async Task ProduceGoodsAsync()
    {
        while (true)
        {
            if (Stock < 50) // Minimalny poziom towaru
            {
                Stock += rand.Next(50, 101); // Produkcja nowego towaru
                Console.WriteLine($"Producer {Id} produced goods. Current stock: {Stock}");
            }
            await Task.Delay(1000); // Czas produkcji
        }
    }
}
