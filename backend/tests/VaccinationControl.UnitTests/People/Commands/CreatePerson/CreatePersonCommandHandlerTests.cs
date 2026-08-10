using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Application.People.Commands.CreatePerson;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.People.Commands.CreatePerson
{
    public class CreatePersonCommandHandlerTests
    {
        private const string Documento = "12345678901";

        private readonly IPersonRepository _personRepository = Substitute.For<IPersonRepository>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private readonly CreatePersonCommandHandler _handler;

        public CreatePersonCommandHandlerTests()
        {
            _handler = new CreatePersonCommandHandler(_personRepository, _unitOfWork);
        }

        [Fact]
        public async Task Deve_recusar_documento_ja_cadastrado()
        {
            _personRepository.ExistsByDocumentAsync(Documento, Arg.Any<CancellationToken>())
                .Returns(true);

            var act = () => _handler.Handle(
                new CreatePersonCommand("Maria Silva", Documento),
                CancellationToken.None);

            (await act.Should().ThrowAsync<ConflictException>())
                .WithMessage("*já existe*");

            _personRepository.DidNotReceive().Add(Arg.Any<Person>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_remover_espacos_das_pontas_do_nome_e_do_documento()
        {
            _personRepository.ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var response = await _handler.Handle(
                new CreatePersonCommand("  Maria Silva  ", $"  {Documento}  "),
                CancellationToken.None);

            response.Name.Should().Be("Maria Silva");
            response.Document.Should().Be(Documento);
        }

        [Fact]
        public async Task Deve_cadastrar_e_gravar_quando_o_documento_e_novo()
        {
            _personRepository.ExistsByDocumentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var response = await _handler.Handle(
                new CreatePersonCommand("Maria Silva", Documento),
                CancellationToken.None);

            response.Id.Should().NotBeEmpty();

            _personRepository.Received(1).Add(Arg.Is<Person>(person =>
                person.Name == "Maria Silva" && person.Document == Documento));
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
