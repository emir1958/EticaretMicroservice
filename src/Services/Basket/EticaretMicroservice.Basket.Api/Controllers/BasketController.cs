using System.Security.Claims;
using EticaretMicroservice.Basket.Api.Models;
using EticaretMicroservice.Basket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EticaretMicroservice.Basket.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BasketController : ControllerBase
{
    private readonly IBasketService _basketService;

    public BasketController(IBasketService basketService)
    {
        _basketService = basketService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBasket()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var basket = await _basketService.GetBasketAsync(userId);
        return Ok(basket ?? new CustomerBasket { UserId = userId });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateBasket([FromBody] CustomerBasket basket)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        basket.UserId = userId; // Token'daki UserId garanti edilir
        var updatedBasket = await _basketService.UpdateBasketAsync(basket);
        return Ok(updatedBasket);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBasket()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _basketService.DeleteBasketAsync(userId);
        return Ok();
    }
}