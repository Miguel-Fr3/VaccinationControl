using FluentValidation;

namespace VaccinationControl.Application.Vaccines.Queries.GetVaccineById
{
    public class GetVaccineByIdQueryValidator : AbstractValidator<GetVaccineByIdQuery>
    {
        public GetVaccineByIdQueryValidator()
        {
            // A rota já garante o formato Guid; aqui barramos o Guid vazio.
            RuleFor(query => query.Id)
                .NotEmpty()
                .WithName("Id da vacina");
        }
    }
}
