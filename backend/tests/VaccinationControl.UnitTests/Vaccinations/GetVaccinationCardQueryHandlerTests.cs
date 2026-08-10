using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.Vaccinations.Queries.GetVaccinationCard;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Enums;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Vaccinations
{
    public class GetVaccinationCardQueryHandlerTests
    {
        private static readonly Guid PersonId = Guid.NewGuid();
        private static readonly Guid HepatiteId = Guid.NewGuid();
        private static readonly Guid BcgId = Guid.NewGuid();

        private readonly IPersonRepository _personRepository = Substitute.For<IPersonRepository>();
        private readonly IVaccinationRecordRepository _recordRepository =
            Substitute.For<IVaccinationRecordRepository>();

        private readonly GetVaccinationCardQueryHandler _handler;

        public GetVaccinationCardQueryHandlerTests()
        {
            _handler = new GetVaccinationCardQueryHandler(_personRepository, _recordRepository);

            _personRepository.GetByIdAsync(PersonId, Arg.Any<CancellationToken>())
                .Returns(new Person { Id = PersonId, Name = "Maria Silva", Document = "12345678901" });
        }

        private void DadosOsRegistros(params VaccinationRecord[] registros)
        {
            _recordRepository.GetByPersonAsync(PersonId, Arg.Any<CancellationToken>())
                .Returns(registros);
        }

        private static VaccinationRecord Registro(
            Guid vaccineId,
            string vaccineName,
            int doseNumber,
            VaccinationTypeEnum vaccinationType = VaccinationTypeEnum.Dose)
        {
            return new VaccinationRecord
            {
                PersonId = PersonId,
                VaccineId = vaccineId,
                Vaccine = new Vaccine { Id = vaccineId, Name = vaccineName },
                VaccinationType = vaccinationType,
                DoseNumber = doseNumber,
                VaccinationDate = new DateOnly(2024, 1, 10)
            };
        }

        private Task<VaccinationCardResponse> Consultar() => _handler.Handle(
            new GetVaccinationCardQuery(PersonId),
            CancellationToken.None);

        [Fact]
        public async Task Deve_falhar_quando_a_pessoa_nao_existe()
        {
            _personRepository.GetByIdAsync(PersonId, Arg.Any<CancellationToken>())
                .Returns((Person?)null);

            var act = Consultar;

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task Cartao_sem_registros_deve_vir_vazio_e_nao_falhar()
        {
            // A pessoa existe; o cartão dela é que está vazio.
            DadosOsRegistros();

            var card = await Consultar();

            card.PersonName.Should().Be("Maria Silva");
            card.Vaccines.Should().BeEmpty();
        }

        [Fact]
        public async Task Deve_agrupar_os_registros_por_vacina()
        {
            DadosOsRegistros(
                Registro(HepatiteId, "Hepatite B", 1),
                Registro(HepatiteId, "Hepatite B", 2),
                Registro(HepatiteId, "Hepatite B", 1, VaccinationTypeEnum.BoosterDose),
                Registro(BcgId, "BCG", 1));

            var card = await Consultar();

            card.Vaccines.Should().HaveCount(2);

            var hepatite = card.Vaccines.Single(vaccine => vaccine.VaccineId == HepatiteId);
            hepatite.VaccineName.Should().Be("Hepatite B");
            hepatite.Doses.Should().HaveCount(3);
        }

        [Fact]
        public async Task TotalDoses_deve_somar_doses_normais_e_reforcos()
        {
            DadosOsRegistros(
                Registro(HepatiteId, "Hepatite B", 1),
                Registro(HepatiteId, "Hepatite B", 1, VaccinationTypeEnum.BoosterDose));

            var card = await Consultar();

            card.Vaccines.Single().TotalDoses.Should().Be(2);
        }

        [Fact]
        public async Task Cada_dose_deve_expor_o_identificador_do_registro()
        {
            // É o recordId que o cliente usa para remover aquela aplicação.
            var registro = Registro(BcgId, "BCG", 1);

            DadosOsRegistros(registro);

            var card = await Consultar();

            card.Vaccines.Single().Doses.Single().RecordId.Should().Be(registro.Id);
        }
    }
}
