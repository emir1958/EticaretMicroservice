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
    // GET: api/Orders (Tüm siparişleri getirir - Admin)
    [HttpGet]
    // İsteğe bağlı rol kontrolü: [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _mediator.Send(new GetAllOrdersQuery());
        return Ok(orders);
    }

    // GET: api/Orders/5 (Admin detay sayfası için tekil sipariş getirme)
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        return Ok(order);
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