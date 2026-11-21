using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EticaretMicroservice.Shared.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "AllowFrontend";

    // Yapılandırma yoksa yerel geliştirme adresleri geçerlidir
    private static readonly string[] DefaultOrigins = { "http://localhost:5173", "http://localhost:3000" };

    // İzin verilen adresler "Cors:AllowedOrigins" listesinden okunur (ortam değişkeni: Cors__AllowedOrigins__0, __1 ...).
    // Seçenekler DI üzerinden yapılandırıldığı için servislerin AddSharedCors() çağrıları değişmez.
    public static IServiceCollection AddSharedCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IConfiguration>((options, configuration) =>
        {
            var configured = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            var origins = configured is { Length: > 0 } ? configured : DefaultOrigins;

            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins(origins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .WithExposedHeaders("X-Correlation-ID");
            });
        });

        return services;
    }
}
