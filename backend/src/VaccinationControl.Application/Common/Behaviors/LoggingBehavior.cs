using System.Diagnostics;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using VaccinationControl.Application.Common.Interfaces;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Application.Common.Behaviors
{
    /// <summary>
    /// Registra uma linha por caso de uso, sempre com os mesmos campos. Fica no pipeline, e
    /// não nos handlers, porque log espalhado por handler diverge no primeiro caso de uso
    /// novo — em formato, em nível e em quem lembra de escrevê-lo.
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse>(
        ILoggerFactory loggerFactory,
        ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        /// <summary>
        /// Categoria fixa para todas as linhas de caso de uso. O padrão do
        /// <c>ILogger&lt;T&gt;</c> produziria uma categoria por combinação de request e
        /// resposta, com os genéricos por extenso — impossível de filtrar no appsettings.
        /// </summary>
        public const string Category = "VaccinationControl.UseCase";

        private readonly ILogger _logger = loggerFactory.CreateLogger(Category);

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Sem linha de início: ela dobra o volume e não diz nada que a de conclusão não
            // diga. O que só existe durante a execução — a duração — é medido aqui.
            var start = Stopwatch.GetTimestamp();
            var useCase = typeof(TRequest).Name;
            var userId = currentUser.Id?.ToString() ?? Anonymous;

            try
            {
                var response = await next();

                _logger.LogInformation(
                    "{UseCase} concluido em {ElapsedMs} ms por {UserId}",
                    useCase,
                    Elapsed(start),
                    userId);

                return response;
            }
            catch (ValidationException exception)
            {
                // Os nomes dos campos, nunca os valores: é o que basta para saber qual
                // formulário está mandando entrada inválida.
                _logger.LogWarning(
                    "{UseCase} rejeitado na validacao de {Campos} em {ElapsedMs} ms por {UserId}",
                    useCase,
                    FailedFields(exception),
                    Elapsed(start),
                    userId);

                throw;
            }
            catch (DomainException exception)
            {
                _logger.LogWarning(
                    "{UseCase} recusado por {Motivo} em {ElapsedMs} ms por {UserId}",
                    useCase,
                    exception.GetType().Name,
                    Elapsed(start),
                    userId);

                throw;
            }

            // Falha inesperada não é registrada aqui de propósito: quem a registra, com a
            // exceção inteira e em nível de erro, é o GlobalExceptionHandler. Logar nos dois
            // lugares produziria duas entradas para o mesmo incidente.
        }

        private const string Anonymous = "anonimo";

        private static long Elapsed(long start)
        {
            return (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        private static string FailedFields(ValidationException exception)
        {
            return string.Join(
                ", ",
                exception.Errors.Select(failure => failure.PropertyName).Distinct());
        }
    }
}
