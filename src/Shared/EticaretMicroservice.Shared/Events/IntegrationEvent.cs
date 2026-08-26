using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Shared.Events
{
    public abstract record IntegrationEvent
    {
        public Guid CorrelationId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
