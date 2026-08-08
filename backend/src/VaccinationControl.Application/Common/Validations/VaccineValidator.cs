using FluentValidation;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Validations
{
    public class VaccineValidator : AbstractValidator<Vaccine>
    {
        public VaccineValidator()
        {
            RuleFor(vaccine => vaccine.Id)
                .NotEmpty();

            RuleFor(vaccine => vaccine.Name)
                .NotEmpty()
                .MaximumLength(200);
        }
    }
}
