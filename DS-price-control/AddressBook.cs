using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

static class AddressBook
{
    private static List<Producer> _Producers = new List<Producer>();

    public static List<Producer> GetProducers()
    {
        return new List<Producer>(_Producers);
    }

    public static void AddProducer(Producer producer)
    {
        if (AddressBook._Producers.Any(prod => prod.Id == producer.Id))
        {
            throw new InvalidOperationException("Address book cannot have two producers with the same ID!");
        }
        AddressBook._Producers.Add(producer);
    }

    public static void RemoveProducer(Producer producer)
    {
        AddressBook._Producers.Remove(producer);
    }

    
}
