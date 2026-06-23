namespace FreshCart.Reporting.Domain.Kpis;

/// <summary>
/// Canonical KPI codes used as stable identifiers across DB, API and dashboard. Never rely on
/// the display name because it is localised.
/// </summary>
public static class KpiCodes
{
    public const string GrossMerchandiseValue = "kpi.sales.gmv";
    public const string NetRevenue            = "kpi.sales.net-revenue";
    public const string OrderCount            = "kpi.sales.order-count";
    public const string AverageOrderValue     = "kpi.sales.aov";
    public const string RefundRate            = "kpi.sales.refund-rate";
    public const string NewCustomerCount      = "kpi.customers.new";
    public const string ReturningCustomerRate = "kpi.customers.returning-rate";
    public const string CartAbandonmentRate   = "kpi.basket.abandonment-rate";
    public const string ConversionRate        = "kpi.funnel.conversion-rate";
    public const string DeliverySuccessRate   = "kpi.delivery.success-rate";
    public const string AverageDeliveryTime   = "kpi.delivery.avg-time-minutes";
    public const string OutOfStockCount       = "kpi.inventory.out-of-stock";
    public const string LowStockCount         = "kpi.inventory.low-stock";
    public const string SupportTicketBacklog  = "kpi.support.ticket-backlog";
    public const string SupportResolutionMinutes = "kpi.support.resolution-minutes";
}
