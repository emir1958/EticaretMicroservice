using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace EticaretMicroservice.Catalog.Api.Models
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Name")]
        public string Name { get; set; } = null!;

        [BsonElement("Description")]
        public string Description { get; set; } = null!;

        [BsonElement("Price")]
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        // İndirim öncesi liste fiyatı (yalnızca gösterim). Satış fiyatı her zaman Price'tır; Order bunu doğrular.
        public decimal? CompareAtPrice { get; set; }

        public bool IsSample { get; set; }
    }
}
