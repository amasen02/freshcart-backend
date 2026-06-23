using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.CustomerSupport.Api.Authentication;
using FreshCart.CustomerSupport.Api.Domain;
using FreshCart.CustomerSupport.Api.Persistence;
using FreshCart.CustomerSupport.Api.Realtime;

namespace FreshCart.CustomerSupport.Api.Endpoints;

/// <summary>
/// Read-side of the support service. Live conversation runs over the hub; these endpoints serve the
/// history a reconnecting client replays and the queues back-office staff audit. Every route scopes
/// its data to the caller: a customer sees only their own session, a participant or administrator
/// reads a transcript, back-office staff browse the full list.
/// </summary>
public sealed class SupportEndpoints : ICarterModule
{
    private const string StatusQueryParameterName = "status";

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var sessionsGroup = app.MapGroup("/support/sessions").WithTags("CustomerSupport");

        sessionsGroup.MapGet("/active", GetActiveSessionsAsync)
            .RequireAuthorization(AuthorizationPolicies.CustomerOrSupportAgent)
            .WithSummary("A customer's own open session (204 when none) or an agent's active sessions.")
            .Produces<IReadOnlyList<ChatSessionDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        sessionsGroup.MapGet("/{sessionId:guid}/messages", GetSessionMessagesAsync)
            .RequireAuthorization()
            .WithSummary("Transcript for a session, ascending by send time, paginated. Participant or administrator only.")
            .Produces<PaginatedResult<ChatMessageDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        sessionsGroup.MapGet("/", GetSessionsAsync)
            .RequireAuthorization(AuthorizationPolicies.BackOfficeUser)
            .WithSummary("All sessions, paginated and filterable by status. Back-office staff only.")
            .Produces<PaginatedResult<ChatSessionDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetActiveSessionsAsync(
        HttpContext httpContext,
        IChatSessionRepository sessionRepository,
        CancellationToken cancellationToken)
    {
        var user = httpContext.User;
        var userId = user.GetUserId();

        if (user.IsSupportAgent())
        {
            var agentSessions = await sessionRepository
                .GetActiveSessionsForAgentAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            return Results.Ok(agentSessions.Select(ChatSessionDto.FromDomain).ToList());
        }

        if (!user.IsCustomer())
        {
            throw new ForbiddenException("Only a customer or a support agent can read active sessions.");
        }

        var customerSession = await sessionRepository
            .GetOpenSessionForCustomerAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return customerSession is null
            ? Results.NoContent()
            : Results.Ok(new List<ChatSessionDto> { ChatSessionDto.FromDomain(customerSession) });
    }

    private static async Task<IResult> GetSessionMessagesAsync(
        Guid sessionId,
        HttpContext httpContext,
        [AsParameters] PaginationRequest paginationRequest,
        IChatSessionRepository sessionRepository,
        IChatMessageRepository messageRepository,
        CancellationToken cancellationToken)
    {
        var user = httpContext.User;
        var userId = user.GetUserId();

        var session = await sessionRepository.GetByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ChatSession), sessionId);

        if (!user.IsAdministrator() && !session.IsParticipant(userId))
        {
            throw new ForbiddenException("You are not a participant of this chat session.");
        }

        var messagesPage = await messageRepository
            .GetSessionMessagesAsync(sessionId, paginationRequest, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(ToDtoPage(messagesPage, ChatMessageDto.FromDomain));
    }

    private static async Task<IResult> GetSessionsAsync(
        HttpContext httpContext,
        [AsParameters] PaginationRequest paginationRequest,
        IChatSessionRepository sessionRepository,
        CancellationToken cancellationToken)
    {
        var statusFilter = ParseStatusFilter(httpContext);

        var sessionsPage = await sessionRepository
            .GetSessionsAsync(statusFilter, paginationRequest, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(ToDtoPage(sessionsPage, ChatSessionDto.FromDomain));
    }

    private static SessionStatus? ParseStatusFilter(HttpContext httpContext)
    {
        var statusValue = httpContext.Request.Query[StatusQueryParameterName].ToString();
        if (string.IsNullOrWhiteSpace(statusValue))
        {
            return null;
        }

        return Enum.TryParse<SessionStatus>(statusValue, ignoreCase: true, out var parsedStatus)
            ? parsedStatus
            : throw new BadRequestException($"\"{statusValue}\" is not a valid session status.");
    }

    private static PaginatedResult<TDto> ToDtoPage<TSource, TDto>(
        PaginatedResult<TSource> sourcePage,
        Func<TSource, TDto> projection) =>
        new(
            sourcePage.PageNumber,
            sourcePage.PageSize,
            sourcePage.TotalItemCount,
            sourcePage.Items.Select(projection).ToList());
}
