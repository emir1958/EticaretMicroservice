using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EticaretMicroservice.Identity.Api.Dtos;
using EticaretMicroservice.Identity.Api.Models;
using EticaretMicroservice.Identity.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Identity.Api.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly AppIdentityDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IdentityService> _logger;

        public IdentityService(
            AppIdentityDbContext context,
            IConfiguration configuration,
            ILogger<IdentityService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            var normalizedEmail = registerDto.Email.Trim().ToLowerInvariant();

            bool userExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (userExists)
                return false;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            // 🟢 GÜVENLİK DÜZELTMESİ: E-postadan rol türetme kaldırıldı. 
            // Dışarıdan kayıt olan tüm kullanıcılar varsayılan olarak "User" rolünü alır.
            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = registerDto.Username.Trim(),
                Email = normalizedEmail,
                PasswordHash = hashedPassword,
                Role = "User"
            };

            try
            {
                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                // Eşzamanlı isteklerde Unique Index ihlal edilirse güvenli dönüş
                _logger.LogWarning(ex, "Mükerrer e-posta kaydı engellendi: {Email}", normalizedEmail);
                return false;
            }
        }

        public async Task<string?> LoginAsync(LoginDto loginDto)
        {
            var normalizedEmail = loginDto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null) return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isPasswordValid) return null;

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Shared kütüphanesindeki fallback anahtarla tam uyumlu secret
            var secretKey = _configuration["JwtSettings:Secret"]
                            ?? _configuration["Jwt:Secret"]
                            ?? "SuperSecretKey_For_Jwt_Auth_EticaretMicroservice_2026_Secure_Key!";

            var key = Encoding.ASCII.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}