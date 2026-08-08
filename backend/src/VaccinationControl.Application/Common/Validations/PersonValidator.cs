using FluentValidation;
using VaccinationControl.Domain.Entities;

namespace VaccinationControl.Application.Common.Validations
{
    public class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(person => person.Id)
                .NotEmpty();

            RuleFor(person => person.Name)
                .NotEmpty()
                .MaximumLength(200);
        }
    }
}
