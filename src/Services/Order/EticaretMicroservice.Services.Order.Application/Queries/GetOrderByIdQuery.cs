using EticaretMicroservice.Services.Order.Application.Dtos;
using MediatR;

namespace EticaretMicroservice.Services.Order.Application.Queries;

public record GetOrderByIdQuery(int Id) : IRequest<OrderDto?>;