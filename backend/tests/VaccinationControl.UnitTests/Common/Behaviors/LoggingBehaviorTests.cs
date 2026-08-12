using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using VaccinationControl.Application.Common.Behaviors;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.UnitTests.Common.Behaviors
{
    /// <summary>
    /// O behavior é a única fonte das linhas de caso de uso. O que se verifica aqui é o
    /// contrato delas: um nível por desfecho, os mesmos campos sempre, e nada do request —
    /// que carrega senha e CPF.
    /// </summary>
    public class LoggingBehaviorTests
    {
        public record RequisicaoFake(string Senha) : IRequest<string>;

        /// <summary>Captura o que foi registrado, já com o template renderizado.</summary>
        private class LoggerEspiao : ILogger
        {
            public List<(LogLevel Level, string Message)> Entradas { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Entradas.Add((logLevel, formatter(state, exception)));
            }
        }

        private static readonly Guid UserId = Guid.NewGuid();

        private readonly LoggerEspiao _logger = new();
        private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

        private LoggingBehavior<RequisicaoFake, string> Behavior()
        {
            var factory = Substitute.For<ILoggerFactory>();
            factory.CreateLogger(Arg.Any<string>()).Returns(_logger);

            return new LoggingBehavior<RequisicaoFake, string>(factory, _currentUser);
        }

        private Task<string> Executar(RequestHandlerDelegate<string> proximo)
        {
            return Behavior().Handle(new RequisicaoFake("senha12345"), proximo, CancellationToken.None);
        }

        private static Task<string> Concluido(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("chegou no handler");
        }

        [Fact]
        public async Task Deve_registrar_uma_linha_de_conclusao_com_o_caso_de_uso_e_o_usuario()
        {
            _currentUser.Id.Returns(UserId);

            var resultado = await Executar(Concluido);

            resultado.Should().Be("chegou no handler");

            var entrada = _logger.Entradas.Should().ContainSingle().Subject;
            entrada.Level.Should().Be(LogLevel.Information);
            entrada.Message.Should().Contain("RequisicaoFake concluido");
            entrada.Message.Should().Contain(UserId.ToString());
        }

        [Fact]
        public async Task Deve_identificar_a_requisicao_sem_sessao_como_anonima()
        {
            // Login e cadastro do primeiro usuário passam por aqui sem ninguém autenticado.
            _currentUser.Id.Returns((Guid?)null);

            await Executar(Concluido);

            _logger.Entradas.Should().ContainSingle()
                .Which.Message.Should().Contain("anonimo");
        }

        [Fact]
        public async Task Deve_registrar_a_recusa_de_dominio_como_aviso_sem_engolir_a_excecao()
        {
            var act = () => Executar(_ => throw new ConflictException("A vacina 'BCG' ja existe."));

            await act.Should().ThrowAsync<ConflictException>();

            var entrada = _logger.Entradas.Should().ContainSingle().Subject;
            entrada.Level.Should().Be(LogLevel.Warning);
            entrada.Message.Should().Contain("recusado por ConflictException");
        }

        [Fact]
        public async Task Deve_registrar_o_motivo_pelo_tipo_e_nunca_pela_mensagem()
        {
            // A mensagem da exceção repete o dado do usuário — o CPF, no cadastro de pessoa.
            var act = () => Executar(
                _ => throw new ConflictException("Ja existe uma pessoa com o CPF '12345678901'."));

            await act.Should().ThrowAsync<ConflictException>();

            _logger.Entradas.Should().ContainSingle()
                .Which.Message.Should().NotContain("12345678901");
        }

        [Fact]
        public async Task Deve_registrar_os_campos_invalidos_da_validacao()
        {
            var falhas = new[]
            {
                new ValidationFailure("Name", "Nome é obrigatório."),
                new ValidationFailure("Document", "CPF inválido.")
            };

            var act = () => Executar(_ => throw new ValidationException(falhas));

            await act.Should().ThrowAsync<ValidationException>();

            var entrada = _logger.Entradas.Should().ContainSingle().Subject;
            entrada.Level.Should().Be(LogLevel.Warning);
            entrada.Message.Should().Contain("Name, Document");
        }

        [Fact]
        public async Task Nao_deve_registrar_a_falha_inesperada_que_o_handler_de_excecao_ja_registra()
        {
            // Duas entradas para o mesmo incidente é o que se evita: a exceção inteira sai
            // uma vez só, em nível de erro, no GlobalExceptionHandler.
            var act = () => Executar(_ => throw new InvalidOperationException("banco fora do ar"));

            await act.Should().ThrowAsync<InvalidOperationException>();

            _logger.Entradas.Should().BeEmpty();
        }

        [Fact]
        public async Task Nunca_deve_registrar_o_conteudo_do_request()
        {
            // O LoginCommand carrega a senha em claro: registrar o request a colocaria no log.
            _currentUser.Id.Returns(UserId);

            await Executar(Concluido);

            _logger.Entradas.Should().ContainSingle()
                .Which.Message.Should().NotContain("senha12345");
        }
    }
}
