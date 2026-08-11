using FluentValidation;

namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationRecordById
{
    public class GetVaccinationRecordByIdQueryValidator
        : AbstractValidator<GetVaccinationRecordByIdQuery>
    {
        public GetVaccinationRecordByIdQueryValidator()
        {
            // Garante que o Id da pessoa não seja nulo ou vazio
            RuleFor(query => query.PersonId)
                .NotEmpty()
                .WithName("Id da pessoa");

            RuleFor(query => query.RecordId)
                .NotEmpty()
                .WithName("Id do registro");
        }
    }
}
