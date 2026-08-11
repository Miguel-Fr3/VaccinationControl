using FluentValidation;

namespace VaccinationControl.Application.Vaccinations.Commands.DeleteVaccinationRecord
{
    public class DeleteVaccinationRecordCommandValidator
        : AbstractValidator<DeleteVaccinationRecordCommand>
    {
        public DeleteVaccinationRecordCommandValidator()
        {
            // Garante que o Id da pessoa não seja nulo ou vazio
            RuleFor(command => command.PersonId)
                .NotEmpty()
                .WithName("Id da pessoa");

            RuleFor(command => command.RecordId)
                .NotEmpty()
                .WithName("Id do registro");
        }
    }
}
