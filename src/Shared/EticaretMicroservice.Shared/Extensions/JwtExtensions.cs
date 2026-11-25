using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Shared.Extensions;

public static class JwtExtensions
{
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string? signalRHubPath = null, // Opsiyonel SignalR rotası
        SecurityKey? signingKey = null)
    {
        var issuer = configuration["Jwt:Issuer"] ?? "EticaretIdentityServer";
        var audience = configuration["Jwt:Audience"] ?? "EticaretGateway";

        var publicKeyPem = configuration["Jwt:PublicKey"];
        var metadataAddress = configuration["Jwt:MetadataAddress"];

        SecurityKey? staticKey = signingKey ?? (string.IsNullOrWhiteSpace(publicKeyPem) ? null : JwtKeys.LoadPublicKey(publicKeyPem));

        if (staticKey is null && string.IsNullOrWhiteSpace(metadataAddress))
        {
            throw new InvalidOperationException(
                "JWT doğrulama anahtarı yapılandırması eksik! Jwt:MetadataAddress (Identity'nin openid-configuration adresi) ya da Jwt:PublicKey verilmeli.");
        }

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Servisler Docker ağı içinde HTTP ile konuşur; TLS'i dışarıdaki katman (load balancer) sonlandırır
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;

            if (staticKey is null)
                options.MetadataAddress = metadataAddress!;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = staticKey,
                // Yalnızca RS256 kabul edilir. Kütüphane RSA anahtarına karşı HS256 doğrulamayı zaten reddeder (algoritma karıştırma
                // saldırısı, Gateway testlerinde denenir); bu satır ek bir savunma katmanı ve niyeti açıkça belirtir.
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            if (!string.IsNullOrWhiteSpace(signalRHubPath))
            {
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments(signalRHubPath, StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            }
        });

        return services;
    }
}
