using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VaccinationControl.Domain.Exceptions;

namespace VaccinationControl.Api.Middleware
{
    /// <summary>
    /// Traduz as falhas previstas em respostas HTTP. É aqui que a distinção entre 400, 404
    /// e 409 acontece — o domínio e a Application não conhecem códigos de status.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private const string ProblemJsonContentType = "application/problem+json";

        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var problemDetails = exception switch
            {
                ValidationException validationException => BuildValidationProblem(validationException),

                NotFoundException notFoundException => BuildProblem(
                    StatusCodes.Status404NotFound,
                    "Recurso não encontrado",
                    notFoundException.Message),

                ConflictException conflictException => BuildProblem(
                    StatusCodes.Status409Conflict,
                    "Conflito com o estado atual",
                    conflictException.Message),

                DomainException domainException => BuildProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Regra de negócio violada",
                    domainException.Message),

                _ => null
            };

            if (problemDetails is null)
            {
                // Falha não prevista: registra e devolve o pipeline padrão, que responde 500.
                _logger.LogError(
                    exception,
                    "Falha nao tratada ao processar {Method} {Path}.",
                    httpContext.Request.Method,
                    httpContext.Request.Path);

                return false;
            }

            problemDetails.Instance = httpContext.Request.Path;
            httpContext.Response.StatusCode = problemDetails.Status!.Value;

            // O tipo concreto precisa ser explícito: serializado como ProblemDetails, o
            // dicionário Errors do ValidationProblemDetails ficaria de fora da resposta.
            // O content-type de erro é o da RFC 9457, não application/json.
            await httpContext.Response.WriteAsJsonAsync(
                problemDetails,
                problemDetails.GetType(),
                options: null,
                contentType: ProblemJsonContentType,
                cancellationToken);

            return true;
        }

        private static ValidationProblemDetails BuildValidationProblem(ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).ToArray());

            return new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Requisição inválida"
            };
        }

        private static ProblemDetails BuildProblem(int status, string title, string detail)
        {
            return new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            };
        }
    }
}
