using System.Security.Claims;
using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Queries; // 👈 Query namespace'i eklendi
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EticaretMicroservice.Services.Order.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdFromToken))
        {
            return Unauthorized(new { message = "Geçersiz token veya kullanıcı kimliği bulunamadı." });
        }

        command.BuyerId = userIdFromToken;

        var orderId = await _mediator.Send(command);
        return Ok(new { OrderId = orderId });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetOrdersByUser()
    {
        var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdFromToken))
        {
            return Unauthorized(new { message = "Geçersiz token veya kullanıcı kimliği bulunamadı." });
        }

        // 🟢 MediatR sorgusu bağlandı (IDOR korumalı sipariş geçmişi)
        var result = await _mediator.Send(new GetOrdersByUserIdQuery(userIdFromToken));
        return Ok(result);
    }
}