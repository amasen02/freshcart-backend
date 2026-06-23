using System.Reflection;
using FluentValidation;
using FreshCart.BuildingBlocks.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FreshCart.Identity.Application;

/// <summary>
/// Composition root for the application layer. Registers MediatR with the pipeline behaviors and
/// every FluentValidation validator discovered in this assembly.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
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

        return services;
    }
}
