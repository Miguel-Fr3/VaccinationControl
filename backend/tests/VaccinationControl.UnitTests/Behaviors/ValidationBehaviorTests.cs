using FluentAssertions;
using FluentValidation;
using MediatR;
using VaccinationControl.Application.Common.Behaviors;

namespace VaccinationControl.UnitTests.Behaviors
{
    /// <summary>
    /// O behavior é o que torna a validação automática: se ele parar de rodar, nenhum
    /// handler percebe, porque nenhum handler valida entrada.
    /// </summary>
    public class ValidationBehaviorTests
    {
        public record RequisicaoFake(string Nome) : IRequest<string>;

        private class ValidatorQueRecusa : AbstractValidator<RequisicaoFake>
        {
            public ValidatorQueRecusa()
            {
                RuleFor(requisicao => requisicao.Nome).NotEmpty();
            }
        }

        // Assinatura igual à do RequestHandlerDelegate do MediatR, que recebe o token.
        private static Task<string> Proximo(CancellationToken cancellationToken = default)
        {
            return Task.FromResult("chegou no handler");
        }

        [Fact]
        public async Task Deve_chamar_o_handler_quando_nao_ha_validators()
        {
            var behavior = new ValidationBehavior<RequisicaoFake, string>([]);

            var resultado = await behavior.Handle(
                new RequisicaoFake(""),
                Proximo,
                CancellationToken.None);

            resultado.Should().Be("chegou no handler");
        }

        [Fact]
        public async Task Deve_chamar_o_handler_quando_a_validacao_passa()
        {
            var behavior = new ValidationBehavior<RequisicaoFake, string>([new ValidatorQueRecusa()]);

            var resultado = await behavior.Handle(
                new RequisicaoFake("Maria"),
                Proximo,
                CancellationToken.None);

            resultado.Should().Be("chegou no handler");
        }

        [Fact]
        public async Task Deve_lancar_sem_chegar_no_handler_quando_a_validacao_falha()
        {
            var behavior = new ValidationBehavior<RequisicaoFake, string>([new ValidatorQueRecusa()]);
            var chegouNoHandler = false;

            var act = () => behavior.Handle(
                new RequisicaoFake(""),
                _ =>
                {
                    chegouNoHandler = true;
                    return Proximo();
                },
                CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
            chegouNoHandler.Should().BeFalse();
        }

        [Fact]
        public async Task Deve_agregar_as_falhas_de_todos_os_validators()
        {
            var behavior = new ValidationBehavior<RequisicaoFake, string>(
                [new ValidatorQueRecusa(), new ValidatorQueRecusa()]);

            // Assert.ThrowsAsync garante uma única execução do pipeline.
            var excecao = await Assert.ThrowsAsync<ValidationException>(
                () => behavior.Handle(new RequisicaoFake(""), Proximo, CancellationToken.None));

            excecao.Errors.Should().HaveCount(2);
        }
    }
}
