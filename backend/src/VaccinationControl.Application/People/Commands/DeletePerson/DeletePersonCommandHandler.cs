using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.People.Commands.DeletePerson
{
    public class DeletePersonCommandHandler(
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<DeletePersonCommand>
    {
        public async Task Handle(DeletePersonCommand request, CancellationToken cancellationToken)
        {
            var person = await personRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Person), request.Id);

            // Os registros de vacinação são apagados pelo ON DELETE CASCADE da FK;
            personRepository.Remove(person);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
