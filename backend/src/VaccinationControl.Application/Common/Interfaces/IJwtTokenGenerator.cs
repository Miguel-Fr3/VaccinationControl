using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Interfaces
{
    /// <summary>
    /// Emite o token de acesso. A Application não conhece JWT nem System.IdentityModel —
    /// só sabe que existe um token com prazo de validade.
    /// </summary>
    public interface IJwtTokenGenerator
    {
        (string Token, DateTime ExpiresAtUtc) Generate(User user);
    }
}
