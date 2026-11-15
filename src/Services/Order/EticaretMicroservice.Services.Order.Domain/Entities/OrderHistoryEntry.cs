using EticaretMicroservice.Services.Order.Domain.Enums;

namespace EticaretMicroservice.Services.Order.Domain.Entities;

// Siparişin geçmişindeki tek bir olay. Yalnızca Order ekler, sonradan değiştirilmez.
public class OrderHistoryEntry
{
    public int Id { get; private set; }
    public OrderHistoryType Type { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? Note { get; private set; }

    private OrderHistoryEntry() { }

    internal OrderHistoryEntry(OrderHistoryType type, DateTime occurredAt, string? note)
    {
        Type = type;
        OccurredAt = occurredAt;
        Note = note;
    }
}
