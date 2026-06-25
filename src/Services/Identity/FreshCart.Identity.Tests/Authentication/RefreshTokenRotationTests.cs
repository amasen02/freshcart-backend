using FluentAssertions;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FreshCart.Identity.Tests.Authentication;

public sealed class RefreshTokenRotationTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    private const string TestIpAddress = "203.0.113.7";
    private const string TestUserAgent = "FreshCart-Tests/1.0";

    [Fact]
    public async Task ConcurrentRotationsOfTheSameTokenLetExactlyOneSucceed()
    {
        const int ConcurrentRefreshers = 8;
        var userId = Guid.NewGuid();
        var plaintextToken = await IssueTokenAsync(userId);

        var rotationTasks = Enumerable
            .Range(0, ConcurrentRefreshers)
            .Select(_ => Task.Run(() => TryRotateAsync(plaintextToken)));
        var outcomes = await Task.WhenAll(rotationTasks);

        outcomes.Count(succeeded => succeeded).Should().Be(1);
    }

    [Fact]
    public async Task ReusingAnAlreadyRotatedTokenIsRejected()
    {
        var userId = Guid.NewGuid();
        var plaintextToken = await IssueTokenAsync(userId);

        var firstRotation = await TryRotateAsync(plaintextToken);
        var replay = await TryRotateAsync(plaintextToken);

        firstRotation.Should().BeTrue();
        replay.Should().BeFalse();
    }

    private async Task<string> IssueTokenAsync(Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
        var issued = await refreshTokenService
            .IssueAsync(userId, TestIpAddress, TestUserAgent, CancellationToken.None)
            .ConfigureAwait(false);
        return issued.PlaintextToken;
    }

    private async Task<bool> TryRotateAsync(string plaintextToken)
    {
        using var scope = factory.Services.CreateScope();
        var refreshTokenService = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
        try
        {
            await refreshTokenService
                .RotateAsync(plaintextToken, TestIpAddress, TestUserAgent, CancellationToken.None)
                .ConfigureAwait(false);
            return true;
        }
        catch (ForbiddenException)
        {
            return false;
        }
    }
}
