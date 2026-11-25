namespace EticaretMicroservice.Services.Order.Application.Dtos
{
    public class OrderDto
    {
        public int Id { get; set; }
        public string BuyerId { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime? RefundedAt { get; set; }
        public DateTime CreatedDate { get; set; }
        public AddressDto Address { get; set; }
        public List<OrderItemDto> OrderItems { get; set; }
        public decimal TotalPrice { get; set; }
        // Yalnızca detay sorgusunda dolu; listeler geçmişi yüklemez
        public List<OrderHistoryDto> History { get; set; } = new();
    }
}
