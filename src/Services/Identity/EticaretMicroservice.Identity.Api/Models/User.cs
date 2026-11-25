namespace EticaretMicroservice.Identity.Api.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";

        // Refresh token'lar bu sürümü taşır; çıkışta artırılınca kullanıcının tüm refresh token'ları geçersiz olur
        public int TokenVersion { get; set; }
    }
}
