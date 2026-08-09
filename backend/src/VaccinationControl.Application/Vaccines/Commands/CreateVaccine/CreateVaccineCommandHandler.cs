using FluentValidation;
using MediatR;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccines.Commands.CreateVaccine
{
    public class CreateVaccineCommandHandler : IRequestHandler<CreateVaccineCommand, VaccineResponse>
    {
        private readonly IVaccineRepository _vaccineRepository;
        private readonly IValidator<Vaccine> _vaccineValidator;
        private readonly IUnitOfWork _unitOfWork;

        public CreateVaccineCommandHandler(
            IVaccineRepository vaccineRepository,
            IValidator<Vaccine> vaccineValidator,
            IUnitOfWork unitOfWork)
        {
            _vaccineRepository = vaccineRepository;
            _vaccineValidator = vaccineValidator;
            _unitOfWork = unitOfWork;
        }

        public async Task<VaccineResponse> Handle(
            CreateVaccineCommand request,
            CancellationToken cancellationToken)
        {
            var vaccine = new Vaccine { Name = request.Name.Trim() };

            // Rede de segurança, antes do conflito: entrada malformada é 400, não 409.
            await _vaccineValidator.ValidateAndThrowAsync(vaccine, cancellationToken);

            // Antecipa o índice único para responder 409 com uma mensagem útil. A corrida
            // entre esta checagem e o commit é coberta pela tradução no SaveChangesAsync.
            if (await _vaccineRepository.ExistsByNameAsync(vaccine.Name, cancellationToken))
            {
                throw new ConflictException(
                    $"Já existe uma vacina cadastrada com o nome '{vaccine.Name}'.");
            }

            _vaccineRepository.Add(vaccine);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new VaccineResponse(vaccine.Id, vaccine.Name);
        }
    }
}
