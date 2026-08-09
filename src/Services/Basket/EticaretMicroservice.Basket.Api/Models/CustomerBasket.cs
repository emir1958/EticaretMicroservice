namespace EticaretMicroservice.Basket.Api.Models
{
    public class CustomerBasket
    {
        public string UserId { get; set; } = string.Empty;
        public List<BasketItem> Items { get; set; } = new();

        // Toplam tutarı dinamik hesaplayan property
        public decimal TotalPrice => Items.Sum(x => x.Price * x.Quantity);
    }
}
