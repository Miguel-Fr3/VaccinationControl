using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccines.Commands.CreateVaccine
{
    public class CreateVaccineCommandHandler(
        IVaccineRepository vaccineRepository,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateVaccineCommand, VaccineResponse>
    {
        public async Task<VaccineResponse> Handle(
            CreateVaccineCommand request,
            CancellationToken cancellationToken)
        {
            var vaccine = new Vaccine { Name = request.Name.Trim() };

            // Antecipa o índice único para responder 409 com uma mensagem útil. A corrida
            // entre esta checagem e o commit é coberta pela tradução no SaveChangesAsync.
            if (await vaccineRepository.ExistsByNameAsync(vaccine.Name, cancellationToken))
            {
                throw new ConflictException(
                    $"Já existe uma vacina cadastrada com o nome '{vaccine.Name}'.");
            }

            vaccineRepository.Add(vaccine);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new VaccineResponse(vaccine.Id, vaccine.Name);
        }
    }
}
