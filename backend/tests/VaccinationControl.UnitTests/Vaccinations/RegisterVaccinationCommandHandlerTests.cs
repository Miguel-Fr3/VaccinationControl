using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.Vaccinations.Commands.RegisterVaccination;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Enums;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Vaccinations
{
    /// <summary>
    /// Cobre RN03 a RN08 — as regras que dependem do estado já gravado. Os repositórios são
    /// substitutos, então nenhum teste aqui toca banco: o que se verifica é a decisão do
    /// handler diante das doses que já existem.
    /// </summary>
    public class RegisterVaccinationCommandHandlerTests
    {
        private static readonly Guid PersonId = Guid.NewGuid();
        private static readonly Guid VaccineId = Guid.NewGuid();

        private readonly IPersonRepository _personRepository = Substitute.For<IPersonRepository>();
        private readonly IVaccineRepository _vaccineRepository = Substitute.For<IVaccineRepository>();
        private readonly IVaccinationRecordRepository _recordRepository =
            Substitute.For<IVaccinationRecordRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private readonly RegisterVaccinationCommandHandler _handler;

        public RegisterVaccinationCommandHandlerTests()
        {
            _handler = new RegisterVaccinationCommandHandler(
                _personRepository,
                _vaccineRepository,
                _recordRepository,
                _unitOfWork);

            // Por padrão pessoa e vacina existem; cada teste sobrescreve o que precisar.
            _personRepository.GetByIdAsync(PersonId, Arg.Any<CancellationToken>())
                .Returns(new Person { Id = PersonId, Name = "Maria Silva", Document = "12345678901" });

            _vaccineRepository.GetByIdAsync(VaccineId, Arg.Any<CancellationToken>())
                .Returns(new Vaccine { Id = VaccineId, Name = "Hepatite B" });

            DadasAsDosesRegistradas();
        }

        private void DadasAsDosesRegistradas(params VaccinationRecord[] doses)
        {
            _recordRepository
                .GetDosesAsync(PersonId, VaccineId, Arg.Any<CancellationToken>())
                .Returns(doses);
        }

        private static VaccinationRecord Dose(
            int doseNumber,
            VaccinationTypeEnum vaccinationType = VaccinationTypeEnum.Dose,
            string vaccinationDate = "2024-01-10")
        {
            return new VaccinationRecord
            {
                PersonId = PersonId,
                VaccineId = VaccineId,
                VaccinationType = vaccinationType,
                DoseNumber = doseNumber,
                VaccinationDate = DateOnly.Parse(vaccinationDate)
            };
        }

        private static RegisterVaccinationCommand Comando(
            int doseNumber,
            VaccinationTypeEnum vaccinationType = VaccinationTypeEnum.Dose,
            string vaccinationDate = "2024-06-10")
        {
            return new RegisterVaccinationCommand(
                PersonId,
                VaccineId,
                vaccinationType,
                doseNumber,
                DateOnly.Parse(vaccinationDate));
        }

        private Task<Application.Vaccinations.VaccinationRecordResponse> Registrar(
            RegisterVaccinationCommand command)
        {
            return _handler.Handle(command, CancellationToken.None);
        }

        [Fact]
        public async Task RN03_deve_falhar_quando_a_pessoa_nao_existe()
        {
            _personRepository.GetByIdAsync(PersonId, Arg.Any<CancellationToken>())
                .Returns((Person?)null);

            var act = () => Registrar(Comando(1));

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RN04_deve_falhar_quando_a_vacina_nao_existe()
        {
            _vaccineRepository.GetByIdAsync(VaccineId, Arg.Any<CancellationToken>())
                .Returns((Vaccine?)null);

            var act = () => Registrar(Comando(1));

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RN05_deve_recusar_a_mesma_dose_do_mesmo_tipo()
        {
            DadasAsDosesRegistradas(Dose(1));

            var act = () => Registrar(Comando(1));

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage("*dose 1*já foi registrada*");
        }

        [Fact]
        public async Task RN05_deve_aceitar_a_mesma_dose_em_tipo_diferente()
        {
            // Numeração é independente por tipo: dose normal 1 e reforço 1 coexistem.
            DadasAsDosesRegistradas(Dose(1));

            var response = await Registrar(Comando(1, VaccinationTypeEnum.BoosterDose));

            response.DoseNumber.Should().Be(1);
            response.VaccinationType.Should().Be(VaccinationTypeEnum.BoosterDose);
        }

        [Fact]
        public async Task RN06_deve_recusar_dose_sem_a_anterior()
        {
            var act = () => Registrar(Comando(2));

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage("*dose 1 precisa ser registrada antes*");
        }

        [Fact]
        public async Task RN06_deve_exigir_a_anterior_do_mesmo_tipo()
        {
            // Ter a dose normal 1 não habilita o reforço 2 — o reforço 1 é que habilita.
            DadasAsDosesRegistradas(Dose(1));

            var act = () => Registrar(Comando(2, VaccinationTypeEnum.BoosterDose));

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage("*dose de reforço 1 precisa ser registrada*");
        }

        [Fact]
        public async Task RN07_deve_recusar_dose_anterior_a_data_da_anterior()
        {
            DadasAsDosesRegistradas(Dose(1, vaccinationDate: "2024-03-10"));

            var act = () => Registrar(Comando(2, vaccinationDate: "2024-01-01"));

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage("*não pode ser anterior*");
        }

        [Fact]
        public async Task RN08_deve_recusar_reforco_sem_nenhuma_dose_normal()
        {
            var act = () => Registrar(Comando(1, VaccinationTypeEnum.BoosterDose));

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage("*não possui nenhuma dose registrada*");
        }

        [Fact]
        public async Task Deve_registrar_a_primeira_dose_e_gravar()
        {
            var response = await Registrar(Comando(1));

            response.PersonId.Should().Be(PersonId);
            response.VaccineId.Should().Be(VaccineId);
            response.VaccineName.Should().Be("Hepatite B");
            response.DoseNumber.Should().Be(1);

            _recordRepository.Received(1).Add(Arg.Any<VaccinationRecord>());
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Nao_deve_gravar_quando_uma_regra_falha()
        {
            DadasAsDosesRegistradas(Dose(1));

            var act = () => Registrar(Comando(1));

            await act.Should().ThrowAsync<ConflictException>();

            _recordRepository.DidNotReceive().Add(Arg.Any<VaccinationRecord>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
