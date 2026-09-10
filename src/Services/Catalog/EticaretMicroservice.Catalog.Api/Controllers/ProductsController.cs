using EticaretMicroservice.Catalog.Api.Dtos;
using EticaretMicroservice.Catalog.Api.Models;
using EticaretMicroservice.Catalog.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;
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
        private readonly IPublishEndpoint _publishEndpoint;

        public ProductsController(IProductService productService, IPublishEndpoint publishEndpoint)
        {
            _productService = productService;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(string id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return NotFound(new { Message = "Ürün bulunamadı." });

            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price
            };

            var createdProduct = await _productService.CreateAsync(product);

            // 🟢 ASENKRON EVENT: Stock API'ye yeni ürünün stok kaydını açması için haber veriyoruz
            await _publishEndpoint.Publish(new ProductCreatedEvent
            {
                ProductId = createdProduct.Id!,
                Name = createdProduct.Name,
                Price = createdProduct.Price,
                InitialStock = dto.InitialStock
            });

            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

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