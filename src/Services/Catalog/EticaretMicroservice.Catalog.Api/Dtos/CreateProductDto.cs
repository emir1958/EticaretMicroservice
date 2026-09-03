namespace EticaretMicroservice.Catalog.Api.Dtos;

public record CreateProductDto
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int InitialStock { get; init; } = 0; // Yeni ürünün başlangıç stoğu
}