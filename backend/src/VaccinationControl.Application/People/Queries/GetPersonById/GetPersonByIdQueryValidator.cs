using FluentValidation;

namespace VaccinationControl.Application.People.Queries.GetPersonById
{
    public class GetPersonByIdQueryValidator : AbstractValidator<GetPersonByIdQuery>
    {
        public GetPersonByIdQueryValidator()
        {
            // Garante que o Id da pessoa não seja nulo ou vazio
            RuleFor(query => query.Id)
                .NotEmpty();
        }
    }
}
