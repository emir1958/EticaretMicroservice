using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EticaretMicroservice.Identity.Api.Dtos;
using EticaretMicroservice.Identity.Api.Models;
using EticaretMicroservice.Identity.Api.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Identity.Api.Services
{
    public class IdentityService : IIdentityService
    {
        // Refresh token'ın "aud" değeri access token'dan farklıdır: servisler (ValidAudience = EticaretGateway) refresh token'ı
        // access token olarak kabul etmez.
        private const string RefreshAudience = "EticaretIdentityRefresh";
        private const string TokenVersionClaim = "tv";

        private readonly AppIdentityDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IdentityService> _logger;
        private readonly RsaSecurityKey _signingKey;

        public IdentityService(
            AppIdentityDbContext context,
            IConfiguration configuration,
            ILogger<IdentityService> logger,
            RsaSecurityKey signingKey)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _signingKey = signingKey;
        }

        private string Issuer => _configuration["Jwt:Issuer"] ?? "EticaretIdentityServer";

        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            var normalizedEmail = registerDto.Email.Trim().ToLowerInvariant();

            bool userExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail);
            if (userExists)
                return false;

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            // Kayıtla açılan her hesap "User" rolündedir; rol istekten veya e-postadan türetilmez
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

        public async Task<TokenPair?> LoginAsync(LoginDto loginDto)
        {
            var normalizedEmail = loginDto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
            if (user == null) return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isPasswordValid) return null;

            return IssueTokens(user);
        }

        // Refresh token, Identity'nin imzaladığı bir JWT'dir; sunucuda saklanmaz. İptal, kullanıcının TokenVersion değeri
        // artırılarak yapılır: token'daki sürüm kullanıcınınkiyle eşleşmiyorsa geçersizdir.
        public async Task<TokenPair?> RefreshAsync(string refreshToken)
        {
            var parsed = ReadRefreshToken(refreshToken);
            if (parsed is null) return null;

            // Rol gibi bilgiler her yenilemede veritabanından okunur: değişiklik en geç access token ömrü kadar sürede yansır
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == parsed.Value.UserId);
            if (user == null || user.TokenVersion != parsed.Value.TokenVersion) return null;

            return IssueTokens(user);
        }

        // Çıkış: sürüm artar, kullanıcının elindeki (ve kopyalanmış) tüm refresh token'lar geçersiz olur
        public async Task RevokeAsync(string refreshToken)
        {
            var parsed = ReadRefreshToken(refreshToken);
            if (parsed is null) return;

            // Sürüm koşulu: zaten iptal edilmiş eski bir token, sonradan açılmış yeni oturumları kapatamaz
            await _context.Users
                .Where(u => u.Id == parsed.Value.UserId && u.TokenVersion == parsed.Value.TokenVersion)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.TokenVersion, u => u.TokenVersion + 1));
        }

        private TokenPair IssueTokens(User user)
        {
            var expiresAt = DateTime.UtcNow.AddDays(_configuration.GetValue("Jwt:RefreshTokenDays", 14.0));
            return new TokenPair(GenerateJwtToken(user), GenerateRefreshToken(user, expiresAt), expiresAt);
        }

        private string GenerateRefreshToken(User user, DateTime expiresAt)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(TokenVersionClaim, user.TokenVersion.ToString())
                }),
                Expires = expiresAt,
                Issuer = Issuer,
                Audience = RefreshAudience,
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
            });

            return tokenHandler.WriteToken(token);
        }

        // İmza, süre, issuer ve audience doğrulanır; geçersizse null
        private (string UserId, int TokenVersion)? ReadRefreshToken(string refreshToken)
        {
            try
            {
                new JwtSecurityTokenHandler().ValidateToken(refreshToken, new TokenValidationParameters
                {
                    IssuerSigningKey = _signingKey,
                    ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
                    ValidIssuer = Issuer,
                    ValidAudience = RefreshAudience,
                    ClockSkew = TimeSpan.Zero
                }, out var validated);

                var jwt = (JwtSecurityToken)validated;
                var version = jwt.Claims.FirstOrDefault(c => c.Type == TokenVersionClaim)?.Value;

                return int.TryParse(version, out var tokenVersion) && jwt.Subject is { Length: > 0 } userId
                    ? (userId, tokenVersion)
                    : null;
            }
            catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
            {
                return null;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var audience = _configuration["Jwt:Audience"] ?? "EticaretGateway";

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
                // Kısa ömürlü: oturumu refresh token sürdürür, rol değişikliği ve iptal en geç bu süre içinde etkili olur
                Expires = DateTime.UtcNow.AddMinutes(_configuration.GetValue("Jwt:AccessTokenMinutes", 15.0)),
                Issuer = Issuer,
                Audience = audience,
                // RS256: yalnızca Identity'deki özel anahtarla imzalanır; token başlığına kid yazılır (JWKS ile eşleşir)
                SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
