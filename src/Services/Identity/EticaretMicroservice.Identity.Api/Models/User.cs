namespace EticaretMicroservice.Identity.Api.Models
{
    public class User
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // Güvenlik için hash'li tutulacak
        public string Role { get; set; } = "User"; // Varsayılan rol
    }
}
