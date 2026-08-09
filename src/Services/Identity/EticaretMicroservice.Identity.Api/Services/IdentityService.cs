using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EticaretMicroservice.Identity.Api.Dtos;
using EticaretMicroservice.Identity.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Identity.Api.Services
{
    public class IdentityService : IIdentityService
    {
        private static readonly List<User> Users = new();
        private readonly IConfiguration _configuration;

        public IdentityService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> RegisterAsync(RegisterDto registerDto)
        {
            if (Users.Any(u => u.Email.Equals(registerDto.Email, StringComparison.OrdinalIgnoreCase)))
                return false;

            string hashedForm = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);
            string assignedRole = registerDto.Email.ToLower().Contains("admin") ? "Admin" : "User";

            var newUser = new User
            {
                Id = Guid.NewGuid().ToString(), // 🔹 Sabit ID Ataması Yapıldı
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = hashedForm,
                Role = assignedRole
            };

            Users.Add(newUser);
            return await Task.FromResult(true);
        }

        public async Task<string?> LoginAsync(LoginDto loginDto)
        {
            var user = Users.FirstOrDefault(u => u.Email.Equals(loginDto.Email, StringComparison.OrdinalIgnoreCase));
            if (user == null) return null;

            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
            if (!isPasswordValid) return null;

            return GenerateJwtToken(user);
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var secretKey = _configuration["JwtSettings:Secret"] ?? "BuCokGizliVeUzunBirAnahtarCumlesidir12345!";
            var key = Encoding.ASCII.GetBytes(secretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id ?? Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}