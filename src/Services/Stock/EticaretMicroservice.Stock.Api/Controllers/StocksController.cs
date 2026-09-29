using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/stocks")]
public class StocksController : ControllerBase
{
    private readonly StockDbContext _context;

    public StocksController(StockDbContext context)
    {
        _context = context;
    }

    // GET: api/stocks/prod-1
    [AllowAnonymous]
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetStockByProductId(string productId)
    {
        var stock = await _context.ProductStocks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
            return NotFound(new { Message = "Stok bilgisi bulunamadı." });

        return Ok(stock);
    }

    // GET: api/stocks
    [HttpGet]
    public async Task<IActionResult> GetAllStocks()
    {
        var stocks = await _context.ProductStocks.AsNoTracking().ToListAsync();
        return Ok(stocks);
    }

    // 🟢 EKLENDİ: PUT api/stocks/{productId} -> StockDb üzerindeki AvailableStock alanını günceller
    [HttpPut("{productId}")]
    public async Task<IActionResult> UpdateStock(string productId, [FromBody] UpdateStockDto dto)
    {
        var stock = await _context.ProductStocks.FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
        {
            stock = new ProductStock
            {
                ProductId = productId,
                AvailableStock = dto.AvailableStock,
                ReservedStock = 0
            };
            await _context.ProductStocks.AddAsync(stock);
        }
        else
        {
            stock.AvailableStock = dto.AvailableStock;
        }

        await _context.SaveChangesAsync();
        return Ok(stock);
    }
}

public record UpdateStockDto(int AvailableStock);