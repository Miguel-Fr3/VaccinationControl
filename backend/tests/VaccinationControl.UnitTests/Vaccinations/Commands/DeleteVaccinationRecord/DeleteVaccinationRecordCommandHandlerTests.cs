using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.Vaccinations.Commands.DeleteVaccinationRecord;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Enums;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Vaccinations.Commands.DeleteVaccinationRecord
{
    public class DeleteVaccinationRecordCommandHandlerTests
    {
        private static readonly Guid PersonId = Guid.NewGuid();
        private static readonly Guid RecordId = Guid.NewGuid();

        private readonly IVaccinationRecordRepository _recordRepository =
            Substitute.For<IVaccinationRecordRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private readonly DeleteVaccinationRecordCommandHandler _handler;

        public DeleteVaccinationRecordCommandHandlerTests()
        {
            _handler = new DeleteVaccinationRecordCommandHandler(_recordRepository, _unitOfWork);
        }

        private Task Remover() => _handler.Handle(
            new DeleteVaccinationRecordCommand(PersonId, RecordId),
            CancellationToken.None);

        [Fact]
        public async Task Deve_falhar_quando_o_registro_nao_existe()
        {
            _recordRepository.GetByIdAsync(PersonId, RecordId, Arg.Any<CancellationToken>())
                .Returns((VaccinationRecord?)null);

            var act = Remover;

            await act.Should().ThrowAsync<NotFoundException>();

            _recordRepository.DidNotReceive().Remove(Arg.Any<VaccinationRecord>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_remover_qualquer_dose_inclusive_do_meio_da_sequencia()
        {
            // A remoção é livre: as regras de registro permitem recriar a dose depois.
            var doseDoMeio = new VaccinationRecord
            {
                Id = RecordId,
                PersonId = PersonId,
                VaccineId = Guid.NewGuid(),
                VaccinationType = VaccinationTypeEnum.Dose,
                DoseNumber = 1,
                VaccinationDate = new DateOnly(2024, 1, 10)
            };

            _recordRepository.GetByIdAsync(PersonId, RecordId, Arg.Any<CancellationToken>())
                .Returns(doseDoMeio);

            await Remover();

            _recordRepository.Received(1).Remove(doseDoMeio);
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
