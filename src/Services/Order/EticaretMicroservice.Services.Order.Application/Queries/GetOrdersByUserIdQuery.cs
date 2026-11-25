using EticaretMicroservice.Services.Order.Application.Dtos;
using MediatR;

namespace EticaretMicroservice.Services.Order.Application.Queries
{
    public class GetOrdersByUserIdQuery : IRequest<List<OrderDto>>
    {
        public string UserId { get; set; }

        public GetOrdersByUserIdQuery(string userId)
        {
            UserId = userId;
        }
    }
}
