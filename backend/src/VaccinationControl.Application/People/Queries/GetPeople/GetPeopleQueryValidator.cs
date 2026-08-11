using FluentValidation;

namespace VaccinationControl.Application.People.Queries.GetPeople
{
    public class GetPeopleQueryValidator : AbstractValidator<GetPeopleQuery>
    {
        public GetPeopleQueryValidator()
        {
            RuleFor(query => query.Search)
                .MaximumLength(200)
                .WithName("Busca");

            RuleFor(query => query.Page)
                .GreaterThan(0)
                .When(query => query.Page.HasValue)
                .WithName("Página");

            // Teto para impedir que um pageSize enorme anule o efeito da paginação.
            RuleFor(query => query.PageSize)
                .InclusiveBetween(1, 100)
                .When(query => query.PageSize.HasValue)
                .WithName("Tamanho da página");
        }
    }
}
