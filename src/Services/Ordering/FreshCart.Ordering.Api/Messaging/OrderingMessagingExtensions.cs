using FreshCart.BuildingBlocks.Messaging.MassTransit;
using FreshCart.Ordering.Application.Checkout;
using FreshCart.Ordering.Infrastructure.Persistence;
using MassTransit;

namespace FreshCart.Ordering.Api.Messaging;

/// <summary>
/// MassTransit wiring for the Ordering host. The shared <c>AddRabbitMqMessageBroker</c> extension
/// discovers consumers and sagas but cannot attach the EF Core saga repository, which has to be
/// configured inside <c>AddMassTransit</c>. Rather than modify the shared building block, the saga
/// state machine and its persistent repository are registered here using the identical RabbitMQ host
/// and exponential-retry settings the shared extension applies, so behaviour stays consistent across
/// services.
/// </summary>
public static class OrderingMessagingExtensions
{
    private const int RetryLimit = 5;
    private static readonly TimeSpan MinRetryInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxRetryInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan RetryIntervalDelta = TimeSpan.FromSeconds(2);

    public static IServiceCollection AddOrderingMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var brokerOptions = ReadBrokerOptions(configuration);
        var applicationAssembly = typeof(CheckoutSagaStateMachine).Assembly;

        services.AddMassTransit(massTransit =>
        {
            massTransit.SetKebabCaseEndpointNameFormatter();

            massTransit.AddConsumers(applicationAssembly);

            massTransit
                .AddSagaStateMachine<CheckoutSagaStateMachine, CheckoutState>()
                .EntityFrameworkRepository(repository =>
                {
                    repository.ConcurrencyMode = ConcurrencyMode.Optimistic;
                    repository.ExistingDbContext<OrderingDbContext>();
                });

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
                    retryConfigurator.Exponential(RetryLimit, MinRetryInterval, MaxRetryInterval, RetryIntervalDelta));

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
