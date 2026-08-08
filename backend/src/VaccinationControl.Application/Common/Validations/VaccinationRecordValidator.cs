using FluentValidation;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Validations
{
    public class VaccinationRecordValidator : AbstractValidator<VaccinationRecord>
    {
        public VaccinationRecordValidator()
        {
            RuleFor(record => record.PersonId)
                .NotEmpty();

            RuleFor(record => record.VaccineId)
                .NotEmpty();

            RuleFor(record => record.VaccinationType)
                .IsInEnum();

            // Dose precisa ser positiva.
            RuleFor(record => record.DoseNumber)
                .GreaterThan(0);

            // Não é possível registrar uma aplicação futura.
            RuleFor(record => record.VaccinationDate)
                .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow));
        }
    }
}
