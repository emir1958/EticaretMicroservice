namespace EticaretMicroservice.Catalog.Api.Dtos;

public record UpdateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Stock { get; set; }
    public string? ImageUrl { get; set; }
}