using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Auth.Commands.Login
{
    public class LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginCommand, AuthenticationResult>
    {
        private const string InvalidCredentials = "E-mail ou senha inválidos.";

        public async Task<AuthenticationResult> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await userRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null)
            {
                // Paga o custo do hash mesmo quando o usuário não existe, para não dar pista de que o e-mail não está cadastrado.
                _ = passwordHasher.Hash(request.Password);

                throw new UnauthorizedException(InvalidCredentials);
            }

            if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException(InvalidCredentials);
            }

            var (token, expiresAtUtc) = jwtTokenGenerator.Generate(user);

            return new AuthenticationResult(user.Id, user.Email, token, expiresAtUtc);
        }
    }
}
