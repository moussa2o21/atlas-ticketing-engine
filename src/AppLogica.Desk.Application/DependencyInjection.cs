using AppLogica.Desk.Application.Common.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AppLogica.Desk.Application;

/// <summary>
/// Registers Application layer services: MediatR handlers, FluentValidation validators,
/// and pipeline behaviors.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // MediatR — scan this assembly for handlers and notification handlers
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation — scan this assembly for all AbstractValidator<T> implementations
        services.AddValidatorsFromAssembly(assembly);

        // Pipeline behaviors — executed in registration order around every MediatR request
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}
