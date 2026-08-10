using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using VaccinationControl.Application.Common.Interfaces;

namespace VaccinationControl.Api.Security
{
    /// <summary>
    /// Lê a identidade do token da requisição corrente. Fica na Api porque é aqui que
    /// existe HttpContext — a Application só conhece a interface.
    /// </summary>
    public class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
    {
        public Guid? Id
        {
            get
            {
                var subject = httpContextAccessor.HttpContext?.User
                    .FindFirstValue(JwtRegisteredClaimNames.Sub);

                return Guid.TryParse(subject, out var userId) ? userId : null;
            }
        }
    }
}
