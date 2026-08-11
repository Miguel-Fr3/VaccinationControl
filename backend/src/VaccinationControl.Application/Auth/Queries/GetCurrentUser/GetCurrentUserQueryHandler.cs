using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Auth.Queries.GetCurrentUser
{
    public class GetCurrentUserQueryHandler(
        ICurrentUser currentUser,
        IUserRepository userRepository) : IRequestHandler<GetCurrentUserQuery, SessionResponse>
    {
        private const string SessionNotFound = "Sessão inválida ou expirada.";

        public async Task<SessionResponse> Handle(
            GetCurrentUserQuery request,
            CancellationToken cancellationToken)
        {
            var userId = currentUser.Id ?? throw new UnauthorizedException(SessionNotFound);

            // O token continua válido até expirar, mesmo que o usuário deixe de existir. Sem
            // esta consulta, a interface exibiria uma sessão que o banco já não reconhece.
            var user = await userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new UnauthorizedException(SessionNotFound);

            return new SessionResponse(user.Id, user.Email);
        }
    }
}
