using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Queries;
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

    // Admin sipariş listesi: sayfalı, durum/arama/müşteri filtreli. Yanıt: { items, totalCount, page, pageSize, statusCounts }
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SearchOrders(
        [FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? buyerId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15)
    {
        return Ok(await _mediator.Send(new SearchOrdersQuery(q, status, buyerId, page, pageSize)));
    }

    // Raporlar: from (dahil) ve to (hariç) kesin birer an olarak gelir (örn. 2026-11-15T21:00:00Z); istemci kendi gün sınırını belirler
    [HttpGet("stats/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSalesSummary([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
        => Ok(await _mediator.Send(new GetSalesSummaryQuery(from, to)));

    [HttpGet("stats/daily")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetDailySales([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to)
        => Ok(await _mediator.Send(new GetDailySalesQuery(from, to)));

    [HttpGet("stats/products")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetProductSales([FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, [FromQuery] int limit = 100)
        => Ok(await _mediator.Send(new GetProductSalesQuery(from, to, limit)));

    [HttpGet("stats/customers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCustomerStats([FromQuery] string[]? buyerIds)
        => Ok(await _mediator.Send(new GetCustomerStatsQuery(buyerIds is { Length: > 0 } ? buyerIds : null)));

    [HttpGet("stats/customers/summary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCustomerSummary()
        => Ok(await _mediator.Send(new GetCustomerSummaryQuery()));

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

        var userIdFromToken = GetUserIdFromToken();

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
        var userIdFromToken = GetUserIdFromToken();

        if (string.IsNullOrEmpty(userIdFromToken))
        {
            return Unauthorized(new { message = "Geçersiz token veya kullanıcı kimliği bulunamadı." });
        }

        var result = await _mediator.Send(new GetOrdersByUserIdQuery(userIdFromToken));
        return Ok(result);
    }

    // Başka kullanıcının siparişi için 403 değil 404 döner: siparişin var olduğu bilgisi de sızdırılmaz (IDOR).
    [HttpGet("user/{id:int}")]
    public async Task<IActionResult> GetOrderByIdForUser(int id)
    {
        var userIdFromToken = GetUserIdFromToken();

        if (string.IsNullOrEmpty(userIdFromToken))
        {
            return Unauthorized(new { message = "Geçersiz token veya kullanıcı kimliği bulunamadı." });
        }

        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        if (order == null || order.BuyerId != userIdFromToken)
            return NotFound(new { message = "Sipariş bulunamadı." });

        return Ok(order);
    }

    private string? GetUserIdFromToken() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
}