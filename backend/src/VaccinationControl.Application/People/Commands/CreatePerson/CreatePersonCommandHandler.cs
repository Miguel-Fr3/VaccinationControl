using FluentValidation;
using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.People.Commands.CreatePerson
{
    public class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, PersonResponse>
    {
        private readonly IPersonRepository _personRepository;
        private readonly IValidator<Person> _personValidator;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePersonCommandHandler(
            IPersonRepository personRepository,
            IValidator<Person> personValidator,
            IUnitOfWork unitOfWork)
        {
            _personRepository = personRepository;
            _personValidator = personValidator;
            _unitOfWork = unitOfWork;
        }

        public async Task<PersonResponse> Handle(
            CreatePersonCommand request,
            CancellationToken cancellationToken)
        {
            var person = new Person
            {
                Name = request.Name.Trim(),
                Document = request.Document.Trim()
            };

            // Rede de segurança, antes do conflito: entrada malformada é 400, não 409.
            await _personValidator.ValidateAndThrowAsync(person, cancellationToken);

            // Antecipa o índice único para responder 409 com uma mensagem útil. A corrida
            // entre esta checagem e o commit é coberta pela tradução no SaveChangesAsync.
            if (await _personRepository.ExistsByDocumentAsync(person.Document, cancellationToken))
            {
                throw new ConflictException(
                    $"Já existe uma pessoa cadastrada com o documento '{person.Document}'.");
            }

            _personRepository.Add(person);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new PersonResponse(person.Id, person.Name, person.Document);
        }
    }
}
