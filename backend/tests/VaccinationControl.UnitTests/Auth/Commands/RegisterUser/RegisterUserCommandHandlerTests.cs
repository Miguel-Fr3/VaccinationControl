using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Auth.Commands.RegisterUser;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Auth.Commands.RegisterUser
{
    public class RegisterUserCommandHandlerTests
    {
        private const string Email = "admin@exemplo.com";
        private const string Senha = "senha12345";

        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
        private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

        private readonly RegisterUserCommandHandler _handler;

        public RegisterUserCommandHandlerTests()
        {
            _handler = new RegisterUserCommandHandler(
                _userRepository,
                _passwordHasher,
                _tokenGenerator,
                _unitOfWork);

            _passwordHasher.Hash(Senha).Returns("hash-da-senha");
            _tokenGenerator.Generate(Arg.Any<User>())
                .Returns(("token-jwt", DateTime.UtcNow.AddHours(1)));
        }

        [Fact]
        public async Task Deve_recusar_email_ja_cadastrado()
        {
            _userRepository.ExistsByEmailAsync(Email, Arg.Any<CancellationToken>()).Returns(true);

            var act = () => _handler.Handle(
                new RegisterUserCommand(Email, Senha),
                CancellationToken.None);

            await act.Should().ThrowAsync<ConflictException>();

            _userRepository.DidNotReceive().Add(Arg.Any<User>());
            await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_normalizar_o_email_antes_de_gravar()
        {
            _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var response = await _handler.Handle(
                new RegisterUserCommand("  Admin@Exemplo.COM  ", Senha),
                CancellationToken.None);

            response.Email.Should().Be(Email);
            _userRepository.Received(1).Add(Arg.Is<User>(user => user.Email == Email));
        }

        [Fact]
        public async Task Deve_gravar_o_hash_e_nunca_a_senha_em_claro()
        {
            _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            await _handler.Handle(new RegisterUserCommand(Email, Senha), CancellationToken.None);

            _userRepository.Received(1).Add(Arg.Is<User>(user =>
                user.PasswordHash == "hash-da-senha" && user.PasswordHash != Senha));
        }

        [Fact]
        public async Task Deve_devolver_o_token_apos_cadastrar()
        {
            _userRepository.ExistsByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(false);

            var response = await _handler.Handle(
                new RegisterUserCommand(Email, Senha),
                CancellationToken.None);

            response.Token.Should().Be("token-jwt");
            response.UserId.Should().NotBeEmpty();
            await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
