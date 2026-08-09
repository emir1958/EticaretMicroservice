using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EticaretMicroservice.Services.Order.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Kullanıcıya ait siparişleri getirir (Query)
        /// GET api/orders/user/123
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUserId(string userId)
        {
            var query = new GetOrdersByUserIdQuery(userId);
            var response = await _mediator.Send(query);
            return Ok(response);
        }

        /// <summary>
        /// Yeni bir sipariş oluşturur (Command)
        /// POST api/orders
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
        {
            var orderId = await _mediator.Send(command);
            return Ok(new { OrderId = orderId, Message = "Sipariş başarıyla oluşturuldu." });
        }
    }
}
