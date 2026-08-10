using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccinations.Commands.DeleteVaccinationRecord
{
    public class DeleteVaccinationRecordCommandHandler
        : IRequestHandler<DeleteVaccinationRecordCommand>
    {
        private readonly IVaccinationRecordRepository _vaccinationRecordRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteVaccinationRecordCommandHandler(
            IVaccinationRecordRepository vaccinationRecordRepository,
            IUnitOfWork unitOfWork)
        {
            _vaccinationRecordRepository = vaccinationRecordRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(
            DeleteVaccinationRecordCommand request,
            CancellationToken cancellationToken)
        {
            // GetByIdAsync já exige que o registro pertença à pessoa da rota.
            var record = await _vaccinationRecordRepository.GetByIdAsync(
                request.PersonId,
                request.RecordId,
                cancellationToken)
                ?? throw new NotFoundException(nameof(VaccinationRecord), request.RecordId);

            // A remoção é livre: qualquer dose pode sair, inclusive do meio da sequência.
            // As regras de registro continuam permitindo recriá-la depois, porque exigem
            // apenas a dose anterior — nenhum estado alcançado por remoção fica sem volta.
            _vaccinationRecordRepository.Remove(record);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
