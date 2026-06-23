using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Payment.Application.Payments.Models;

namespace FreshCart.Payment.Application.Payments.Queries.GetPaymentByOrderId;

public sealed record GetPaymentByOrderIdQuery(Guid OrderId) : IQuery<PaymentReadModel>;
