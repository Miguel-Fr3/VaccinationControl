using MediatR;

namespace VaccinationControl.Application.Auth.Queries.GetCurrentUser
{
    /// <summary>
    /// Não tem parâmetros: a identidade vem do <c>ICurrentUser</c>, e não da requisição. Aceitar
    /// um Id aqui deixaria qualquer sessão consultar qualquer usuário.
    /// </summary>
    public record GetCurrentUserQuery : IRequest<SessionResponse>;
}
