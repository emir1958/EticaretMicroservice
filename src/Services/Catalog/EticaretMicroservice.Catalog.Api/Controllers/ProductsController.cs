using EticaretMicroservice.Catalog.Api.Dtos;
using EticaretMicroservice.Catalog.Api.Models;
using EticaretMicroservice.Catalog.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

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
            // Id, ObjectId biçiminde değilse Mongo sorgusu FormatException (500) fırlatır
            if (!ObjectId.TryParse(id, out _))
                return NotFound(new { Message = "Ürün bulunamadı." });

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
                Price = dto.Price,
                ImageUrl = dto.ImageUrl ?? string.Empty,
                Category = dto.Category?.Trim() ?? string.Empty,
                CompareAtPrice = NormalizeCompareAtPrice(dto.CompareAtPrice, dto.Price)
            };

            var createdProduct = await _productService.CreateAsync(product);

            // Stok kaydını Stock servisi bu event'le açar
            await _publishEndpoint.Publish(new ProductCreatedEvent
            {
                ProductId = createdProduct.Id!,
                Name = createdProduct.Name,
                Price = createdProduct.Price,
                InitialStock = dto.InitialStock
            });

            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateProductDto dto)
        {
            var existingProduct = await _productService.GetByIdAsync(id);
            if (existingProduct == null)
                return NotFound(new { Message = "Güncellenecek ürün bulunamadı." });

            existingProduct.Name = dto.Name;
            existingProduct.Description = dto.Description;
            existingProduct.Price = dto.Price;
            existingProduct.Category = dto.Category?.Trim() ?? string.Empty;
            existingProduct.CompareAtPrice = NormalizeCompareAtPrice(dto.CompareAtPrice, dto.Price);

            existingProduct.ImageUrl = !string.IsNullOrWhiteSpace(dto.ImageUrl)
                ? dto.ImageUrl
                : existingProduct.ImageUrl ?? string.Empty;

            var isUpdated = await _productService.UpdateAsync(existingProduct);
            if (!isUpdated)
                return BadRequest(new { Message = "Güncelleme başarısız." });

            // Stok burada güncellenmez: sahibi Stock servisidir (PUT /api/stocks/{productId})
            return NoContent();
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

        // Satış fiyatından yüksek değilse indirim sayılmaz
        private static decimal? NormalizeCompareAtPrice(decimal? compareAtPrice, decimal price) =>
            compareAtPrice > price ? compareAtPrice : null;
    }
}