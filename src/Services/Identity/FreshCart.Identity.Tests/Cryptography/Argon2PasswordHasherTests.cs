using FluentAssertions;
using FreshCart.Identity.Domain.Users;
using FreshCart.Identity.Infrastructure.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace FreshCart.Identity.Tests.Cryptography;

public sealed class Argon2PasswordHasherTests
{
    private const string SamplePassword = "Sup3rSecret!Passphrase";

    private readonly Argon2PasswordHasher<ApplicationUser> hasher = new();
    private readonly ApplicationUser sampleUser = new() { DisplayName = "Test", Email = "test@freshcart.test" };

    [Fact]
    public void HashedPasswordCarriesTheArgon2idHeader()
    {
        var hashed = hasher.HashPassword(sampleUser, SamplePassword);

        hashed.Should().StartWith("argon2id$v=19$");
    }

    [Fact]
    public void ProducedHashEncodesArgon2idParameters()
    {
        var hashed = hasher.HashPassword(sampleUser, SamplePassword);

        var segments = hashed.Split('$');
        segments.Should().HaveCount(5);
        segments[2].Should().Be("m=65536,t=3,p=4");
    }

    [Fact]
    public void VerifyReturnsSuccessForTheOriginalPassword()
    {
        var hashed = hasher.HashPassword(sampleUser, SamplePassword);

        hasher.VerifyHashedPassword(sampleUser, hashed, SamplePassword)
            .Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void VerifyReturnsFailedForAWrongPassword()
    {
        var hashed = hasher.HashPassword(sampleUser, SamplePassword);

        hasher.VerifyHashedPassword(sampleUser, hashed, "wrong-passphrase")
            .Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void VerifyReturnsFailedForAMalformedHash()
    {
        hasher.VerifyHashedPassword(sampleUser, "not-an-argon2id-string", SamplePassword)
            .Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void TwoHashesOfTheSamePasswordDifferBecauseTheSaltIsRandom()
    {
        var first = hasher.HashPassword(sampleUser, SamplePassword);
        var second = hasher.HashPassword(sampleUser, SamplePassword);

        first.Should().NotBe(second);
    }
}
