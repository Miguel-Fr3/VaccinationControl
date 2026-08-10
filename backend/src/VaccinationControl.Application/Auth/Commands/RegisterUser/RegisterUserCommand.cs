using MediatR;

namespace VaccinationControl.Application.Auth.Commands.RegisterUser
{
    public record RegisterUserCommand(string Email, string Password) : IRequest<AuthenticationResponse>;
}
