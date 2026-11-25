using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EticaretMicroservice.IntegrationTests.Infrastructure;
using EticaretMicroservice.Services.Order.Domain.Entities;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EticaretMicroservice.IntegrationTests;

// Admin sipariş listesi ve raporları. Paylaşılan veritabanında diğer testlerin siparişleri (bugünün tarihli) rapor toplamlarını
// bozmasın diye her test kendine ait uzak bir tarih penceresi (2001-2010) ve benzersiz müşteri kullanır.
[Collection(OrderApiCollection.Name)]
public class OrderReportTests
{
    private readonly OrderApiFactory _factory;
    private readonly HttpClient _client;

    public OrderReportTests(OrderApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ---------- Liste (sayfalama, filtre, arama) ----------

    [Fact]
    public async Task Search_ReturnsNewestFirst_WithPagingAndTotals()
    {
        var buyer = NewBuyer();
        var start = Window(2001);
        var ids = new List<int>();
        for (var i = 0; i < 5; i++)
            ids.Add(await Seed(buyer, start.AddDays(i), OrderStatus.Tamamlandı, Item("p", 10, 1)));

        var page2 = await GetJsonAsync($"/api/orders?buyerId={buyer}&pageSize=2&page=2");
        var page3 = await GetJsonAsync($"/api/orders?buyerId={buyer}&pageSize=2&page=3");

        page2.GetProperty("totalCount").GetInt32().Should().Be(5);
        page2.GetProperty("items").EnumerateArray().Select(x => x.GetProperty("id").GetInt32())
            .Should().Equal(ids[2], ids[1]); // en yeniden eskiye: 5,4 | 3,2 | 1
        page3.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task Search_FiltersByStatus_AndReportsCountsPerStatusIgnoringTheStatusFilter()
    {
        var buyer = NewBuyer();
        var start = Window(2002);
        await Seed(buyer, start, OrderStatus.Tamamlandı, Item("p", 10, 1));
        await Seed(buyer, start.AddDays(1), OrderStatus.Tamamlandı, Item("p", 10, 1));
        await Seed(buyer, start.AddDays(2), OrderStatus.IptalEdildi, Item("p", 10, 1));
        await Seed(buyer, start.AddDays(3), OrderStatus.Beklemede, Item("p", 10, 1));

        var result = await GetJsonAsync($"/api/orders?buyerId={buyer}&status={Uri.EscapeDataString("Tamamlandı")}");

        result.GetProperty("totalCount").GetInt32().Should().Be(2);
        result.GetProperty("items").EnumerateArray().Should().OnlyContain(x => x.GetProperty("orderStatus").GetString() == "Tamamlandı");
        var counts = result.GetProperty("statusCounts");
        counts.GetProperty("Tamamlandı").GetInt32().Should().Be(2);
        counts.GetProperty("IptalEdildi").GetInt32().Should().Be(1);
        counts.GetProperty("Beklemede").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Search_MatchesOrderNumberRecipientAndCity_IgnoringCaseAndDiacritics()
    {
        var buyer = NewBuyer();
        var id = await SeedAsync(buyer, Window(2003), OrderStatus.Tamamlandı, new[] { Item("p", 10, 1) }, contactName: "Çağlar Öztürk", city: "İstanbul");
        var baseUrl = $"/api/orders?buyerId={buyer}&q=";

        (await TotalAsync(baseUrl + "istanbul")).Should().Be(1, "İ harfi i ile eşleşmeli");
        (await TotalAsync(baseUrl + "caglar")).Should().Be(1, "ç ve ğ aksansız yazılabilmeli");
        (await TotalAsync(baseUrl + "OZTURK")).Should().Be(1, "büyük/küçük harf fark etmemeli");
        (await TotalAsync(baseUrl + Uri.EscapeDataString($"#{id}"))).Should().Be(1, "sipariş numarası '#' ile de aranabilmeli");
        (await TotalAsync(baseUrl + "caglar%20istanbul")).Should().Be(1, "birden çok terim birlikte sağlanmalı");
        (await TotalAsync(baseUrl + "caglar%20ankara")).Should().Be(0);
        (await TotalAsync(baseUrl + "ankara")).Should().Be(0);
    }

    [Theory]
    [InlineData("yilmaz")]     // noktalı i ile yazılan, veride noktasız ı
    [InlineData("yılmaz")]
    [InlineData("YILMAZ")]
    [InlineData("YİLMAZ")]
    [InlineData("isparta")]    // veride büyük noktasız I
    [InlineData("ısparta")]
    [InlineData("ISPARTA")]
    [InlineData("celik")]      // aksansız yazım
    public async Task Search_TreatsDottedAndDotlessIAsTheSame(string query)
    {
        var buyer = NewBuyer();
        await SeedAsync(buyer, Window(2004).AddDays(40), OrderStatus.Tamamlandı, new[] { Item("p", 10, 1) }, contactName: "Ahmet Yılmaz Çelik", city: "Isparta");

        (await TotalAsync($"/api/orders?buyerId={buyer}&q={Uri.EscapeDataString(query)}")).Should().Be(1, $"'{query}' araması eşleşmeli");
    }

    [Fact]
    public async Task Search_ReturnsItemCountAndTotalComputedFromItems()
    {
        var buyer = NewBuyer();
        await Seed(buyer, Window(2004), OrderStatus.Tamamlandı, Item("a", 10, 2), Item("b", 5, 1));

        var row = (await GetJsonAsync($"/api/orders?buyerId={buyer}")).GetProperty("items")[0];

        row.GetProperty("itemCount").GetInt32().Should().Be(3);
        row.GetProperty("totalPrice").GetDecimal().Should().Be(25m);
        row.GetProperty("paymentStatus").GetString().Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("/api/orders")]
    [InlineData("/api/orders/stats/summary?from=2001-01-01T00:00:00Z&to=2001-02-01T00:00:00Z")]
    [InlineData("/api/orders/stats/daily?from=2001-01-01T00:00:00Z&to=2001-02-01T00:00:00Z")]
    [InlineData("/api/orders/stats/products?from=2001-01-01T00:00:00Z&to=2001-02-01T00:00:00Z")]
    [InlineData("/api/orders/stats/customers")]
    [InlineData("/api/orders/stats/customers/summary")]
    public async Task ReportEndpoints_RequireTheAdminRole(string url)
    {
        (await SendAsync(url, userId: null, role: null)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await SendAsync(url, userId: "some-customer", role: "User")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("/api/orders?pageSize=0")]
    [InlineData("/api/orders?pageSize=1000")]
    [InlineData("/api/orders?page=0")]
    [InlineData("/api/orders?status=Bilinmeyen")]
    [InlineData("/api/orders/stats/summary?from=2001-02-01T00:00:00Z&to=2001-01-01T00:00:00Z")]
    [InlineData("/api/orders/stats/daily?from=2001-01-01T00:00:00Z&to=2005-01-01T00:00:00Z")]
    [InlineData("/api/orders/stats/products?from=2001-01-01T00:00:00Z&to=2001-02-01T00:00:00Z&limit=0")]
    public async Task ReportEndpoints_RejectInvalidInputWith400(string url)
    {
        (await SendAsync(url)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- Raporlar ----------

    [Fact]
    public async Task Summary_CountsEveryStatus_ButRevenueAndItemsOnlyFromCompletedOrders()
    {
        var buyer = NewBuyer();
        var start = Window(2005);
        await Seed(buyer, start.AddDays(1), OrderStatus.Tamamlandı, Item("a", 100, 1), Item("b", 50, 2)); // 200 TL, 3 adet
        await Seed(buyer, start.AddDays(2), OrderStatus.Tamamlandı, Item("a", 100, 1)); // 100 TL, 1 adet
        await Seed(buyer, start.AddDays(3), OrderStatus.IptalEdildi, Item("a", 999, 1));
        await Seed(buyer, start.AddDays(4), OrderStatus.Beklemede, Item("a", 888, 1));

        var summary = await GetJsonAsync(PeriodUrl("/api/orders/stats/summary", start, start.AddDays(10)));

        summary.GetProperty("revenue").GetDecimal().Should().Be(300m);
        summary.GetProperty("itemsSold").GetInt32().Should().Be(4);
        summary.GetProperty("orderCount").GetInt32().Should().Be(4);
        summary.GetProperty("completedCount").GetInt32().Should().Be(2);
        summary.GetProperty("pendingCount").GetInt32().Should().Be(1);
        summary.GetProperty("canceledCount").GetInt32().Should().Be(1);
        summary.GetProperty("averageOrderValue").GetDecimal().Should().Be(150m);
        // Bekleyen sipariş oranı bozmaz: 1 iptal / (2 tamamlanan + 1 iptal)
        summary.GetProperty("cancelRate").GetDouble().Should().BeApproximately(1.0 / 3, 1e-9);
    }

    [Fact]
    public async Task Summary_PeriodStartIsInclusive_AndEndIsExclusive()
    {
        var buyer = NewBuyer();
        var from = Window(2006);
        var to = from.AddDays(10);
        await Seed(buyer, from, OrderStatus.Tamamlandı, Item("a", 10, 1));        // tam başlangıç: dahil
        await Seed(buyer, to, OrderStatus.Tamamlandı, Item("a", 1000, 1));        // tam bitiş: hariç
        await Seed(buyer, from.AddTicks(-1), OrderStatus.Tamamlandı, Item("a", 5000, 1)); // başlangıçtan hemen önce: hariç

        var summary = await GetJsonAsync(PeriodUrl("/api/orders/stats/summary", from, to));

        summary.GetProperty("orderCount").GetInt32().Should().Be(1);
        summary.GetProperty("revenue").GetDecimal().Should().Be(10m);
    }

    [Fact]
    public async Task Summary_ForAnEmptyPeriod_ReturnsZeros()
    {
        var from = Window(2007);

        var summary = await GetJsonAsync(PeriodUrl("/api/orders/stats/summary", from, from.AddDays(5)));

        summary.GetProperty("revenue").GetDecimal().Should().Be(0);
        summary.GetProperty("orderCount").GetInt32().Should().Be(0);
        summary.GetProperty("averageOrderValue").GetDecimal().Should().Be(0);
        summary.GetProperty("cancelRate").GetDouble().Should().Be(0);
    }

    [Fact]
    public async Task Daily_BucketsByElapsedDaysFromPeriodStart_AndFillsEmptyDays()
    {
        var buyer = NewBuyer();
        // Yerel gece yarısını taklit eden, UTC'de yuvarlak olmayan başlangıç (Türkiye: UTC+3 → önceki gün 21:00Z)
        var from = Window(2008).AddHours(21);
        await Seed(buyer, from.AddHours(2), OrderStatus.Tamamlandı, Item("a", 10, 1));        // gün 0
        await Seed(buyer, from.AddHours(23).AddMinutes(59), OrderStatus.IptalEdildi, Item("a", 70, 1)); // gün 0 (24. saatten 1 dk önce)
        await Seed(buyer, from.AddHours(24), OrderStatus.Tamamlandı, Item("a", 20, 1));       // gün 1 (tam 24. saat)
        // gün 2 boş
        await Seed(buyer, from.AddDays(3).AddHours(5), OrderStatus.Tamamlandı, Item("a", 40, 1)); // gün 3

        var points = (await GetJsonAsync(PeriodUrl("/api/orders/stats/daily", from, from.AddDays(4)))).EnumerateArray().ToList();

        points.Should().HaveCount(4);
        points.Select(p => p.GetProperty("orders").GetInt32()).Should().Equal(2, 1, 0, 1);
        // Ciro yalnızca tamamlananlardan: iptal edilen 70 TL günlük ciroya girmez
        points.Select(p => p.GetProperty("revenue").GetDecimal()).Should().Equal(10m, 20m, 0m, 40m);
        points[1].GetProperty("date").GetDateTime().Should().Be(from.AddDays(1));
    }

    [Fact]
    public async Task ProductSales_AggregatesCompletedOrdersOnly_SortedByRevenue_AndHonoursLimit()
    {
        var buyer = NewBuyer();
        var start = Window(2009);
        await Seed(buyer, start.AddDays(1), OrderStatus.Tamamlandı, Item("pahali", 500, 1, "Pahalı Ürün"), Item("ucuz", 10, 3, "Ucuz Ürün"));
        await Seed(buyer, start.AddDays(2), OrderStatus.Tamamlandı, Item("ucuz", 10, 2, "Ucuz Ürün"));
        await Seed(buyer, start.AddDays(3), OrderStatus.IptalEdildi, Item("iptal", 9999, 9, "İptal Ürünü"));
        var url = PeriodUrl("/api/orders/stats/products", start, start.AddDays(10));

        var all = (await GetJsonAsync(url)).EnumerateArray().ToList();
        var top1 = (await GetJsonAsync(url + "&limit=1")).EnumerateArray().ToList();

        all.Select(p => p.GetProperty("productId").GetString()).Should().Equal("pahali", "ucuz");
        all[1].GetProperty("quantity").GetInt32().Should().Be(5);
        all[1].GetProperty("revenue").GetDecimal().Should().Be(50m);
        all[0].GetProperty("name").GetString().Should().Be("Pahalı Ürün");
        top1.Should().ContainSingle().Which.GetProperty("productId").GetString().Should().Be("pahali");
    }

    [Fact]
    public async Task CustomerStats_AggregatesPerBuyer_IncludingBuyersWithoutCompletedOrders()
    {
        var spender = NewBuyer();
        var waiting = NewBuyer();
        var start = Window(2010);
        await SeedAsync(spender, start, OrderStatus.Tamamlandı, new[] { Item("a", 100, 1) }, city: "Ankara");
        await SeedAsync(spender, start.AddDays(1), OrderStatus.Tamamlandı, new[] { Item("a", 50, 2) }, city: "İzmir");
        await SeedAsync(spender, start.AddDays(2), OrderStatus.IptalEdildi, new[] { Item("a", 999, 1) }, city: "Bursa");
        await SeedAsync(waiting, start, OrderStatus.Beklemede, new[] { Item("a", 70, 1) }, city: "Konya");

        var all = (await GetJsonAsync("/api/orders/stats/customers")).EnumerateArray().ToList();
        var mine = all.Single(c => c.GetProperty("buyerId").GetString() == spender);
        var other = all.Single(c => c.GetProperty("buyerId").GetString() == waiting);

        mine.GetProperty("orderCount").GetInt32().Should().Be(3);
        mine.GetProperty("completedCount").GetInt32().Should().Be(2);
        mine.GetProperty("totalSpent").GetDecimal().Should().Be(200m);
        mine.GetProperty("city").GetString().Should().Be("Bursa", "şehir, müşterinin en son siparişinden alınır");
        other.GetProperty("completedCount").GetInt32().Should().Be(0);
        other.GetProperty("totalSpent").GetDecimal().Should().Be(0);
    }

    [Fact]
    public async Task CustomerStats_WithBuyerIds_ReturnsOnlyThoseBuyers()
    {
        var wanted = NewBuyer();
        var unrelated = NewBuyer();
        var start = Window(2011);
        await Seed(wanted, start, OrderStatus.Tamamlandı, Item("a", 10, 1));
        await Seed(unrelated, start, OrderStatus.Tamamlandı, Item("a", 10, 1));

        var result = (await GetJsonAsync($"/api/orders/stats/customers?buyerIds={wanted}&buyerIds=siparisi-olmayan")).EnumerateArray().ToList();

        result.Should().ContainSingle().Which.GetProperty("buyerId").GetString().Should().Be(wanted);
    }

    [Fact]
    public async Task CustomerSummary_CountsBuyersWithCompletedOrders_AndRepeatBuyers()
    {
        var before = await GetJsonAsync("/api/orders/stats/customers/summary");

        var repeat = NewBuyer();
        var once = NewBuyer();
        var start = Window(2012);
        await Seed(repeat, start, OrderStatus.Tamamlandı, Item("a", 100, 1));
        await Seed(repeat, start.AddDays(1), OrderStatus.Tamamlandı, Item("a", 50, 1));
        await Seed(once, start, OrderStatus.Tamamlandı, Item("a", 30, 1));
        await Seed(once, start.AddDays(1), OrderStatus.IptalEdildi, Item("a", 999, 1));
        await Seed(NewBuyer(), start, OrderStatus.Beklemede, Item("a", 70, 1));

        var after = await GetJsonAsync("/api/orders/stats/customers/summary");

        // Paylaşılan veritabanında başka testlerin siparişleri de var: yalnızca bu testin eklediği fark ölçülür
        int Delta(string name) => after.GetProperty(name).GetInt32() - before.GetProperty(name).GetInt32();
        Delta("buyerCount").Should().Be(2);
        Delta("repeatBuyerCount").Should().Be(1);
        (after.GetProperty("totalSpent").GetDecimal() - before.GetProperty("totalSpent").GetDecimal()).Should().Be(180m);
    }

    // ---------- Yardımcılar ----------

    private static string NewBuyer() => $"report-buyer-{Guid.NewGuid():N}";

    // Her test için ayrı, uzak bir yıl: başka testlerin (bugünün tarihli) siparişleri pencereye girmez
    private static DateTime Window(int year) => new(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed record SeedItem(string ProductId, decimal Price, int Quantity, string? Name = null);

    private static SeedItem Item(string productId, decimal price, int quantity, string? name = null) => new(productId, price, quantity, name);

    private Task<int> Seed(string buyerId, DateTime createdUtc, OrderStatus status, params SeedItem[] items) =>
        SeedAsync(buyerId, createdUtc, status, items);

    // Siparişi domain metotlarıyla DB'ye yazar (saga'ya girmez), sonra oluşturulma tarihini istenen değere çeker
    private async Task<int> SeedAsync(string buyerId, DateTime createdUtc, OrderStatus status, SeedItem[] items,
        string contactName = "Test Alıcı", string city = "İstanbul")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = new Order(buyerId, new Address(contactName, "", city, "Merkez", "Cadde", "", "No:1"), Guid.NewGuid());
        foreach (var item in items)
            order.AddOrderItem(item.ProductId, item.Name ?? $"Ürün {item.ProductId}", item.Price, item.Quantity);

        switch (status)
        {
            case OrderStatus.Tamamlandı:
                order.TryMarkPaymentReceived(createdUtc);
                order.TrySetStatusToCompleted(createdUtc);
                break;
            case OrderStatus.IptalEdildi:
                order.TrySetStatusToCanceled(createdUtc);
                break;
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await db.Orders.Where(o => o.Id == order.Id).ExecuteUpdateAsync(s => s.SetProperty(o => o.CreatedDate, createdUtc));
        return order.Id;
    }

    private static string PeriodUrl(string path, DateTime from, DateTime to) =>
        $"{path}?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";

    private Task<HttpResponseMessage> SendAsync(string url, string? userId = "report-admin", string? role = "Admin")
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (userId is not null) request.Headers.Add(TestAuthHandler.UserIdHeader, userId);
        if (role is not null) request.Headers.Add(TestAuthHandler.RoleHeader, role);
        return _client.SendAsync(request);
    }

    private async Task<JsonElement> GetJsonAsync(string url)
    {
        var response = await SendAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<int> TotalAsync(string url) => (await GetJsonAsync(url)).GetProperty("totalCount").GetInt32();
}
