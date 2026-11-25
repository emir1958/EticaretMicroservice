using EticaretMicroservice.Identity.Api.Dtos;

namespace EticaretMicroservice.Identity.Api.Services
{
    public record TokenPair(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

    public interface IIdentityService
    {
        Task<bool> RegisterAsync(RegisterDto registerDto);
        Task<TokenPair?> LoginAsync(LoginDto loginDto); // Başarısızsa null
        Task<TokenPair?> RefreshAsync(string refreshToken); // Geçersiz, süresi dolmuş ya da iptal edilmişse null
        Task RevokeAsync(string refreshToken); // Kullanıcının tüm refresh token'larını geçersiz kılar
    }
}
