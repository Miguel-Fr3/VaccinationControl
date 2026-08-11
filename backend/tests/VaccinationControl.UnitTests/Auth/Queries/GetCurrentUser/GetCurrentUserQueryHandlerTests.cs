using FluentAssertions;
using NSubstitute;
using VaccinationControl.Application.Auth.Queries.GetCurrentUser;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Entities;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Auth.Queries.GetCurrentUser
{
    public class GetCurrentUserQueryHandlerTests
    {
        private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

        private readonly GetCurrentUserQueryHandler _handler;

        public GetCurrentUserQueryHandlerTests()
        {
            _handler = new GetCurrentUserQueryHandler(_currentUser, _userRepository);
        }

        [Fact]
        public async Task Deve_recusar_quando_nao_ha_ninguem_autenticado()
        {
            _currentUser.Id.Returns((Guid?)null);

            var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task Deve_recusar_quando_o_usuario_do_token_nao_existe_mais()
        {
            // O token continua válido até expirar, mesmo depois de o usuário sumir do banco.
            var userId = Guid.NewGuid();

            _currentUser.Id.Returns(userId);
            _userRepository.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

            var act = () => _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task Deve_devolver_a_identidade_do_usuario_autenticado()
        {
            var user = new User { Email = "admin@exemplo.com", PasswordHash = "hash" };

            _currentUser.Id.Returns(user.Id);
            _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

            var sessao = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            sessao.UserId.Should().Be(user.Id);
            sessao.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task Nao_deve_expor_o_hash_da_senha()
        {
            // A resposta vai para o navegador a cada carregamento de página.
            var user = new User { Email = "admin@exemplo.com", PasswordHash = "hash-secreto" };

            _currentUser.Id.Returns(user.Id);
            _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

            var sessao = await _handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

            sessao.ToString().Should().NotContain("hash-secreto");
        }
    }
}
