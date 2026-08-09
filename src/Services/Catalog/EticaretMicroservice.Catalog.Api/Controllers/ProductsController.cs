using EticaretMicroservice.Catalog.Api.Models;
using EticaretMicroservice.Catalog.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EticaretMicroservice.Catalog.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        // 🟢 Herkes (giriş yapmış User veya Admin) tüm ürünleri görebilir
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        // 🟢 Herkes (giriş yapmış User veya Admin) tek bir ürünü inceleyebilir
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { Message = "Ürün bulunamadı." });

            return Ok(product);
        }

        // 🔴 Sadece "Admin" rolündeki kullanıcılar yeni ürün ekleyebilir
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            var createdProduct = await _productService.CreateAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

        // 🔴 Sadece "Admin" rolündeki kullanıcılar ürün silebilir
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            var isDeleted = await _productService.DeleteAsync(id);
            if (!isDeleted)
                return NotFound(new { Message = "Silinecek ürün bulunamadı." });

            return NoContent();
        }
    }
}