using FluentValidation;
using MediatR;
using VaccinationControl.Application.Common.Extensions;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Enums;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Vaccinations.Commands.RegisterVaccination
{
    public class RegisterVaccinationCommandHandler
        : IRequestHandler<RegisterVaccinationCommand, VaccinationRecordResponse>
    {
        private readonly IPersonRepository _personRepository;
        private readonly IVaccineRepository _vaccineRepository;
        private readonly IVaccinationRecordRepository _vaccinationRecordRepository;
        private readonly IValidator<VaccinationRecord> _vaccinationRecordValidator;
        private readonly IUnitOfWork _unitOfWork;

        public RegisterVaccinationCommandHandler(
            IPersonRepository personRepository,
            IVaccineRepository vaccineRepository,
            IVaccinationRecordRepository vaccinationRecordRepository,
            IValidator<VaccinationRecord> vaccinationRecordValidator,
            IUnitOfWork unitOfWork)
        {
            _personRepository = personRepository;
            _vaccineRepository = vaccineRepository;
            _vaccinationRecordRepository = vaccinationRecordRepository;
            _vaccinationRecordValidator = vaccinationRecordValidator;
            _unitOfWork = unitOfWork;
        }

        public async Task<VaccinationRecordResponse> Handle(
            RegisterVaccinationCommand request,
            CancellationToken cancellationToken)
        {
            var person = await _personRepository.GetByIdAsync(request.PersonId, cancellationToken)
                ?? throw new NotFoundException(nameof(Person), request.PersonId);

            var vaccine = await _vaccineRepository.GetByIdAsync(request.VaccineId, cancellationToken)
                ?? throw new NotFoundException(nameof(Vaccine), request.VaccineId);

            var vaccinationRecord = new VaccinationRecord
            {
                PersonId = person.Id,
                VaccineId = vaccine.Id,
                VaccinationType = request.VaccinationType,
                DoseNumber = request.DoseNumber,
                VaccinationDate = request.VaccinationDate
            };

            // Rede de segurança, antes dos conflitos: entrada malformada é 400, não 409.
            await _vaccinationRecordValidator.ValidateAndThrowAsync(vaccinationRecord, cancellationToken);

            var registeredDoses = await _vaccinationRecordRepository.GetDosesAsync(
                person.Id,
                vaccine.Id,
                cancellationToken);

            // Doses normais e reforços têm numeração independente: as regras de duplicidade,
            // sequência e ordem cronológica olham apenas o mesmo tipo do que está sendo
            // registrado. Ter a dose normal 1 não habilita o reforço 2.
            var sameTypeDoses = registeredDoses
                .Where(dose => dose.VaccinationType == request.VaccinationType)
                .ToList();

            EnsureDoseIsNotDuplicated(sameTypeDoses, request, vaccine);
            EnsureDoseFollowsSequence(sameTypeDoses, request);
            EnsureDoseIsNotBackdated(sameTypeDoses, request);

            // Esta é a única que cruza os tipos: reforço pressupõe dose normal.
            EnsureBoosterFollowsInitialDose(registeredDoses, request, vaccine);

            _vaccinationRecordRepository.Add(vaccinationRecord);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new VaccinationRecordResponse(
                vaccinationRecord.Id,
                person.Id,
                vaccine.Id,
                vaccine.Name,
                vaccinationRecord.VaccinationType,
                vaccinationRecord.DoseNumber,
                vaccinationRecord.VaccinationDate);
        }

        /// <summary>
        /// A mesma dose do mesmo tipo não pode ser registrada duas vezes para a pessoa.
        /// O índice único do banco também barra, mas ali o erro perderia a mensagem útil.
        /// </summary>
        private static void EnsureDoseIsNotDuplicated(
            IReadOnlyList<VaccinationRecord> sameTypeDoses,
            RegisterVaccinationCommand request,
            Vaccine vaccine)
        {
            if (sameTypeDoses.Any(dose => dose.DoseNumber == request.DoseNumber))
            {
                throw new ConflictException(
                    $"A {request.VaccinationType.Describe()} {request.DoseNumber} da vacina "
                    + $"'{vaccine.Name}' já foi registrada para esta pessoa.");
            }
        }

        /// <summary>
        /// Dentro de um tipo, as doses são sequenciais: a de número N só pode ser aplicada
        /// se a N-1 do mesmo tipo já existir.
        /// </summary>
        private static void EnsureDoseFollowsSequence(
            IReadOnlyList<VaccinationRecord> sameTypeDoses,
            RegisterVaccinationCommand request)
        {
            var previousDoseNumber = request.DoseNumber - 1;

            if (request.DoseNumber > 1
                && sameTypeDoses.All(dose => dose.DoseNumber != previousDoseNumber))
            {
                var description = request.VaccinationType.Describe();

                throw new ConflictException(
                    $"A {description} {previousDoseNumber} precisa ser registrada antes da "
                    + $"{description} {request.DoseNumber}.");
            }
        }

        /// <summary>
        /// Reforço pressupõe esquema iniciado: só pode ser registrado se a pessoa já tiver
        /// ao menos uma dose normal da mesma vacina. Sem isto, a primeira aplicação poderia
        /// entrar como reforço — as demais regras não pegam esse caso, porque a dose 1 não
        /// tem antecessora para comparar.
        /// </summary>
        private static void EnsureBoosterFollowsInitialDose(
            IReadOnlyList<VaccinationRecord> registeredDoses,
            RegisterVaccinationCommand request,
            Vaccine vaccine)
        {
            if (request.VaccinationType != VaccinationTypeEnum.BoosterDose)
            {
                return;
            }

            if (!registeredDoses.Any(dose => dose.VaccinationType == VaccinationTypeEnum.Dose))
            {
                throw new ConflictException(
                    $"A vacina '{vaccine.Name}' não possui nenhuma dose registrada para esta "
                    + "pessoa; o reforço só pode ser aplicado após a dose inicial.");
            }
        }

        /// <summary>
        /// Uma dose não pode ser anterior à dose do mesmo tipo que a precede na sequência.
        /// </summary>
        private static void EnsureDoseIsNotBackdated(
            IReadOnlyList<VaccinationRecord> sameTypeDoses,
            RegisterVaccinationCommand request)
        {
            var previousDose = sameTypeDoses
                .FirstOrDefault(dose => dose.DoseNumber == request.DoseNumber - 1);

            if (previousDose is not null && request.VaccinationDate < previousDose.VaccinationDate)
            {
                var description = request.VaccinationType.Describe();

                throw new ConflictException(
                    $"A {description} {request.DoseNumber} não pode ser anterior à "
                    + $"{description} {previousDose.DoseNumber}, aplicada em "
                    + $"{previousDose.VaccinationDate:dd/MM/yyyy}.");
            }
        }
    }
}
