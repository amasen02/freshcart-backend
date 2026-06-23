using System.Text.Json;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Payment.Domain.Events;

namespace FreshCart.Payment.Infrastructure.EventStore;

/// <summary>
/// Maps payment event records to and from their stored JSON payloads. The event type name is the
/// discriminator persisted next to the payload; an unknown name means the store and the code have
/// drifted apart, which is unrecoverable for that stream.
/// </summary>
public static class PaymentEventSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptions.Default;

    private static readonly Dictionary<string, Type> EventTypesByName =
        new(StringComparer.Ordinal)
        {
            [nameof(PaymentInitiated)] = typeof(PaymentInitiated),
            [nameof(PaymentAuthorized)] = typeof(PaymentAuthorized),
            [nameof(PaymentCaptured)] = typeof(PaymentCaptured),
            [nameof(PaymentDeclined)] = typeof(PaymentDeclined),
            [nameof(PaymentRefunded)] = typeof(PaymentRefunded),
        };

    public static string GetEventTypeName(IPaymentEvent paymentEvent)
    {
        ArgumentNullException.ThrowIfNull(paymentEvent);

        return paymentEvent.GetType().Name;
    }

    public static string Serialize(IPaymentEvent paymentEvent)
    {
        ArgumentNullException.ThrowIfNull(paymentEvent);

        return JsonSerializer.Serialize(paymentEvent, paymentEvent.GetType(), SerializerOptions);
    }

    public static IPaymentEvent Deserialize(string eventTypeName, string payloadJson)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        if (!EventTypesByName.TryGetValue(eventTypeName, out var eventType))
        {
            throw new InternalServerException($"Stored payment event type \"{eventTypeName}\" is unknown.");
        }

        return JsonSerializer.Deserialize(payloadJson, eventType, SerializerOptions) as IPaymentEvent
            ?? throw new InternalServerException($"Stored payment event of type \"{eventTypeName}\" could not be deserialized.");
    }
}
