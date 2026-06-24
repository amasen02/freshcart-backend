using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace FreshCart.Ordering.Infrastructure.Inventory;

/// <summary>
/// Development-only TLS validation for the internal Ordering-to-Inventory gRPC hop. Inventory presents
/// the ASP.NET Core HTTPS development certificate, whose self-signed root is absent from the machine
/// trust store on a fresh checkout (and <c>dotnet dev-certs --trust</c> needs an interactive prompt).
/// Accepts only that certificate — identified by the ASP.NET Core development-certificate OID — and only
/// when the sole chain problem is the untrusted root. (Mirrors the helper in ServiceDefaults; kept local
/// because Infrastructure must not reference the hosting/observability bootstrap.) Production validates
/// the full chain against real certificates.
/// </summary>
internal static class DevelopmentCertificateValidation
{
    private const string AspNetCoreHttpsDevelopmentCertificateOid = "1.3.6.1.4.1.311.84.1.1";

    public static bool AcceptAspNetCoreDevelopmentCertificate(
        object sender,
        X509Certificate? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (sslPolicyErrors != SslPolicyErrors.RemoteCertificateChainErrors
            || certificate is not X509Certificate2 inspectedCertificate)
        {
            return false;
        }

        var onlyUntrustedRoot = chain is null || chain.ChainStatus.All(status =>
            status.Status is X509ChainStatusFlags.NoError or X509ChainStatusFlags.UntrustedRoot);

        return onlyUntrustedRoot && inspectedCertificate.Extensions.Any(extension =>
            string.Equals(extension.Oid?.Value, AspNetCoreHttpsDevelopmentCertificateOid, StringComparison.Ordinal));
    }
}
