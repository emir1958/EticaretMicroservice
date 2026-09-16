using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Shared.Events
{
    public record StockUpdatedEvent
    {
        public string ProductId { get; init; } = default!;
        public int NewStock { get; init; }
    }
}
