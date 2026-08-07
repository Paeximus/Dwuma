using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Dwuma.Models.Data.DwumaContext;
using Microsoft.IdentityModel.Tokens;

namespace Dwuma.Services;

public sealed class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public GeneratedToken CreateToken(User user)
    {
        string key =
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        string issuer =
            _configuration["Jwt:Issuer"]
            ?? "DwumaApi";

        string audience =
            _configuration["Jwt:Audience"]
            ?? "DwumaFrontend";

        int expiryMinutes =
            _configuration.GetValue(
                "Jwt:ExpiryMinutes",
                10080);

        DateTime expiresAt =
            DateTime.UtcNow.AddMinutes(
                expiryMinutes);

        Claim[] claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.FullName),

            new Claim(
                ClaimTypes.Email,
                user.Email),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim(
                JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        ];

        var securityKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: expiresAt,
                signingCredentials: credentials);

        string tokenValue =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new GeneratedToken
        {
            Value = tokenValue,
            ExpiresAt = expiresAt
        };
    }
}

public sealed class GeneratedToken
{
    public string Value { get; set; } =
        string.Empty;

    public DateTime ExpiresAt { get; set; }
}