using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;

namespace FreshCart.Identity.Infrastructure.Cryptography;

/// <summary>
/// ASP.NET Identity password hasher backed by Argon2id. Replaces the default PBKDF2 because
/// Argon2id is memory-hard, which makes GPU-based offline attacks materially more expensive.
/// </summary>
/// <remarks>
/// Stored hash format: <c>argon2id$v=19$m=&lt;kib&gt;,t=&lt;iters&gt;,p=&lt;par&gt;$&lt;salt-b64&gt;$&lt;hash-b64&gt;</c>.
/// Cost parameters are stored inline so we can raise them in the future without invalidating
/// existing hashes; verification reads them back and signals
/// <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> when they are weaker than the
/// current target.
/// </remarks>
public sealed class Argon2PasswordHasher<TUser> : IPasswordHasher<TUser>
    where TUser : class
{
    private const int SaltByteLength = 16;
    private const int HashByteLength = 32;
    private const int TargetMemorySizeInKilobytes = 65_536;
    private const int TargetIterations = 3;
    private const int TargetDegreeOfParallelism = 4;
    private const string FormatPrefix = "argon2id";
    private const string FormatVersionSegment = "v=19";

    public string HashPassword(TUser user, string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = RandomNumberGenerator.GetBytes(SaltByteLength);
        var hash = ComputeHash(password, salt, TargetMemorySizeInKilobytes, TargetIterations, TargetDegreeOfParallelism, HashByteLength);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{FormatPrefix}${FormatVersionSegment}$m={TargetMemorySizeInKilobytes},t={TargetIterations},p={TargetDegreeOfParallelism}$" +
            $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    public PasswordVerificationResult VerifyHashedPassword(TUser user, string hashedPassword, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(hashedPassword);
        ArgumentNullException.ThrowIfNull(providedPassword);

        var parsed = ParsedHash.TryParse(hashedPassword);
        if (parsed is null)
        {
            return PasswordVerificationResult.Failed;
        }

        var candidate = ComputeHash(
            providedPassword,
            parsed.Salt,
            parsed.MemorySizeInKilobytes,
            parsed.Iterations,
            parsed.DegreeOfParallelism,
            parsed.HashLength);

        // FixedTimeEquals avoids timing-side-channel leakage between byte differences.
        if (!CryptographicOperations.FixedTimeEquals(candidate, parsed.Hash))
        {
            return PasswordVerificationResult.Failed;
        }

        var needsRehash =
            parsed.MemorySizeInKilobytes < TargetMemorySizeInKilobytes ||
            parsed.Iterations < TargetIterations ||
            parsed.DegreeOfParallelism < TargetDegreeOfParallelism;

        return needsRehash
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Success;
    }

    private static byte[] ComputeHash(
        string password,
        byte[] salt,
        int memorySizeInKilobytes,
        int iterations,
        int degreeOfParallelism,
        int hashLength)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            MemorySize = memorySizeInKilobytes,
            Iterations = iterations,
            DegreeOfParallelism = degreeOfParallelism,
        };

        return argon2.GetBytes(hashLength);
    }

    private sealed record ParsedHash(
        byte[] Salt,
        byte[] Hash,
        int HashLength,
        int MemorySizeInKilobytes,
        int Iterations,
        int DegreeOfParallelism)
    {
        private const int ExpectedSegmentCount = 5;
        private const int ExpectedParameterCount = 3;
        private const int MemoryParameterIndex = 0;
        private const int IterationsParameterIndex = 1;
        private const int ParallelismParameterIndex = 2;

        public static ParsedHash? TryParse(string encoded)
        {
            var segments = encoded.Split('$');
            if (segments.Length != ExpectedSegmentCount || !string.Equals(segments[0], FormatPrefix, StringComparison.Ordinal))
            {
                return null;
            }

            var parameters = segments[2].Split(',');
            if (parameters.Length != ExpectedParameterCount
                || !TryReadInt(parameters[MemoryParameterIndex],     "m=", out var memorySizeInKilobytes)
                || !TryReadInt(parameters[IterationsParameterIndex], "t=", out var iterations)
                || !TryReadInt(parameters[ParallelismParameterIndex], "p=", out var degreeOfParallelism))
            {
                return null;
            }

            try
            {
                var salt = Convert.FromBase64String(segments[3]);
                var hash = Convert.FromBase64String(segments[4]);
                return new ParsedHash(salt, hash, hash.Length, memorySizeInKilobytes, iterations, degreeOfParallelism);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        private static bool TryReadInt(string segment, string expectedPrefix, out int value)
        {
            value = 0;
            return segment.StartsWith(expectedPrefix, StringComparison.Ordinal)
                && int.TryParse(segment.AsSpan(expectedPrefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
    }
}
