using EticaretMicroservice.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Identity.Api.Controllers;

// Diğer servislerin token'ı doğrulamak için ihtiyaç duyduğu genel anahtarı yayınlar (OpenID Connect discovery + JWKS).
// Gateway'de bu yollar için route yoktur: yalnızca Docker ağı içindeki servisler erişir, internete açılmaz.
[ApiController]
[Route(".well-known")]
public class WellKnownController : ControllerBase
{
    private readonly RsaSecurityKey _signingKey;
    private readonly IConfiguration _configuration;

    public WellKnownController(RsaSecurityKey signingKey, IConfiguration configuration)
    {
        _signingKey = signingKey;
        _configuration = configuration;
    }

    // JwtBearer, MetadataAddress verildiğinde önce bu belgeyi okur; yalnızca issuer ve jwks_uri gerekir
    [HttpGet("openid-configuration")]
    public IActionResult Discovery() => Ok(new
    {
        issuer = _configuration["Jwt:Issuer"] ?? "EticaretIdentityServer",
        jwks_uri = $"{Request.Scheme}://{Request.Host}/.well-known/jwks.json"
    });

    [HttpGet("jwks.json")]
    public IActionResult Jwks()
    {
        // Özel anahtar parametreleri asla yazılmaz: yalnızca genel kısım (n, e) yayınlanır
        var publicJwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(JwtKeys.ToPublicKey(_signingKey));

        return Ok(new
        {
            keys = new[]
            {
                new { kty = publicJwk.Kty, use = "sig", alg = SecurityAlgorithms.RsaSha256, kid = _signingKey.KeyId, n = publicJwk.N, e = publicJwk.E }
            }
        });
    }
}
