using System.Reflection;
using FluentValidation;
using FreshCart.BuildingBlocks.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FreshCart.Reporting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var applicationAssembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(mediatrConfiguration =>
        {
            mediatrConfiguration.RegisterServicesFromAssembly(applicationAssembly);
            mediatrConfiguration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            mediatrConfiguration.AddOpenBehavior(typeof(LoggingBehavior<,>));
        });

        services.AddValidatorsFromAssembly(applicationAssembly);

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    private static IServiceCollection TryAddSingleton<TService>(this IServiceCollection services, TService instance)
        where TService : class
    {
        Microsoft.Extensions.DependencyInjection.Extensions.ServiceCollectionDescriptorExtensions
            .TryAddSingleton(services, instance);
        return services;
    }
}
