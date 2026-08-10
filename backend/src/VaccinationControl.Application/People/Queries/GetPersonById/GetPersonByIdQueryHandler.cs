using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.People.Queries.GetPersonById
{
    public class GetPersonByIdQueryHandler(IPersonRepository personRepository) : IRequestHandler<GetPersonByIdQuery, PersonResponse>
    {
        public async Task<PersonResponse> Handle(
            GetPersonByIdQuery request,
            CancellationToken cancellationToken)
        {
            var person = await personRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Person), request.Id);

            return new PersonResponse(person.Id, person.Name, person.Document);
        }
    }
}
