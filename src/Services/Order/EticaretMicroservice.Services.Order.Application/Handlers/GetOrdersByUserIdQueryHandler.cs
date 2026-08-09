using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Application.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Application.Handlers
{
    public class GetOrdersByUserIdQueryHandler : IRequestHandler<GetOrdersByUserIdQuery, List<OrderDto>>
    {
        private readonly IOrderRepository _orderRepository;

        public GetOrdersByUserIdQueryHandler(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task<List<OrderDto>> Handle(GetOrdersByUserIdQuery request, CancellationToken cancellationToken)
        {
            var orders = await _orderRepository.GetOrdersByUserIdAsync(request.UserId);

            // Manuel veya AutoMapper dönüşümü
            return orders.Select(o => new OrderDto
            {
                Id = o.Id,
                BuyerId = o.BuyerId,
                CreatedDate = o.CreatedDate,
                TotalPrice = o.GetTotalPrice,
                Address = new AddressDto
                {
                    City = o.Address.City,
                    District = o.Address.District,
                    Street = o.Address.Street,
                    ZipCode = o.Address.ZipCode,
                    Line = o.Address.Line
                },
                OrderItems = o.OrderItems.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Price = i.Price,
                    Quantity = i.Quantity
                }).ToList()
            }).ToList();
        }
    }
}
