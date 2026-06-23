using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FreshCart.BuildingBlocks.Messaging.MassTransit;

/// <summary>
/// Single entry point used by every service to wire MassTransit against the message broker configured
/// for the current environment. Locally and in DEV the broker is RabbitMQ; in STAGING/PROD the same
/// extension is overloaded to wire Azure Service Bus.
/// </summary>
public static class MessageBrokerExtensions
{
    /// <summary>
    /// Registers MassTransit with RabbitMQ. Consumers, sagas and activities are discovered from the
    /// supplied <paramref name="consumerAssemblies"/>.
    /// </summary>
    public static IServiceCollection AddRabbitMqMessageBroker(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] consumerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var brokerOptions = ReadBrokerOptions(configuration);

        services.AddMassTransit(massTransit =>
        {
            massTransit.SetKebabCaseEndpointNameFormatter();

            if (consumerAssemblies.Length > 0)
            {
                massTransit.AddConsumers(consumerAssemblies);
                massTransit.AddSagaStateMachines(consumerAssemblies);
                massTransit.AddSagas(consumerAssemblies);
                massTransit.AddActivities(consumerAssemblies);
            }

            massTransit.UsingRabbitMq((registrationContext, busFactoryConfigurator) =>
            {
                busFactoryConfigurator.Host(new Uri(brokerOptions.Host), hostConfigurator =>
                {
                    if (!string.IsNullOrWhiteSpace(brokerOptions.UserName))
                    {
                        hostConfigurator.Username(brokerOptions.UserName);
                    }

                    if (!string.IsNullOrWhiteSpace(brokerOptions.Password))
                    {
                        hostConfigurator.Password(brokerOptions.Password);
                    }
                });

                busFactoryConfigurator.UseMessageRetry(retryConfigurator =>
                    retryConfigurator.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromMinutes(1),
                        intervalDelta: TimeSpan.FromSeconds(2)));

                busFactoryConfigurator.ConfigureEndpoints(registrationContext);
            });
        });

        return services;
    }

    private static MessageBrokerOptions ReadBrokerOptions(IConfiguration configuration)
    {
        var brokerSection = configuration.GetSection(MessageBrokerOptions.SectionName);
        var brokerHost = brokerSection[nameof(MessageBrokerOptions.Host)];

        if (string.IsNullOrWhiteSpace(brokerHost))
        {
            throw new InvalidOperationException(
                $"Configuration value \"{MessageBrokerOptions.SectionName}:{nameof(MessageBrokerOptions.Host)}\" is required.");
        }

        return new MessageBrokerOptions
        {
            Host = brokerHost,
            UserName = brokerSection[nameof(MessageBrokerOptions.UserName)],
            Password = brokerSection[nameof(MessageBrokerOptions.Password)],
        };
    }
}
