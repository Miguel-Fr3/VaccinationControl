using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Auth.Commands.Login;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Auth
{
    public class LoginCommandHandlerTests
    {
        private const string Email = "admin@exemplo.com";
        private const string Senha = "senha12345";
        private const string HashGravado = "hash-gravado";

        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
        private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();

        private readonly LoginCommandHandler _handler;

        public LoginCommandHandlerTests()
        {
            _handler = new LoginCommandHandler(_userRepository, _passwordHasher, _tokenGenerator);
        }

        private void DadoOUsuarioCadastrado()
        {
            _userRepository.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
                .Returns(new User { Email = Email, PasswordHash = HashGravado });
        }

        [Fact]
        public async Task Deve_recusar_quando_o_email_nao_existe()
        {
            _userRepository.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
                .Returns((User?)null);

            var act = () => _handler.Handle(new LoginCommand(Email, Senha), CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task Deve_recusar_quando_a_senha_nao_confere()
        {
            DadoOUsuarioCadastrado();
            _passwordHasher.Verify(Senha, HashGravado).Returns(false);

            var act = () => _handler.Handle(new LoginCommand(Email, Senha), CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task Deve_usar_a_mesma_mensagem_para_email_inexistente_e_senha_errada()
        {
            // Distinguir os dois permitiria descobrir quais e-mails estão cadastrados.
            _userRepository.GetByEmailAsync(Email, Arg.Any<CancellationToken>())
                .Returns((User?)null);

            var semUsuario = await Assert.ThrowsAsync<UnauthorizedException>(
                () => _handler.Handle(new LoginCommand(Email, Senha), CancellationToken.None));

            DadoOUsuarioCadastrado();
            _passwordHasher.Verify(Senha, HashGravado).Returns(false);

            var senhaErrada = await Assert.ThrowsAsync<UnauthorizedException>(
                () => _handler.Handle(new LoginCommand(Email, Senha), CancellationToken.None));

            senhaErrada.Message.Should().Be(semUsuario.Message);
        }

        [Fact]
        public async Task Deve_normalizar_o_email_antes_de_buscar()
        {
            DadoOUsuarioCadastrado();
            _passwordHasher.Verify(Senha, HashGravado).Returns(true);
            _tokenGenerator.Generate(Arg.Any<User>()).Returns(("token", DateTime.UtcNow.AddHours(1)));

            await _handler.Handle(new LoginCommand("  Admin@Exemplo.COM  ", Senha), CancellationToken.None);

            await _userRepository.Received(1).GetByEmailAsync(Email, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Deve_devolver_o_token_quando_a_credencial_confere()
        {
            var expiracao = DateTime.UtcNow.AddHours(1);

            DadoOUsuarioCadastrado();
            _passwordHasher.Verify(Senha, HashGravado).Returns(true);
            _tokenGenerator.Generate(Arg.Any<User>()).Returns(("token-jwt", expiracao));

            var response = await _handler.Handle(
                new LoginCommand(Email, Senha),
                CancellationToken.None);

            response.Email.Should().Be(Email);
            response.Token.Should().Be("token-jwt");
            response.ExpiresAtUtc.Should().Be(expiracao);
        }
    }
}
