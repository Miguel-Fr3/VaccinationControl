using FluentValidation;

namespace VaccinationControl.Application.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .WithName("E-mail");

            RuleFor(command => command.Password)
                .NotEmpty()
                .WithName("Senha");
        }
    }
}
