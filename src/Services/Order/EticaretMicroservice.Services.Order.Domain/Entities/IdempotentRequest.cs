using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Domain.Entities
{
    public class IdempotentRequest
    {
        public Guid Id { get; set; } 
        public string OperationName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? StatusCode { get; set; }
        public string? ResponseBody { get; set; } 
        public bool IsCompleted { get; set; } 
    }
}
