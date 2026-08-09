using EticaretMicroservice.Identity.Api.Dtos;

namespace EticaretMicroservice.Identity.Api.Services
{
    public interface IIdentityService
    {
        Task<bool> RegisterAsync(RegisterDto registerDto);
        Task<string?> LoginAsync(LoginDto loginDto); // Başarılıysa JWT token dönecek, başarısızsa null
    }
}
