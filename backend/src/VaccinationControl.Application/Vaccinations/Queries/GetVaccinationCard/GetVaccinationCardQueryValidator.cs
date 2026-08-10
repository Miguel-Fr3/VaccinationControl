using FluentValidation;

namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard
{
    public class GetVaccinationCardQueryValidator : AbstractValidator<GetVaccinationCardQuery>
    {
        public GetVaccinationCardQueryValidator()
        {
            // Garante que o Id da pessoa não seja nulo ou vazio
            RuleFor(query => query.PersonId)
                .NotEmpty();
        }
    }
}
