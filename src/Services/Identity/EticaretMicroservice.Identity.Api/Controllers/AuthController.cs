using EticaretMicroservice.Identity.Api.Dtos;
using EticaretMicroservice.Identity.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EticaretMicroservice.Identity.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            var result = await _identityService.RegisterAsync(registerDto);
            if (!result)
                return BadRequest(new { Message = "Bu e-posta adresi zaten kullanımda." });

            return Ok(new { Message = "Kullanıcı başarıyla kaydedildi." });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var token = await _identityService.LoginAsync(loginDto);
            if (token == null)
                return Unauthorized(new { Message = "E-posta veya şifre hatalı." });

            return Ok(new { Token = token });
        }
    }
}