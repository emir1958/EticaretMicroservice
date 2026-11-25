using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EticaretMicroservice.Shared.Extensions;

// RSA anahtarlarını yapılandırmadan okur. Yapılandırma değeri, PEM dosyasının base64'üdür (çok satırlı PEM ortam değişkenine
// sığmadığı için): `openssl genrsa 2048 | base64 | tr -d '\n'`
public static class JwtKeys
{
    // Özel anahtar yalnızca Identity'dedir: token imzalamak için kullanılır
    public static RsaSecurityKey LoadPrivateKey(string base64Pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(DecodePem(base64Pem));
        return WithKeyId(new RsaSecurityKey(rsa));
    }

    // Diğer servisler yalnızca genel anahtarı bilir: token imzalayamaz, sadece doğrulayabilir
    public static RsaSecurityKey LoadPublicKey(string base64Pem)
    {
        var rsa = RSA.Create();
        rsa.ImportFromPem(DecodePem(base64Pem));
        return WithKeyId(new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)));
    }

    // Genel anahtardan türetilen, özel anahtarı içermeyen kopya (Identity kendi token'larını doğrularken kullanır)
    public static RsaSecurityKey ToPublicKey(RsaSecurityKey key) =>
        WithKeyId(new RsaSecurityKey(key.Rsa is { } rsa ? rsa.ExportParameters(false) : key.Parameters));

    // kid: genel anahtarın parmak izi. Aynı anahtar çifti her yerde aynı kid'i üretir; token başlığındaki kid ile JWKS eşleşir.
    private static RsaSecurityKey WithKeyId(RsaSecurityKey key)
    {
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(key);
        key.KeyId = Base64UrlEncoder.Encode(jwk.ComputeJwkThumbprint());
        return key;
    }

    private static string DecodePem(string base64Pem) => Encoding.UTF8.GetString(Convert.FromBase64String(base64Pem));
}
