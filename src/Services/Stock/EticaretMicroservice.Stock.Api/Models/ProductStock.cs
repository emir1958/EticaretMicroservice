namespace EticaretMicroservice.Stock.Api.Models
{
    public class ProductStock
    {
        public int Id { get; set; }
        public string ProductId { get; set; } // OrderItem tarafındaki ProductId ile eşleşecek
        public int AvailableStock { get; set; }
    }
}
