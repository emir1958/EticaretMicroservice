using Microsoft.Extensions.DependencyInjection;

namespace EticaretMicroservice.Shared.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "AllowFrontend";

    public static IServiceCollection AddSharedCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .WithExposedHeaders("X-Correlation-ID");
            });
        });

        return services;
    }
}