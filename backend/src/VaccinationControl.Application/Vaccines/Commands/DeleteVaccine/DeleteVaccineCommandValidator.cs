using FluentValidation;

namespace VaccinationControl.Application.Vaccines.Commands.DeleteVaccine
{
    public class DeleteVaccineCommandValidator : AbstractValidator<DeleteVaccineCommand>
    {
        public DeleteVaccineCommandValidator()
        {
            // Garante que o Id da vacina não seja nulo ou vazio
            RuleFor(command => command.Id)
                .NotEmpty()
                .WithName("Id da vacina");
        }
    }
}
