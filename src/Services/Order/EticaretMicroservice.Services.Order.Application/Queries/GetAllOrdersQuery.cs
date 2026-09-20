using EticaretMicroservice.Services.Order.Application.Dtos;
using MediatR;
using System.Collections.Generic;

namespace EticaretMicroservice.Services.Order.Application.Queries;

public record GetAllOrdersQuery : IRequest<List<OrderDto>>;