using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Queries; // 👈 Query namespace'i eklendi
using EticaretMicroservice.Services.Order.Infrastructure.Filters;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
    [HttpGet]
    [Authorize(Roles = "Admin")]
    // İsteğe bağlı rol kontrolü: [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _mediator.Send(new GetAllOrdersQuery());
        return Ok(orders);
    }

    // GET: api/Orders/5 (Admin detay sayfası için tekil sipariş getirme)
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        if (order == null)
            return NotFound(new { message = "Sipariş bulunamadı." });

        return Ok(order);
    }

    [HttpPost]
    [Idempotent]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
    {
        if (User.IsInRole("Admin"))
        {
            return BadRequest(new { message = "Yönetici (Admin) hesapları üzerinden sipariş verilemez." });
        }

        var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdFromToken))
        {
            return Unauthorized(new { message = "Geçersiz token veya kullanıcı kimliği bulunamadı." });
        }

        command.BuyerId = userIdFromToken;
        if (Request.Headers.TryGetValue("X-Correlation-ID", out var correlationHeader) &&
        Guid.TryParse(correlationHeader.FirstOrDefault(), out var correlationId))
        {
            command.CorrelationId = correlationId;
        }
        else
        {
            command.CorrelationId = Guid.NewGuid();
        }
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