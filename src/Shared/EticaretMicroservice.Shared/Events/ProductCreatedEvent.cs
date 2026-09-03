namespace EticaretMicroservice.Shared.Events;

public record ProductCreatedEvent
{
    public string ProductId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int InitialStock { get; init; }
}