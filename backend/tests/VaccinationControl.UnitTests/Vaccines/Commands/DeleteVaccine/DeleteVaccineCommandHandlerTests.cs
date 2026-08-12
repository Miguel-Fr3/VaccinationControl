using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.Vaccines.Commands.DeleteVaccine;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Vaccines.Commands.DeleteVaccine
{
    public class DeleteVaccineCommandHandlerTests
    {
        private static readonly Guid VaccineId = Guid.NewGuid();

        private readonly IVaccineRepository _vaccineRepository =
            Substitute.For<IVaccineRepository>();
        private readonly IVaccinationRecordRepository _recordRepository =
            Substitute.For<IVaccinationRecordRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private readonly DeleteVaccineCommandHandler _handler;

        public DeleteVaccineCommandHandlerTests()
        {
            _handler = new DeleteVaccineCommandHandler(
                _vaccineRepository,
                _recordRepository,
                _unitOfWork);
        }

        private Task Remover() => _handler.Handle(
            new DeleteVaccineCommand(VaccineId),
            CancellationToken.None);

        private Vaccine DarVacinaExistente()
        {
            var vaccine = new Vaccine { Id = VaccineId, Name = "Hepatite B" };

            _vaccineRepository.GetByIdAsync(VaccineId, Arg.Any<CancellationToken>())
                .Returns(vaccine);

            return vaccine;
        }

        [Fact]
        public async Task Deve_falhar_quando_a_vacina_nao_existe()
        {
            _vaccineRepository.GetByIdAsync(VaccineId, Arg.Any<CancellationToken>())
                .Returns((Vaccine?)null);

            var act = Remover;

            await act.Should().ThrowAsync<NotFoundException>();

            _vaccineRepository.DidNotReceive().Remove(Arg.Any<Vaccine>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_recusar_vacina_com_dose_registrada()
        {
            var vaccine = DarVacinaExistente();

            _recordRepository.ExistsByVaccineAsync(VaccineId, Arg.Any<CancellationToken>())
                .Returns(true);

            var act = Remover;

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage($"*{vaccine.Name}*");

            _vaccineRepository.DidNotReceive().Remove(Arg.Any<Vaccine>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_remover_e_gravar_quando_a_vacina_nao_esta_em_uso()
        {
            var vaccine = DarVacinaExistente();

            _recordRepository.ExistsByVaccineAsync(VaccineId, Arg.Any<CancellationToken>())
                .Returns(false);

            await Remover();

            _vaccineRepository.Received(1).Remove(vaccine);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
