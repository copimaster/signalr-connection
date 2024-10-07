using Microsoft.IdentityModel.Tokens;
using SignalR.Extensions;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace SignalR.Services
{
    public interface IAuthService
    {
        string GetOrCreateUniqueIdentifier(string username);
        string GenerateJwtToken(string username);
    }

    public class AuthService : IAuthService
    {
        private static readonly ConcurrentDictionary<string, string> UserIdentifiers = new();
        private readonly JwtSettings _jwtSettings;

        public AuthService(JwtSettings jwtSettings)
        {
            _jwtSettings = jwtSettings;
        }

        public string GetOrCreateUniqueIdentifier(string username)
        {
            return UserIdentifiers.GetOrAdd(username, _ => GenerateUniqueIdentifier(username));
        }

        private string GenerateUniqueIdentifier(string username)
        {
            var hashedUsername = SHA256.HashData(Encoding.UTF8.GetBytes(username));
            var guid = Guid.NewGuid();
            var combinedBytes = hashedUsername.Concat(guid.ToByteArray()).ToArray();
            return Convert.ToBase64String(combinedBytes);
        }

        public string GenerateJwtToken(string username)
        {
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, GetOrCreateUniqueIdentifier(username)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, "123_44"), // El Context.UserIdentifier se asigna automáticamente a partir del ClaimTypes.NameIdentifier del usuario autenticado.
                new Claim(ClaimTypes.Name, username)
                //new Claim("username", username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtSettings.ExpirationMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
