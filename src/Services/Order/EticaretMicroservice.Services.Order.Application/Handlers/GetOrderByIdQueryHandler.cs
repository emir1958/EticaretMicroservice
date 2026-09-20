using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Application.Queries;
using MediatR;

namespace EticaretMicroservice.Services.Order.Application.Handlers;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id);

        if (order == null) return null;

        return new OrderDto
        {
            Id = order.Id,
            BuyerId = order.BuyerId,
            CreatedDate = order.CreatedDate,
            TotalPrice = order.GetTotalPrice,
            Address = new AddressDto
            {
                City = order.Address.City,
                District = order.Address.District,
                Street = order.Address.Street,
                ZipCode = order.Address.ZipCode,
                Line = order.Address.Line
            },
            OrderItems = order.OrderItems?.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Price = i.Price,
                Quantity = i.Quantity
            }).ToList() ?? new()
        };
    }
}