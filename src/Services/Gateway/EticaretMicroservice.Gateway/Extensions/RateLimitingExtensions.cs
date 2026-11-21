using System.Security.Claims;
using System.Threading.RateLimiting;

namespace EticaretMicroservice.Gateway.Extensions;

public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth-policy";
    public const string BasketPolicy = "basket-policy";
    public const string OrderPolicy = "order-policy";

    // Her policy kendi partition'ını (kovasını) kullanıcı ya da IP başına üretir: bir istemcinin limiti doldurması
    // başkasını etkilemez. Partition'sız limiter (AddFixedWindowLimiter) tüm istemciler için tek kova olur.
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        return services.AddRateLimiter(options =>
        {
            // Giriş/kayıt: kimlik henüz yok, brute-force'u IP başına sınırlar
            options.AddPolicy(AuthPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(ClientIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            // Sepet her adet değişikliğinde yazılır, bu yüzden auth'tan çok daha geniş
            options.AddPolicy(BasketPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(UserOrIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));

            options.AddPolicy(OrderPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(UserOrIp(context), _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(10),
                    QueueLimit = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";
                await context.HttpContext.Response.WriteAsync(
                    "{\"message\": \"Çok fazla istek gönderildi. Lütfen bir süre sonra tekrar deneyin.\"}",
                    cancellationToken);
            };
        });
    }

    private static string ClientIp(HttpContext context) =>
        $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

    // Doğrulanmış token'daki kullanıcı; token yoksa (misafir ya da geçersiz token) IP'ye düşer
    private static string UserOrIp(HttpContext context)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;
        return string.IsNullOrEmpty(userId) ? ClientIp(context) : $"user:{userId}";
    }
}
