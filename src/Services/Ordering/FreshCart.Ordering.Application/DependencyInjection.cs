using System.Reflection;
using FluentValidation;
using FreshCart.BuildingBlocks.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FreshCart.Ordering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingApplication(this IServiceCollection services)
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
}
