using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccines.Queries.GetVaccineById
{
    public class GetVaccineByIdQueryHandler : IRequestHandler<GetVaccineByIdQuery, VaccineResponse>
    {
        private readonly IVaccineRepository _vaccineRepository;

        public GetVaccineByIdQueryHandler(IVaccineRepository vaccineRepository)
        {
            _vaccineRepository = vaccineRepository;
        }

        public async Task<VaccineResponse> Handle(
            GetVaccineByIdQuery request,
            CancellationToken cancellationToken)
        {
            var vaccine = await _vaccineRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException(nameof(Vaccine), request.Id);

            return new VaccineResponse(vaccine.Id, vaccine.Name);
        }
    }
}
