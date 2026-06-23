using FreshCart.Reporting.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace FreshCart.Reporting.Infrastructure.Persistence.Warehouse;

/// <summary>
/// EF Core context for the reporting warehouse. Holds the <c>InvoiceNumberSequence</c> table that
/// supplies gap-free numbering, the <c>Invoices</c> table for issued invoices and the
/// <c>ProjectionInbox</c> table used by consumers for idempotency.
/// </summary>
/// <remarks>
/// Read-side dashboard queries run via Dapper directly against the same database; EF Core is
/// only used here for the writes that actually need a transactional unit of work.
/// </remarks>
public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
{
    public DbSet<InvoiceRecord> Invoices => Set<InvoiceRecord>();

    public DbSet<InvoiceLineRecord> InvoiceLines => Set<InvoiceLineRecord>();

    public DbSet<InvoiceNumberSequence> InvoiceNumberSequences => Set<InvoiceNumberSequence>();

    public DbSet<ProjectionInboxEntry> ProjectionInbox => Set<ProjectionInboxEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<InvoiceRecord>(builder =>
        {
            builder.ToTable("invoices");
            builder.HasKey(record => record.Id);
            builder.Property(record => record.InvoiceNumber).HasMaxLength(32).IsRequired();
            builder.HasIndex(record => record.InvoiceNumber).IsUnique();
            builder.HasIndex(record => record.OrderId).IsUnique();
            builder.Property(record => record.CustomerEmail).HasMaxLength(256).IsRequired();
            builder.Property(record => record.CustomerDisplayName).HasMaxLength(128).IsRequired();
            builder.Property(record => record.CurrencyCode).HasMaxLength(3).IsRequired();
            builder.OwnsOne(record => record.BillingAddress, addressBuilder =>
            {
                addressBuilder.Property(address => address.FullName).HasColumnName("billing_full_name").HasMaxLength(128);
                addressBuilder.Property(address => address.AddressLine1).HasColumnName("billing_address_line_1").HasMaxLength(256);
                addressBuilder.Property(address => address.AddressLine2).HasColumnName("billing_address_line_2").HasMaxLength(256);
                addressBuilder.Property(address => address.City).HasColumnName("billing_city").HasMaxLength(128);
                addressBuilder.Property(address => address.State).HasColumnName("billing_state").HasMaxLength(64);
                addressBuilder.Property(address => address.PostalCode).HasColumnName("billing_postal_code").HasMaxLength(32);
                addressBuilder.Property(address => address.Country).HasColumnName("billing_country").HasMaxLength(64);
            });
            builder.OwnsOne(record => record.ShippingAddress, addressBuilder =>
            {
                addressBuilder.Property(address => address.FullName).HasColumnName("shipping_full_name").HasMaxLength(128);
                addressBuilder.Property(address => address.AddressLine1).HasColumnName("shipping_address_line_1").HasMaxLength(256);
                addressBuilder.Property(address => address.AddressLine2).HasColumnName("shipping_address_line_2").HasMaxLength(256);
                addressBuilder.Property(address => address.City).HasColumnName("shipping_city").HasMaxLength(128);
                addressBuilder.Property(address => address.State).HasColumnName("shipping_state").HasMaxLength(64);
                addressBuilder.Property(address => address.PostalCode).HasColumnName("shipping_postal_code").HasMaxLength(32);
                addressBuilder.Property(address => address.Country).HasColumnName("shipping_country").HasMaxLength(64);
            });
            builder.HasMany(record => record.Lines)
                .WithOne()
                .HasForeignKey(line => line.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceLineRecord>(builder =>
        {
            builder.ToTable("invoice_lines");
            builder.HasKey(line => new { line.InvoiceId, line.LineNumber });
            builder.Property(line => line.ProductSku).HasMaxLength(64).IsRequired();
            builder.Property(line => line.ProductName).HasMaxLength(256).IsRequired();
        });

        modelBuilder.Entity<InvoiceNumberSequence>(builder =>
        {
            builder.ToTable("invoice_number_sequences");
            builder.HasKey(sequence => new { sequence.Year, sequence.Kind });
            builder.Property(sequence => sequence.LastSequence);
        });

        modelBuilder.Entity<ProjectionInboxEntry>(builder =>
        {
            builder.ToTable("projection_inbox");
            builder.HasKey(entry => entry.EventId);
            builder.Property(entry => entry.ProcessedOnUtc).IsRequired();
        });
    }
}
