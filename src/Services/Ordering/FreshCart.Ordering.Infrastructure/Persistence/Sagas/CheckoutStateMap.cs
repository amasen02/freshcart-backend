using FreshCart.Ordering.Application.Checkout;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshCart.Ordering.Infrastructure.Persistence.Sagas;

/// <summary>
/// EF Core mapping for the MassTransit saga instance. <c>RowVersion</c> is the optimistic
/// concurrency token: two saga events for the same order racing on the same row force one to retry
/// rather than silently clobbering the other.
/// </summary>
public sealed class CheckoutStateMap : SagaClassMap<CheckoutState>
{
    protected override void Configure(EntityTypeBuilder<CheckoutState> entity, ModelBuilder model)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.ToTable("CheckoutSagaState", OrderingSchema.Name);

        entity.Property(saga => saga.CurrentState).HasMaxLength(OrderingFieldLengths.SagaState).IsRequired();
        entity.Property(saga => saga.CustomerId);
        entity.Property(saga => saga.ReservationId);
        entity.Property(saga => saga.PaymentId);
        entity.Property(saga => saga.RowVersion).IsRowVersion();
    }
}
