using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WidgetPlatform.Repositories;

namespace WidgetPlatform.Services;

public sealed class AuthService
{
    private readonly ITenantRepository _tenants;
    private readonly IConfiguration _config;

    public AuthService(ITenantRepository tenants, IConfiguration config)
    {
        _tenants = tenants;
        _config = config;
    }

    public async Task<string?> RegisterAsync(string email, string password, CancellationToken ct)
    {
        if (await _tenants.GetByEmailAsync(email, ct) is not null)
            return null;                               

        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        var tenant = await _tenants.AddAsync(email, hash, ct);
        return CreateToken(tenant.Id, tenant.Email);
    }

    public async Task<string?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var tenant = await _tenants.GetByEmailAsync(email, ct);
        if (tenant is null)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, tenant.PasswordHash))
            return null;                               

        return CreateToken(tenant.Id, tenant.Email);
    }

    private string CreateToken(Guid tenantId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("tenantId", tenantId.ToString()),
            new Claim("email", email)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Issuer"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}