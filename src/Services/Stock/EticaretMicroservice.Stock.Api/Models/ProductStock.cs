namespace EticaretMicroservice.Stock.Api.Models
{
    public class ProductStock
    {
        public int Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public int AvailableStock { get; set; }
        public int ReservedStock { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}