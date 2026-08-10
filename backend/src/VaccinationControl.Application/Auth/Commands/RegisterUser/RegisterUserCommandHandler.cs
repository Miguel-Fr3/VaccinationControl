using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IUnitOfWork unitOfWork)
        : IRequestHandler<RegisterUserCommand, AuthenticationResponse>
    {
        public async Task<AuthenticationResponse> Handle(
            RegisterUserCommand request,
            CancellationToken cancellationToken)
        {
            var email = request.Email.Trim().ToLowerInvariant();

            if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            {
                throw new ConflictException($"Já existe um usuário cadastrado com o e-mail '{email}'.");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(request.Password)
            };

            userRepository.Add(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var (token, expiresAtUtc) = jwtTokenGenerator.Generate(user);

            return new AuthenticationResponse(user.Id, user.Email, token, expiresAtUtc);
        }
    }
}
