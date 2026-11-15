using EticaretMicroservice.Services.Order.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Application.Commands
{
    public class CreateOrderCommand : IRequest<int>
    {
        public string BuyerId { get; set; } = string.Empty;
        public AddressDto Address { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        public PaymentDto Payment { get; set; } = new();
    }

}
