namespace EticaretMicroservice.Services.Order.Application.Dtos;

public class OrderHistoryDto
{
    public string Type { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? Note { get; set; }
}
