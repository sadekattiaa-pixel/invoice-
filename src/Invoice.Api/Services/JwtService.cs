using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Invoice.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Invoice.Api.Services;

/// <summary>
/// تنفيذ خدمة JWT — HMAC-SHA256 مع المفتاح السري من الإعدادات.
/// </summary>
public class JwtService : IJwtService
{
    private const string TokenTypeClaim = "token_type";
    private const string PermissionClaim = "permission";

    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;

        if (string.IsNullOrWhiteSpace(_settings.Key) || _settings.Key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key must be at least 32 characters. Check appsettings.json.");
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TokenTypeClaim, "access"),
            new("full_name", user.FullName)
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim(PermissionClaim, permission));

        return BuildToken(claims, DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes));
    }

    public string GenerateRefreshToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(TokenTypeClaim, "refresh")
        };

        return BuildToken(claims, DateTime.UtcNow.AddDays(_settings.RefreshTokenDays));
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));

        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }

    private string BuildToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}