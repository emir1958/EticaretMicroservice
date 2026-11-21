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
        private readonly IIdentityService _identityService;
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;

        public AuthController(
            IIdentityService identityService,
            IValidator<RegisterDto> registerValidator,
            IValidator<LoginDto> loginValidator)
        {
            _identityService = identityService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
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

            var token = await _identityService.LoginAsync(loginDto);
            if (token == null)
                return Unauthorized(new { Message = "E-posta veya şifre hatalı." });

            return Ok(new { Token = token });
        }
    }
}