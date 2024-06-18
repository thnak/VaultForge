using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Protector.Certificates.Models;

namespace Protector.Certificates;

/// <summary>
/// Tạo jwt từ file cert
/// </summary>
/// <param name="settings"></param>
public class JsonWebTokenCertificateProvider(IOptions<AppCertificate> settings)
{
    private readonly X509Certificate2 _certificate = new(settings.Value.FilePath, settings.Value.Password);

    public string GenerateJwtToken(string username, int expiresHours = ProtectorTime.JsonWebTokenMaxAge)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new X509SecurityKey(_certificate);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username)
            }),
            Expires = DateTime.UtcNow.AddHours(expiresHours),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateJwtToken(List<Claim> claims, int expiresHours = ProtectorTime.JsonWebTokenMaxAge)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new X509SecurityKey(_certificate);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expiresHours),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? GetClaimsFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new X509SecurityKey(_certificate),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        try
        {
            var jwtSecurityToken = new JwtSecurityToken(token);
            if (jwtSecurityToken.ValidTo > DateTime.Now) return tokenHandler.ValidateToken(token, validationParameters, out _);
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}