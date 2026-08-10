using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard
{
    public class GetVaccinationCardQueryHandler
        : IRequestHandler<GetVaccinationCardQuery, VaccinationCardResponse>
    {
        private readonly IPersonRepository _personRepository;
        private readonly IVaccinationRecordRepository _vaccinationRecordRepository;

        public GetVaccinationCardQueryHandler(
            IPersonRepository personRepository,
            IVaccinationRecordRepository vaccinationRecordRepository)
        {
            _personRepository = personRepository;
            _vaccinationRecordRepository = vaccinationRecordRepository;
        }

        public async Task<VaccinationCardResponse> Handle(
            GetVaccinationCardQuery request,
            CancellationToken cancellationToken)
        {

            var person = await _personRepository.GetByIdAsync(request.PersonId, cancellationToken)
                ?? throw new NotFoundException(nameof(Person), request.PersonId);

            var records = await _vaccinationRecordRepository.GetByPersonAsync(
                person.Id,
                cancellationToken);

            var vaccines = records
                .GroupBy(record => new { record.VaccineId, VaccineName = record.Vaccine.Name })
                .Select(group => new VaccinationCardVaccineResponse(
                    group.Key.VaccineId,
                    group.Key.VaccineName,
                    group.Count(),
                    [.. group
                        .Select(record => new VaccinationCardDoseResponse(
                            record.Id,
                            record.VaccinationType,
                            record.DoseNumber,
                            record.VaccinationDate))]))
                .ToList();

            return new VaccinationCardResponse(
                person.Id,
                person.Name,
                person.Document,
                vaccines);
        }
    }
}
