using EticaretMicroservice.Identity.Api.Dtos;
using EticaretMicroservice.Identity.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace EticaretMicroservice.Identity.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private const string RefreshCookieName = "refresh_token";

        private readonly IIdentityService _identityService;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IConfiguration _configuration;

        public AuthController(
            IIdentityService identityService,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator,
            IConfiguration configuration)
        {
            _identityService = identityService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Hata ValidationException fırlatır; ortak exception middleware'i alan bazlı 400'e çevirir
            await _registerValidator.ValidateAndThrowAsync(registerDto);

            var result = await _identityService.RegisterAsync(registerDto);
            if (!result)
                return BadRequest(new { Message = "Bu e-posta adresi zaten kullanımda." });

            return Ok(new { Message = "Kullanıcı başarıyla kaydedildi." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            await _loginValidator.ValidateAndThrowAsync(loginDto);

            var tokens = await _identityService.LoginAsync(loginDto);
            if (tokens == null)
                return Unauthorized(new { Message = "E-posta veya şifre hatalı." });

            SetRefreshCookie(tokens);
            return Ok(new { Token = tokens.AccessToken });
        }

        // Refresh token yalnızca HttpOnly cookie'den okunur (JavaScript erişemez); gövde taşımaz.
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            var tokens = string.IsNullOrEmpty(refreshToken) ? null : await _identityService.RefreshAsync(refreshToken);

            if (tokens == null)
            {
                ClearRefreshCookie();
                return Unauthorized(new { Message = "Oturum süresi doldu. Lütfen tekrar giriş yapın." });
            }

            SetRefreshCookie(tokens);
            return Ok(new { Token = tokens.AccessToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies[RefreshCookieName];
            if (!string.IsNullOrEmpty(refreshToken))
                await _identityService.RevokeAsync(refreshToken);

            ClearRefreshCookie();
            return NoContent();
        }

        // Path yalnızca /api/auth: tarayıcı cookie'yi başka hiçbir isteğe eklemez.
        // SameSite=Strict + CORS'un yalnızca izinli origin'lere credentials vermesi, cookie'ye dayanan bu uçları CSRF'e karşı korur.
        private CookieOptions CookieOptions(DateTime? expiresAt) => new()
        {
            HttpOnly = true,
            // Varsayılan güvenli; yerel geliştirmede (HTTP) docker-compose üzerinden kapatılır
            Secure = _configuration.GetValue("Auth:RefreshCookieSecure", true),
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = expiresAt,
            IsEssential = true
        };

        private void SetRefreshCookie(TokenPair tokens) =>
            Response.Cookies.Append(RefreshCookieName, tokens.RefreshToken, CookieOptions(tokens.RefreshTokenExpiresAt));

        private void ClearRefreshCookie() => Response.Cookies.Delete(RefreshCookieName, CookieOptions(null));
    }
}
