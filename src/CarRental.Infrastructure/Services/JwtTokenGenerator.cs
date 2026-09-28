using CarRental.Application.Abstractions.Services;
using CarRental.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarRental.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        string secret = _configuration["JwtSettings:Secret"] ?? "dev-secret-key-minimum-32-characters-long!!";
        string issuer = _configuration["JwtSettings:Issuer"] ?? "CarRentalApi";
        string audience = _configuration["JwtSettings:Audience"] ?? "CarRentalClient";
        int minutes = int.TryParse(_configuration["JwtSettings:ExpirationMinutes"], out int result) ? result : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
        };

        foreach (Role role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Name.ToString()));
            claims.Add(new Claim("role", role.Name.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}