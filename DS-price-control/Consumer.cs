using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

class Consumer
{
    public Consumer(AddressBook addressBook)
    {
        GetCurrentAddressBookAsync();
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
    }

    public async Task MakeDecisionAsync()
    {
        throw new NotImplementedException();
    }

    public async Task GetCurrentAddressBookAsync()
    {
        throw new NotImplementedException();
    }

    private List<Producer> _Producers;
}
