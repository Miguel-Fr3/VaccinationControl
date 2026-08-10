using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.Vaccines.Commands.CreateVaccine;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Vaccines.Commands.CreateVaccine
{
    public class CreateVaccineCommandHandlerTests
    {
        private readonly IVaccineRepository _vaccineRepository = Substitute.For<IVaccineRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private readonly CreateVaccineCommandHandler _handler;

        public CreateVaccineCommandHandlerTests()
        {
            _handler = new CreateVaccineCommandHandler(_vaccineRepository, _unitOfWork);
        }

        [Fact]
        public async Task Deve_recusar_nome_ja_cadastrado()
        {
            _vaccineRepository.ExistsByNameAsync("Hepatite B", Arg.Any<CancellationToken>())
                .Returns(true);

            var act = () => _handler.Handle(
                new CreateVaccineCommand("Hepatite B"),
                CancellationToken.None);

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage("*já existe*");

            _vaccineRepository.DidNotReceive().Add(Arg.Any<Vaccine>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_remover_espacos_das_pontas_antes_de_verificar_duplicidade()
        {
            _vaccineRepository.ExistsByNameAsync("Hepatite B", Arg.Any<CancellationToken>())
                .Returns(false);

            var response = await _handler.Handle(
                new CreateVaccineCommand("  Hepatite B  "),
                CancellationToken.None);

            response.Name.Should().Be("Hepatite B");
            await _vaccineRepository.Received(1)
                .ExistsByNameAsync("Hepatite B", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_cadastrar_e_gravar_quando_o_nome_e_novo()
        {
            _vaccineRepository.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var response = await _handler.Handle(
                new CreateVaccineCommand("BCG"),
                CancellationToken.None);

            response.Id.Should().NotBeEmpty();
            response.Name.Should().Be("BCG");

            _vaccineRepository.Received(1).Add(Arg.Is<Vaccine>(vaccine => vaccine.Name == "BCG"));
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
