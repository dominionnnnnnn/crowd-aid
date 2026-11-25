using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CrowdAid.Data.Entities;
using CrowdAid.Data.IRepo;
using CrowdAid.Data.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CrowdAid.Service.Infrastructure;

/// <summary>HS256 JWT with identity claims only: sub, name, role (spec §5a). No campaign id, no PII.</summary>
public class JwtTokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _s = options.Value;

    public (string Token, int ExpiresIn) CreateAccessToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_s.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_s.Secret));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("name", user.Name),
            new Claim("role", user.Role),
        };

        var jwt = new JwtSecurityToken(
            issuer: _s.Issuer,
            audience: _s.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(jwt), (int)(expires - now).TotalSeconds);
    }
}
