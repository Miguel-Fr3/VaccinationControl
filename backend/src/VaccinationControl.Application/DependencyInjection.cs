using System.Globalization;
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using VaccinationControl.Application.Common.Behaviors;

namespace VaccinationControl.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var applicationAssembly = Assembly.GetExecutingAssembly();

            services.AddMediatR(configuration =>
            {
                // Registra todos os handlers do MediatR no assembly da aplicação
                configuration.RegisterServicesFromAssembly(applicationAssembly);

                // Adiciona o comportamento de logging no pipeline.
                configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));

                // Validação roda no pipeline, antes de qualquer handler.
                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });
            // Registra todos os validadores do FluentValidation no assembly da aplicação
            services.AddValidatorsFromAssembly(applicationAssembly);

            // Configura a cultura padrão do FluentValidation para pt-BR
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");

            return services;
        }
    }
}
