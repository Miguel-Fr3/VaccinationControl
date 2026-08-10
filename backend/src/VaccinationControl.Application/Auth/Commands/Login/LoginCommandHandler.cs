using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Auth.Commands.Login
{
    public class LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginCommand, AuthenticationResponse>
    {
        private const string InvalidCredentials = "E-mail ou senha inválidos.";

        public async Task<AuthenticationResponse> Handle(
            LoginCommand request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            var user = await userRepository.GetByEmailAsync(email, cancellationToken);

            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException(InvalidCredentials);
            }

            var (token, expiresAtUtc) = jwtTokenGenerator.Generate(user);

            return new AuthenticationResponse(user.Id, user.Email, token, expiresAtUtc);
        }
    }
}
