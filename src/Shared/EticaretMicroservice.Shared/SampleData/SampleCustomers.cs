namespace EticaretMicroservice.Shared.SampleData;

public record SampleCustomer(string Id, string Username, string Email, string City, string District);

// Örnek müşteriler gerçek kullanıcılardan iki şekilde ayrılır: Id ön eki ve .test uzantılı e-posta alanı.
public static class SampleCustomers
{
    public const string IdPrefix = "5a3d1e00-0000-4000-8000-";
    public const string EmailDomain = "ornek.test";

    public static bool IsSample(string userId) => userId.StartsWith(IdPrefix, StringComparison.OrdinalIgnoreCase);

    public static readonly IReadOnlyList<SampleCustomer> Customers = new List<SampleCustomer>
    {
        Create(1, "Elif Yıldız", "elif.yildiz", "İstanbul", "Kadıköy"),
        Create(2, "Mert Demir", "mert.demir", "Ankara", "Çankaya"),
        Create(3, "Zeynep Kaya", "zeynep.kaya", "İzmir", "Karşıyaka"),
        Create(4, "Can Öztürk", "can.ozturk", "Bursa", "Nilüfer"),
        Create(5, "Selin Arslan", "selin.arslan", "Antalya", "Muratpaşa"),
        Create(6, "Burak Şahin", "burak.sahin", "İstanbul", "Beşiktaş"),
        Create(7, "Deniz Çelik", "deniz.celik", "Eskişehir", "Tepebaşı"),
        Create(8, "Ayşe Koç", "ayse.koc", "İzmir", "Bornova"),
        Create(9, "Emre Aydın", "emre.aydin", "Kocaeli", "İzmit"),
        Create(10, "İrem Polat", "irem.polat", "İstanbul", "Üsküdar"),
        Create(11, "Kerem Aksoy", "kerem.aksoy", "Adana", "Seyhan"),
        Create(12, "Naz Güneş", "naz.gunes", "Ankara", "Yenimahalle"),
    };

    private static SampleCustomer Create(int no, string name, string mailbox, string city, string district) =>
        new($"{IdPrefix}{no:D12}", name, $"{mailbox}@{EmailDomain}", city, district);
}
