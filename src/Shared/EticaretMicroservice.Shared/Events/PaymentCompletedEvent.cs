using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Shared.Events
{
    public record PaymentCompletedEvent
    {
        public int OrderId { get; init; }
        public string BuyerId { get; init; }
    }
}
