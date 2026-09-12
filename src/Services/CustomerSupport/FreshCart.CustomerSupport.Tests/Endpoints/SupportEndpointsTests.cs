using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FluentAssertions;
using FreshCart.CustomerSupport.Api.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FreshCart.CustomerSupport.Tests.Endpoints;

public sealed class SupportEndpointsTests(SupportApiFactory factory) : IClassFixture<SupportApiFactory>
{
    private const string CustomerRole = "Customer";
    private const string SupportAgentRole = "SupportAgent";
    private const string AdministratorRole = "Administrator";
    private const string Topic = "Where is my order?";

    private static readonly DateTimeOffset StartedOnUtc = new(2026, 6, 26, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ActiveSessionsRejectsAnUnauthenticatedCaller()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/support/sessions/active");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ACustomerSeesTheirOwnOpenSession()
    {
        var customerId = Guid.NewGuid();
        await factory.Sessions.SaveAsync(
            ChatSession.Start(Guid.NewGuid(), Topic, customerId, "Demo Customer", StartedOnUtc),
            CancellationToken.None);

        var response = await SendAsync(HttpMethod.Get, "/support/sessions/active", customerId, CustomerRole);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ACustomerWithNoOpenSessionGetsNoContent()
    {
        var response = await SendAsync(HttpMethod.Get, "/support/sessions/active", Guid.NewGuid(), CustomerRole);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AnAgentSeesTheirActiveSessions()
    {
        var agentId = Guid.NewGuid();
        var session = ChatSession.Start(Guid.NewGuid(), Topic, Guid.NewGuid(), "Demo Customer", StartedOnUtc);
        session.AssignTo(agentId, "Demo Agent");
        await factory.Sessions.SaveAsync(session, CancellationToken.None);

        var response = await SendAsync(HttpMethod.Get, "/support/sessions/active", agentId, SupportAgentRole);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ACustomerCannotReadAnotherCustomersTranscript()
    {
        var ownerCustomerId = Guid.NewGuid();
        var session = ChatSession.Start(Guid.NewGuid(), Topic, ownerCustomerId, "Owner", StartedOnUtc);
        session.AssignTo(Guid.NewGuid(), "Demo Agent");
        await factory.Sessions.SaveAsync(session, CancellationToken.None);

        var response = await SendAsync(
            HttpMethod.Get, $"/support/sessions/{session.Id}/messages", Guid.NewGuid(), CustomerRole);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AParticipantCanReadTheirOwnTranscript()
    {
        var customerId = Guid.NewGuid();
        var session = ChatSession.Start(Guid.NewGuid(), Topic, customerId, "Owner", StartedOnUtc);
        await factory.Sessions.SaveAsync(session, CancellationToken.None);

        var response = await SendAsync(
            HttpMethod.Get, $"/support/sessions/{session.Id}/messages", customerId, CustomerRole);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnAdministratorCanReadAnyTranscriptEvenWhenNotAParticipant()
    {
        var session = ChatSession.Start(Guid.NewGuid(), Topic, Guid.NewGuid(), "Owner", StartedOnUtc);
        await factory.Sessions.SaveAsync(session, CancellationToken.None);

        var response = await SendAsync(
            HttpMethod.Get, $"/support/sessions/{session.Id}/messages", Guid.NewGuid(), AdministratorRole);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadingTheTranscriptOfAnUnknownSessionReturnsNotFound()
    {
        var response = await SendAsync(
            HttpMethod.Get, $"/support/sessions/{Guid.NewGuid()}/messages", Guid.NewGuid(), AdministratorRole);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ACustomerCannotBrowseTheFullSessionList()
    {
        var response = await SendAsync(HttpMethod.Get, "/support/sessions/", Guid.NewGuid(), CustomerRole);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BackOfficeStaffCanBrowseTheFullSessionList()
    {
        var response = await SendAsync(HttpMethod.Get, "/support/sessions/", Guid.NewGuid(), AdministratorRole);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenApiDescribesSupportEndpointsAndResponseSchemas()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await using var documentStream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(documentStream);
        var paths = document.RootElement.GetProperty("paths");
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

        AssertOperation(
            paths,
            schemas,
            "/support/sessions/active",
            "A customer's own open session (204 when none) or an agent's active sessions.",
            ["sessionId", "topic", "customerId", "customerDisplayName", "agentId", "agentDisplayName", "status", "startedOnUtc"]);
        AssertOperation(
            paths,
            schemas,
            "/support/sessions/{sessionId}/messages",
            "Transcript for a session, ascending by send time, paginated. Participant or administrator only.",
            ["messageId", "sessionId", "senderId", "senderDisplayName", "senderRole", "text", "sentOnUtc"]);
        AssertOperation(
            paths,
            schemas,
            "/support/sessions",
            "All sessions, paginated and filterable by status. Back-office staff only.",
            ["sessionId", "topic", "customerId", "customerDisplayName", "agentId", "agentDisplayName", "status", "startedOnUtc"]);
    }

    [Fact]
    public async Task DevelopmentSwaggerUiUsesSelfHostedAssetsAndKeepsOpenApiProtectedByStrictCsp()
    {
        using var client = factory.CreateClient();

        using var index = await client.GetAsync("/swagger/index.html");
        index.StatusCode.Should().Be(HttpStatusCode.OK);
        var indexHtml = await index.Content.ReadAsStringAsync();
        indexHtml.Should().Contain("./swagger-ui.css");
        indexHtml.Should().Contain("./swagger-ui-bundle.js");
        indexHtml.Should().Contain("./swagger-ui-standalone-preset.js");
        indexHtml.Should().Contain("./index.js");
        indexHtml.Should().NotContain("unsafe-inline");
        indexHtml.Should().NotContain("https://cdn.");
        GetHeader(index, "Content-Security-Policy").Should().Be(SwaggerCsp);

        foreach (var asset in new[]
        {
            "/swagger/swagger-ui.css",
            "/swagger/swagger-ui-bundle.js",
            "/swagger/swagger-ui-standalone-preset.js",
            "/swagger/index.js",
        })
        {
            using var assetResponse = await client.GetAsync(asset);
            assetResponse.StatusCode.Should().Be(HttpStatusCode.OK, asset);
        }

        using var documentResponse = await client.GetAsync("/openapi/v1.json");
        documentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertStrictSecurityHeaders(documentResponse);
    }

    [Fact]
    public async Task ProductionDoesNotExposeSwaggerUiOrOpenApiDocument()
    {
        using var productionFactory = new SupportApiFactory { EnvironmentName = "Production" };
        using var client = productionFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
        });

        using var swagger = await client.GetAsync("/swagger/index.html");
        swagger.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertStrictSecurityHeaders(swagger);

        using var document = await client.GetAsync("/openapi/v1.json");
        document.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertStrictSecurityHeaders(document);
    }

    private static void AssertOperation(
        JsonElement paths,
        JsonElement schemas,
        string path,
        string expectedSummary,
        IReadOnlyList<string> expectedItemProperties)
    {
        var operation = paths.GetProperty(path).GetProperty("get");

        operation.GetProperty("summary").GetString().Should().Be(expectedSummary);
        operation.GetProperty("tags").EnumerateArray().Select(tag => tag.GetString()).Should().Contain("CustomerSupport");

        var responseSchema = operation
            .GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("schema");

        var resolvedResponseSchema = ResolveSchema(responseSchema, schemas);
        resolvedResponseSchema.ValueKind.Should().Be(JsonValueKind.Object);

        if (resolvedResponseSchema.TryGetProperty("type", out var type) && string.Equals(type.GetString(), "array", StringComparison.Ordinal))
        {
            AssertProperties(ResolveSchema(resolvedResponseSchema.GetProperty("items"), schemas), expectedItemProperties);
            return;
        }

        AssertProperties(resolvedResponseSchema, ["pageNumber", "pageSize", "totalItemCount", "items"]);
        var items = ResolveSchema(resolvedResponseSchema.GetProperty("properties").GetProperty("items"), schemas);
        items.GetProperty("type").GetString().Should().Be("array");
        AssertProperties(ResolveSchema(items.GetProperty("items"), schemas), expectedItemProperties);
    }

    private static JsonElement ResolveSchema(JsonElement schema, JsonElement schemas)
    {
        while (schema.TryGetProperty("$ref", out var reference))
        {
            var referenceParts = reference.GetString()!.Split('/');
            var schemaName = referenceParts[^1];
            schema = schemas.GetProperty(schemaName);
        }

        return schema;
    }

    private static void AssertProperties(JsonElement schema, IReadOnlyList<string> expectedProperties)
    {
        var properties = schema.GetProperty("properties");
        foreach (var expectedProperty in expectedProperties)
        {
            properties.TryGetProperty(expectedProperty, out _).Should().BeTrue($"schema should describe {expectedProperty}");
        }
    }

    private static string GetHeader(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
        {
            return values.Single();
        }

        if (response.Content.Headers.TryGetValues(name, out values))
        {
            return values.Single();
        }

        throw new InvalidOperationException($"Missing {name} response header.");
    }

    private static void AssertStrictSecurityHeaders(HttpResponseMessage response)
    {
        GetHeader(response, "X-Content-Type-Options").Should().Be("nosniff");
        GetHeader(response, "X-Frame-Options").Should().Be("DENY");
        GetHeader(response, "Referrer-Policy").Should().Be("strict-origin-when-cross-origin");
        GetHeader(response, "Permissions-Policy").Should().Be("accelerometer=(), camera=(), geolocation=(), microphone=(), payment=()");
        GetHeader(response, "Cross-Origin-Opener-Policy").Should().Be("same-origin");
        GetHeader(response, "Cross-Origin-Embedder-Policy").Should().Be("require-corp");
        GetHeader(response, "Cross-Origin-Resource-Policy").Should().Be("same-origin");
        GetHeader(response, "Content-Security-Policy").Should().Be(StrictApiCsp);
        response.Headers.Contains("Server").Should().BeFalse();
        response.Headers.Contains("X-Powered-By").Should().BeFalse();
    }

    private const string StrictApiCsp = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";
    private const string SwaggerCsp = "default-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; font-src 'self'; frame-ancestors 'none'; base-uri 'none'; form-action 'self'";

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string uri, Guid userId, string role)
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SupportApiFactory.CreateAccessToken(userId, role));
        return await client.SendAsync(request);
    }
}
