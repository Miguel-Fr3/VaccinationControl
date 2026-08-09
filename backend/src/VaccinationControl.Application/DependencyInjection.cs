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
                configuration.RegisterServicesFromAssembly(applicationAssembly);

                // Validação roda no pipeline, antes de qualquer handler.
                configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            services.AddValidatorsFromAssembly(applicationAssembly);

            return services;
        }
    }
}
