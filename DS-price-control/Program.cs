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
        Product prd1 = new Product(0, "Mydło", "Szare i pachnące.");
        Product prd2 = new Product(1, "Powidła", "Pyszne powidła jak u babci.");
        Product prd3 = new Product(2, "Scyzoryk", "Wiele funkcji.");
        Product prd4 = new Product(3, "Garnek", "Nie wrzucać do ognia!");

        Producer producer1 = new Producer(0, 500,
            new List<Producer.ProducerItem> () {
                new Producer.ProducerItem(prd1, 22, 100),
                new Producer.ProducerItem(prd2, 50, 50),
                new Producer.ProducerItem(prd3, 34, 130)
                }
            );
        Producer producer2 = new Producer(1, 600,
            new List<Producer.ProducerItem>() {
                new Producer.ProducerItem(prd1, 19, 200),
                new Producer.ProducerItem(prd2, 20, 200),
                new Producer.ProducerItem(prd4, 80, 20)
                }
            );
        Producer producer3 = new Producer(2, 400,
            new List<Producer.ProducerItem>() {
                new Producer.ProducerItem(prd3, 40, 190),
                new Producer.ProducerItem(prd4, 78, 30)
                }
            );

        Consumer janusz1 = new Consumer(0);
        //Thread.Sleep(500);
        Consumer janusz2 = new Consumer(1);

        /*        for (int i = 0; i < 20; i++)
                {
                    Console.WriteLine(producer.ToFullString());
                    Thread.Sleep(2000);
                }*/
        Console.ReadLine();


        /*        AddressBook ad = new AddressBook();

                Consumer c1 = new Consumer(ad);*/
    }
}
