using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Infrastructure.Security
{
    public class JwtTokenGenerator(IOptions<JwtSettings> settings) : IJwtTokenGenerator
    {
        private readonly JwtSettings _settings = settings.Value;

        public (string Token, DateTime ExpiresAtUtc) Generate(User user)
        {
            var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiresInMinutes);

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                SecurityAlgorithms.HmacSha256);

            // O Sub carrega o Id do usuário — é dele que sai o CreatedBy/UpdatedBy gravado
            // nas entidades a cada requisição autenticada.
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
        }
    }
}
