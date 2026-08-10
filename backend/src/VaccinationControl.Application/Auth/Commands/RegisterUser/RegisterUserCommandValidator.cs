using FluentValidation;

namespace VaccinationControl.Application.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(command => command.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(200);

            RuleFor(command => command.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);
        }
    }
}
