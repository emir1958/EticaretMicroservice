namespace EticaretMicroservice.Shared.SampleData;

public record SampleProduct(
    string Id,
    string Name,
    string Category,
    decimal Price,
    decimal? CompareAtPrice,
    int Stock,
    string ImageId,
    string Description)
{
    public string ImageUrl => $"https://images.unsplash.com/photo-{ImageId}?auto=format&fit=crop&w=900&q=80";
}

// Geliştirme ortamı için örnek katalog. Id'ler sabittir: Catalog ürünleri, Order örnek siparişleri aynı kimlikleri kullanır.
public static class SampleCatalog
{
    public static readonly IReadOnlyList<SampleProduct> Products = new List<SampleProduct>
    {
        new("5a3d1e000000000000000001", "Tık 75 Mekanik Klavye", "Klavye & Mouse", 3499m, 3999m, 42, "1618384887929-16ec33fab9ef",
            "75% yerleşimli, hot-swap soketli mekanik klavye. Önceden yağlanmış lineer switch'ler, PBT tuş takımı ve üç cihaza kadar Bluetooth eşleşmesi."),
        new("5a3d1e000000000000000002", "Sessiz Kompakt Klavye", "Klavye & Mouse", 1299m, null, 80, "1541140532154-b024d705b90a",
            "Makas mekanizmalı, düşük profilli sessiz klavye. Tek şarjla üç aya kadar kullanım, Türkçe Q yerleşim."),
        new("5a3d1e000000000000000003", "Aydınlatmalı Tam Boy Klavye", "Klavye & Mouse", 1899m, 2199m, 35, "1547394765-185e1e68f34e",
            "Beyaz arka aydınlatmalı, sayısal tuş takımlı tam boy klavye. Alüminyum üst plaka ve çıkarılabilir USB-C kablo."),
        new("5a3d1e000000000000000004", "Retro Tuş Takımı", "Klavye & Mouse", 899m, null, 4, "1595225476474-87563907a212",
            "Çift enjeksiyon PBT, 132 tuşluk retro renk seti. MX uyumlu switch'lerle çalışır, Türkçe karakter tuşları dahildir."),
        new("5a3d1e000000000000000005", "Akış Ergonomik Mouse", "Klavye & Mouse", 1849m, null, 56, "1527864550417-7fd91fc51a46",
            "Avuç içini dolduran ergonomik gövde, sessiz tıklama ve serbest dönen metal tekerlek. Camda bile çalışan 8000 DPI sensör."),
        new("5a3d1e000000000000000006", "Gölge Kablosuz Mouse", "Klavye & Mouse", 1149m, 1399m, 64, "1615663245857-ac93bb7c39e7",
            "63 gram ağırlığında simetrik kablosuz mouse. 2.4 GHz alıcı ve Bluetooth, 70 saat pil ömrü."),
        new("5a3d1e000000000000000007", "Oyuncu RGB Mouse", "Klavye & Mouse", 1599m, null, 0, "1629429408209-1f912961dbd8",
            "26000 DPI optik sensör, altı programlanabilir tuş ve bölgesel RGB aydınlatma. Örgülü hafif kablo."),

        new("5a3d1e000000000000000008", "Ada ANC Kablosuz Kulaklık", "Ses", 4299m, 4999m, 28, "1546435770-a3e426bf472b",
            "Uyarlanabilir aktif gürültü engelleme, 40 mm sürücüler ve 45 saat pil. İki cihaza aynı anda bağlanır."),
        new("5a3d1e000000000000000009", "Stüdyo Kulak Üstü Kulaklık", "Ses", 2799m, null, 31, "1583394838336-acd977736f90",
            "Düz frekans yanıtlı kapalı tip stüdyo kulaklığı. Değiştirilebilir kadife pedler ve 3 metrelik sökülebilir kablo."),
        new("5a3d1e000000000000000010", "Retro Kablolu Kulaklık", "Ses", 1399m, 1699m, 3, "1484704849700-f032a568e944",
            "Deri baş bandı ve metal gövdeli, sıcak tınılı kablolu kulaklık. 3.5 mm ve 6.3 mm adaptör kutudan çıkar."),
        new("5a3d1e000000000000000011", "Tını Kulak İçi Kulaklık", "Ses", 1599m, null, 90, "1590658268037-6bf12165a8df",
            "Gürültü engellemeli gerçek kablosuz kulaklık. Kablosuz şarj kutusu ile toplam 30 saat dinleme, IPX4 ter dayanımı."),
        new("5a3d1e000000000000000012", "Tempo Spor Kulaklık", "Ses", 949m, 1149m, 47, "1606220588913-b3aacb4d2f46",
            "Kulak kancalı, IPX5 dayanımlı spor kulaklığı. Hızlı şarjla 10 dakikada bir saatlik kullanım."),
        new("5a3d1e000000000000000013", "Nota Masaüstü Hoparlör", "Ses", 2749m, null, 18, "1545454675-3531b543be5d",
            "Ceviz kaplama kabinli aktif raf tipi hoparlör çifti. Bluetooth, optik ve RCA girişleri; 2 × 40 W çıkış."),
        new("5a3d1e000000000000000014", "Fısıltı USB Mikrofon", "Ses", 2199m, 2499m, 22, "1590602847861-f357a9332bbc",
            "Kardioid kapsüllü USB-C yayın mikrofonu. Dokunmatik sessize alma, gecikmesiz kulaklık çıkışı ve masa kolu uyumu."),

        new("5a3d1e000000000000000015", "Ufuk 27\" 4K Monitör", "Ekran & Aydınlatma", 12999m, 14499m, 12, "1616763355548-1b606f439f86",
            "27 inç 4K IPS panel, %99 sRGB ve 65 W güç aktaran USB-C. Yükseklik ayarlı, dikey moda dönebilen ayak."),
        new("5a3d1e000000000000000016", "Loş Mimar Masa Lambası", "Ekran & Aydınlatma", 1099m, null, 38, "1507473885765-e6ed057f782c",
            "Eklemli çelik kollu masa lambası. E27 duy, mat antrasit boya ve ağır döküm taban."),
        new("5a3d1e000000000000000017", "Kor Ampul Masa Lambası", "Ekran & Aydınlatma", 849m, null, 5, "1593062096033-9a26b09da705",
            "Meşe tabanlı, kısılabilir filaman ampullü ortam lambası. Akşam çalışmaları için 2200 K sıcak ışık."),
        new("5a3d1e000000000000000018", "Çalışma Köşesi LED Lamba", "Ekran & Aydınlatma", 1399m, 1599m, 26, "1616400619175-5beda3a17896",
            "Beş renk sıcaklığı ve kademesiz parlaklık ayarlı LED lamba. Tabanda 10 W kablosuz şarj alanı."),

        new("5a3d1e000000000000000019", "Keçe Masa Matı 90×40", "Masa & Şarj", 549m, null, 120, "1518455027359-f3f8164ba6bd",
            "Geri dönüştürülmüş yün keçeden, kaymaz tabanlı masa matı. Klavye ve mouse'u birlikte taşır."),
        new("5a3d1e000000000000000020", "Alüminyum Dizüstü Standı", "Masa & Şarj", 899m, 1099m, 52, "1601524909162-ae8725290836",
            "Ekranı göz hizasına taşıyan tek parça alüminyum stand. 11–16 inç dizüstülerle uyumlu, kablo geçiş boşluklu."),
        new("5a3d1e000000000000000021", "Güç 65W GaN Şarj Cihazı", "Masa & Şarj", 999m, 1199m, 75, "1583863788434-e58a36330cf0",
            "İki USB-C ve bir USB-A çıkışlı kompakt GaN şarj cihazı. Dizüstü, tablet ve telefonu aynı anda şarj eder."),
        new("5a3d1e000000000000000022", "Halka Kablosuz Şarj Pedi", "Masa & Şarj", 749m, null, 0, "1615526675159-e248c3021d3f",
            "15 W manyetik kablosuz şarj pedi. 1.5 metre örgülü kablo ve kaymaz silikon taban."),

        new("5a3d1e000000000000000023", "Günlük Sırt Çantası 20L", "Çanta", 2299m, null, 33, "1553062407-98eeb64c6a62",
            "Su itici kumaştan, 16 inç dizüstü bölmeli şehir çantası. Gizli sırt cebi ve valiz sapı geçişi."),
        new("5a3d1e000000000000000024", "Deri Sırt Çantası", "Çanta", 3899m, 4499m, 9, "1622560480605-d83c853bc5c3",
            "Bitkisel tabaklanmış hakiki deriden el yapımı sırt çantası. 14 inç dizüstü bölmesi, pirinç tokalar."),
        new("5a3d1e000000000000000025", "Gezgin Kanvas Sırt Çantası", "Çanta", 2649m, null, 16, "1547949003-9792a18a2601",
            "Mumlu kanvas ve deri detaylı 25 litrelik çanta. Yuvarlanan üst kapak, yan şişe cebi ve tripod kayışları."),
    };
}
