using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Shared.Extensions;

// EticaretMicroservice.Shared/Extensions/JwtExtensions.cs
public static class JwtExtensions
{
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string? signalRHubPath = null) // 👈 Opsiyonel SignalR rotası
    {
        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey yapılandırması eksik!");
        var issuer = configuration["Jwt:Issuer"] ?? "EticaretIdentityServer";
        var audience = configuration["Jwt:Audience"] ?? "EticaretGateway";

        var key = Encoding.UTF8.GetBytes(secretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            // 🟢 WEBSOCKET / SIGNALR QUERY STRING YAKALAMA MANTIĞI:
            // Standart HTTP isteklerinde header şöyledir: "Authorization: Bearer <token>"
            // Ancak tarayıcılar (JavaScript WebSocket API) handshake sırasında custom header ekleyemez!
            // Bu yüzden SignalR istemcisi token'ı mecbur URL sonuna "?access_token=..." olarak ekler.
            // Buradaki kod gelen isteği dinler; eğer rota SignalR rotasıysa URL'den token'ı söker 
            // ve JwtBearer middleware'ine "İşte doğrulanacak token bu" der.
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