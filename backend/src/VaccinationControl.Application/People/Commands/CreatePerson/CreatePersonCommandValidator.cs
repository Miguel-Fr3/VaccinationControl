using FluentValidation;

namespace VaccinationControl.Application.People.Commands.CreatePerson
{
    public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
    {
        public CreatePersonCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200);

            // A unicidade do documento depende do banco e é verificada no handler.
            RuleFor(command => command.Document)
                .NotEmpty()
                .MaximumLength(50);
        }
    }
}
