using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccines.Commands.DeleteVaccine
{
    public class DeleteVaccineCommandHandler(
        IVaccineRepository vaccineRepository,
        IVaccinationRecordRepository vaccinationRecordRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<DeleteVaccineCommand>
    {
        public async Task Handle(DeleteVaccineCommand request, CancellationToken cancellationToken)
        {
            var vaccine = await vaccineRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Vaccine), request.Id);

            // Antecipa o Restrict da FK para responder 409 com uma mensagem que diz o motivo.
            // A corrida entre esta checagem e o commit é coberta pela tradução no
            // SaveChangesAsync, que transforma a violação da FK no mesmo 409.
            if (await vaccinationRecordRepository.ExistsByVaccineAsync(vaccine.Id, cancellationToken))
            {
                throw new ConflictException(
                    $"A vacina '{vaccine.Name}' tem doses registradas e não pode ser removida.");
            }

            vaccineRepository.Remove(vaccine);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
