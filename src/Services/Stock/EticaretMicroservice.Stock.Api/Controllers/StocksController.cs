using EticaretMicroservice.Stock.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Stock.Api.Controllers;

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
    [HttpGet("{productId}")]
    public async Task<IActionResult> GetStockByProductId(string productId)
    {
        var stock = await _context.ProductStocks
            .FirstOrDefaultAsync(x => x.ProductId == productId);

        if (stock == null)
            return NotFound(new { Message = "Stok bilgisi bulunamadı." });

        return Ok(stock);
    }

    // GET: api/stocks
    [HttpGet]
    public async Task<IActionResult> GetAllStocks()
    {
        var stocks = await _context.ProductStocks.ToListAsync();
        return Ok(stocks);
    }
}