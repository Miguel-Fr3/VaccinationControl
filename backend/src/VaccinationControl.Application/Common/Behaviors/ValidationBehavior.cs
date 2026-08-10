using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace VaccinationControl.Application.Common.Behaviors
{
    /// <summary>
    /// Roda os validators registrados para o request antes do handler. Com isso nenhum
    /// handler precisa validar entrada: se chegou até ele, o request já está bem formado.
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);
            var failures = new List<ValidationFailure>();

            // Sequencial de propósito: um validator pode consultar o banco, e o DbContext
            // é scoped e não suporta duas operações simultâneas.
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(context, cancellationToken);

                failures.AddRange(result.Errors);
            }

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }

            return await next();
        }
    }
}
