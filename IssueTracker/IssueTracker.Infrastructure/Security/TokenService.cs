using IssueTracker.Core.Interfaces;
using IssueTracker.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace IssueTracker.Infrastructure.Security;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    // 1. Iniettiamo IConfiguration per leggere i valori da appsettings.json
    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(User user)
    {
        // 2. CREIAMO I CLAIMS (i dati dell'utente contenuti nel payload)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()) // Gestisce il ruolo (es. Admin, User)
        };

        // 3. RECUPERIAMO LA CHIAVE SEGRETA E LA TRASFORMIAMO IN BYTE
        var secretKey = _config["JwtSettings:SecretKey"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));

        // 4. DEFINIAMO LE CREDENZIALI DI FIRMA (Algoritmo HMAC-SHA256)
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        // 5. IMPOSTIAMO LE CARATTERISTICHE DEL TOKEN (Descriptor)
        var expiryInMinutes = double.Parse(_config["JwtSettings:ExpiryInMinutes"] ?? "60");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
            Issuer = _config["JwtSettings:Issuer"],
            Audience = _config["JwtSettings:Audience"],
            SigningCredentials = creds
        };

        // 6. GENERIAMO E RESTITUIAMO IL TOKEN COME STRINGA
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}