using System.Security.Cryptography;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AppNotify.Services;

public sealed class JwtGenerator
{
    private readonly string _issuerID;
    private readonly string _keyID;
    private readonly string _privateKeyPem;
    private string? _cachedToken;
    private DateTimeOffset? _tokenExpiry;

    public JwtGenerator(string issuerID, string keyID, string privateKey)
    {
        _issuerID = issuerID;
        _keyID = keyID;
        _privateKeyPem = privateKey;
    }

    public string GenerateToken()
    {
        if (_cachedToken is not null && _tokenExpiry is not null &&
            DateTimeOffset.UtcNow < _tokenExpiry.Value.AddSeconds(-60))
        {
            Debug.WriteLine($"[JWT] Using cached token (expires {_tokenExpiry.Value})");
            return _cachedToken;
        }

        Debug.WriteLine($"[JWT] Generating new token for issuer {_issuerID}, key {_keyID}");

        var now = DateTimeOffset.UtcNow;
        var expiry = now.AddMinutes(20);

        var header = JsonSerializer.Serialize(new { alg = "ES256", kid = _keyID, typ = "JWT" });
        var payload = JsonSerializer.Serialize(new
        {
            iss = _issuerID,
            iat = now.ToUnixTimeSeconds(),
            exp = expiry.ToUnixTimeSeconds(),
            aud = "appstoreconnect-v1"
        });

        var headerB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(header));
        var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
        var signingInput = $"{headerB64}.{payloadB64}";

        using var ecdsa = ECDsa.Create();
        var keyBytes = ParseP8PrivateKey(_privateKeyPem);
        ecdsa.ImportPkcs8PrivateKey(keyBytes, out _);

        var signature = ecdsa.SignData(
            Encoding.UTF8.GetBytes(signingInput),
            HashAlgorithmName.SHA256);

        var signatureB64 = Base64UrlEncode(signature);
        var token = $"{signingInput}.{signatureB64}";

        _cachedToken = token;
        _tokenExpiry = expiry;
        Debug.WriteLine("[JWT] Token generated successfully");
        return token;
    }

    private static byte[] ParseP8PrivateKey(string pem)
    {
        var stripped = pem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        return Convert.FromBase64String(stripped);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
