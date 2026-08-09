using EticaretMicroservice.Services.Order.Application.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Application.Commands
{
    // MediatR'ın IRequest arabirimini türetiyoruz. Dönen yanıt tipi bool veya int (Order ID) olabilir.
    public class CreateOrderCommand : IRequest<int>
    {
        public string BuyerId { get; set; }
        public AddressDto Address { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        public PaymentDto Payment { get; set; } = new();
    }

}
