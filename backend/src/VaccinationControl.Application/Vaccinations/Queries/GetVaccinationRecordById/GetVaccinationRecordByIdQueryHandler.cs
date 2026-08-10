using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationRecordById
{
    public class GetVaccinationRecordByIdQueryHandler(IVaccinationRecordRepository vaccinationRecordRepository)
        : IRequestHandler<GetVaccinationRecordByIdQuery, VaccinationRecordResponse>
    {
        public async Task<VaccinationRecordResponse> Handle(
            GetVaccinationRecordByIdQuery request,
            CancellationToken cancellationToken)
        {
            var record = await vaccinationRecordRepository.GetByIdAsync(
                request.PersonId,
                request.RecordId,
                cancellationToken)
                ?? throw new NotFoundException(nameof(VaccinationRecord), request.RecordId);

            return new VaccinationRecordResponse(
                record.Id,
                record.PersonId,
                record.VaccineId,
                record.Vaccine.Name,
                record.VaccinationType,
                record.DoseNumber,
                record.VaccinationDate);
        }
    }
}
