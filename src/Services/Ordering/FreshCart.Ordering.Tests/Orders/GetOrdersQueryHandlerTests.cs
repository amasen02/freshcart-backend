using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Orders.Dtos;
using FreshCart.Ordering.Application.Orders.Queries.GetOrders;
using NSubstitute;

namespace FreshCart.Ordering.Tests.Orders;

public sealed class GetOrdersQueryHandlerTests
{
    private readonly IOrderReadQueries readQueries = Substitute.For<IOrderReadQueries>();
    private readonly GetOrdersQueryHandler handler;

    public GetOrdersQueryHandlerTests() => handler = new GetOrdersQueryHandler(readQueries);

    [Fact]
    public async Task QueriesTheCallersOwnOrdersWhenNoFilterIsSupplied()
    {
        var customerId = Guid.NewGuid();
        StubEmptyPage(customerId);

        await handler.Handle(
            new GetOrdersQuery(customerId, IsAdministrator: false, CustomerIdFilter: null, new PaginationRequest()),
            CancellationToken.None);

        await readQueries.Received(1).GetOrderSummariesPageAsync(
            customerId,
            Arg.Any<PaginationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AllowsAnAdministratorToQueryAnotherCustomersOrders()
    {
        var administratorId = Guid.NewGuid();
        var targetCustomerId = Guid.NewGuid();
        StubEmptyPage(targetCustomerId);

        await handler.Handle(
            new GetOrdersQuery(administratorId, IsAdministrator: true, targetCustomerId, new PaginationRequest()),
            CancellationToken.None);

        await readQueries.Received(1).GetOrderSummariesPageAsync(
            targetCustomerId,
            Arg.Any<PaginationRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ThrowsForbiddenWhenANonAdministratorFiltersByAnotherCustomer()
    {
        var customerId = Guid.NewGuid();

        var act = () => handler.Handle(
            new GetOrdersQuery(customerId, IsAdministrator: false, Guid.NewGuid(), new PaginationRequest()),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await readQueries.DidNotReceive().GetOrderSummariesPageAsync(
            Arg.Any<Guid>(),
            Arg.Any<PaginationRequest>(),
            Arg.Any<CancellationToken>());
    }

    private void StubEmptyPage(Guid customerId) =>
        readQueries
            .GetOrderSummariesPageAsync(customerId, Arg.Any<PaginationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PaginatedResult<OrderSummaryDto>(1, 20, 0, []));
}
