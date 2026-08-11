using FluentValidation;

namespace VaccinationControl.Application.People.Commands.CreatePerson
{
    public class CreatePersonCommandValidator : AbstractValidator<CreatePersonCommand>
    {
        public CreatePersonCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200)
                .WithName("Nome");

            // A unicidade do documento depende do banco e é verificada no handler.
            RuleFor(command => command.Document)
                .NotEmpty()
                .MinimumLength(11)
                .MaximumLength(11)
                .WithName("Documento");
        }
    }
}
