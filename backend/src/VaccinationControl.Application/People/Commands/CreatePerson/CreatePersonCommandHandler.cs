using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.People.Commands.CreatePerson
{
    public class CreatePersonCommandHandler(
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CreatePersonCommand, PersonResponse>
    {
        public async Task<PersonResponse> Handle(
            CreatePersonCommand request,
            CancellationToken cancellationToken)
        {
            var person = new Person
            {
                Name = request.Name.Trim(),
                Document = request.Document.Trim()
            };

            // Antecipa o índice único para responder 409 com uma mensagem útil. A corrida
            // entre esta checagem e o commit é coberta pela tradução no SaveChangesAsync.
            if (await personRepository.ExistsByDocumentAsync(person.Document, cancellationToken))
            {
                throw new ConflictException(
                    $"Já existe uma pessoa cadastrada com o CPF '{person.Document}'.");
            }

            personRepository.Add(person);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new PersonResponse(person.Id, person.Name, person.Document);
        }
    }
}
