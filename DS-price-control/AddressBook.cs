using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PriceControl;

class AddressBook
{
    private ConcurrentBag<Producer> _producers = new ConcurrentBag<Producer>();

    public void AddProducer(Producer producer)
    {
        _producers.Add(producer);
    }

    public IEnumerable<Producer> GetProducers()
    {
        return _producers;
    }
}
