using System.Linq.Expressions;
using FreshCart.Ordering.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FreshCart.Ordering.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Order aggregate. Money totals and both addresses are owned types stored as columns on
/// the order row; order lines are an owned collection in their own table. The line collection is
/// mapped through the aggregate's backing field so callers can only mutate it through the aggregate.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    private const string LinesBackingField = "_lines";

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Orders", OrderingSchema.Name);

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.CustomerEmail).HasMaxLength(OrderingFieldLengths.Email).IsRequired();
        builder.Property(order => order.CustomerDisplayName).HasMaxLength(OrderingFieldLengths.DisplayName).IsRequired();
        builder.Property(order => order.PaymentMethod).HasMaxLength(OrderingFieldLengths.PaymentMethod).IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(OrderingFieldLengths.SagaState)
            .IsRequired();

        builder.Property(order => order.ReservationId);
        builder.Property(order => order.PaymentId);
        builder.Property(order => order.FailureReason).HasMaxLength(OrderingFieldLengths.FailureReason);

        builder.Property(order => order.SubmittedOnUtc).IsRequired();
        builder.Property(order => order.ConfirmedOnUtc);
        builder.Property(order => order.CancelledOnUtc);

        MapMoney(builder, order => order.Subtotal, nameof(Order.Subtotal));
        MapMoney(builder, order => order.DiscountTotal, nameof(Order.DiscountTotal));
        MapMoney(builder, order => order.TaxTotal, nameof(Order.TaxTotal));
        MapMoney(builder, order => order.ShippingTotal, nameof(Order.ShippingTotal));
        MapMoney(builder, order => order.GrandTotal, nameof(Order.GrandTotal));

        builder.OwnsOne(order => order.BillingAddress, MapAddress);
        builder.OwnsOne(order => order.ShippingAddress, MapAddress);

        builder.OwnsMany(order => order.Lines, MapLine);
        builder.Navigation(order => order.Lines)
            .HasField(LinesBackingField)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void MapMoney(
        EntityTypeBuilder<Order> builder,
        Expression<Func<Order, Money?>> moneyProperty,
        string columnPrefix)
    {
        builder.OwnsOne(moneyProperty, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName($"{columnPrefix}Amount")
                .HasPrecision(MoneyPrecision.TotalDigits, MoneyPrecision.DecimalDigits)
                .IsRequired();

            money.Property(value => value.CurrencyCode)
                .HasColumnName($"{columnPrefix}Currency")
                .HasMaxLength(OrderingFieldLengths.CurrencyCode)
                .IsRequired();
        });
    }

    private static void MapAddress(OwnedNavigationBuilder<Order, Address> address)
    {
        address.Property(value => value.Line1).HasMaxLength(OrderingFieldLengths.AddressLine).IsRequired();
        address.Property(value => value.Line2).HasMaxLength(OrderingFieldLengths.AddressLine);
        address.Property(value => value.City).HasMaxLength(OrderingFieldLengths.City).IsRequired();
        address.Property(value => value.PostalCode).HasMaxLength(OrderingFieldLengths.PostalCode).IsRequired();
        address.Property(value => value.CountryCode).HasMaxLength(OrderingFieldLengths.CountryCode).IsRequired();
    }

    private static void MapLine(OwnedNavigationBuilder<Order, OrderLine> line)
    {
        line.ToTable("OrderLines", OrderingSchema.Name);

        line.WithOwner().HasForeignKey("OrderId");
        line.HasKey("OrderId", nameof(OrderLine.ProductId));

        line.Property(orderLine => orderLine.ProductId).IsRequired();
        line.Property(orderLine => orderLine.ProductSku).HasMaxLength(OrderingFieldLengths.Sku).IsRequired();
        line.Property(orderLine => orderLine.ProductName).HasMaxLength(OrderingFieldLengths.ProductName).IsRequired();
        line.Property(orderLine => orderLine.PrimaryCategory).HasMaxLength(OrderingFieldLengths.Category).IsRequired();
        line.Property(orderLine => orderLine.Quantity).IsRequired();
        line.Property(orderLine => orderLine.IsDigital).IsRequired();
        line.Ignore(orderLine => orderLine.LineTotal);

        line.OwnsOne(orderLine => orderLine.UnitPrice, money =>
        {
            money.Property(value => value.Amount)
                .HasColumnName("UnitPriceAmount")
                .HasPrecision(MoneyPrecision.TotalDigits, MoneyPrecision.DecimalDigits)
                .IsRequired();

            money.Property(value => value.CurrencyCode)
                .HasColumnName("UnitPriceCurrency")
                .HasMaxLength(OrderingFieldLengths.CurrencyCode)
                .IsRequired();
        });
    }
}
