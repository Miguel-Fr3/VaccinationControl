using MediatR;

namespace VaccinationControl.Application.People.Queries.GetPersonById
{
    public record GetPersonByIdQuery(Guid Id) : IRequest<PersonResponse>;
}
