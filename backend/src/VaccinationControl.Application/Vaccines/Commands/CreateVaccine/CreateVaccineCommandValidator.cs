using FluentValidation;

namespace VaccinationControl.Application.Vaccines.Commands.CreateVaccine
{
    public class CreateVaccineCommandValidator : AbstractValidator<CreateVaccineCommand>
    {
        public CreateVaccineCommandValidator()
        {
            RuleFor(command => command.Name)
                .NotEmpty()
                .MaximumLength(200);
        }
    }
}
