using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace PriceControl;

class PriceControl
{
    static async Task Main()
    {
        /*// Tworzenie producentów
        ConcurrentBag<Producer> producers = new ConcurrentBag<Producer>()
        {
            new Producer(1, 100, 10),
            new Producer(2, 120, 8),
            new Producer(3, 80, 15),
        };

        // Dodanie producentów do książki adresowej
        AddressBook addressBook = new AddressBook();
        foreach (var producer in producers)
        {
            addressBook.AddProducer(producer);
        }

        // Tworzenie odbiorcy z dostępem do książki adresowej
        Consumer consumer = new Consumer(addressBook);

        // Uruchomienie producentów i odbiorcy na osobnych wątkach
        var producerTasks = producers.Select(p => p.ProduceGoodsAsync());
        var consumerTask = consumer.BuyGoodsAsync(100);

        // Czekanie na zakończenie wszystkich zadań
        await Task.WhenAll(producerTasks.Concat(new[] { consumerTask }));*/
        Product prd = new Product(0, "012345678901234567890123456789", "xvxcvcxvxcvcxcvxcvxcvcxvxcvxcvcxvcxvxvxcvxcvcxvxcvxcvxcvxcvcxvcxvxcvxcvxcvcxvxc");
        Product prd2 = new Product(1, "0123", "xvcvxc");
        Product prd3 = new Product(2, "012345678901234567890123456789", "xvxcvcxvxcvcxcvxcvxcvcxvxcvxcvcxvcxvxvxcvxcvcxvxcvxcvxcvxcvcxvcxvxcvxcvxcvcxvxc");
        Producer producer = new Producer(2, 
            new List<Producer.ProducerItem> () {
                new Producer.ProducerItem(prd, 20, 4),
                new Producer.ProducerItem(prd2, 20, 4),
                new Producer.ProducerItem(prd3, 20, 4)
                }
            );
        Console.WriteLine(producer.ToString());
    }
}
