using System.Globalization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace FreshCart.Delivery.Tests.Infrastructure;

/// <summary>
/// Builds and initiates a throwaway single-node MongoDB replica set for the integration tests. A replica
/// set (not a standalone) is required because the delivery write and its outbox message commit in one
/// multi-document transaction, and a standalone mongod rejects transactions. The default Testcontainers
/// MongoDB module starts a standalone, so the container is configured directly: <c>--replSet</c> plus a
/// one-shot <c>rs.initiate</c>, and clients connect with <c>directConnection=true</c> so the driver talks
/// to the seed without resolving the replica set's advertised member host.
/// </summary>
internal static class MongoReplicaSetContainer
{
    private const string MongoImage = "mongo:7.0";
    private const string ReplicaSetName = "rs0";
    private const int MongoPort = 27017;

    public static IContainer Build() =>
        new ContainerBuilder()
            .WithImage(MongoImage)
            .WithCommand("--replSet", ReplicaSetName, "--bind_ip_all")
            .WithPortBinding(MongoPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Waiting for connections"))
            .Build();

    public static async Task<string> InitiateAsync(IContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var initiate = await container.ExecAsync(
        [
            "mongosh", "--quiet", "--eval",
            $"rs.initiate({{_id:'{ReplicaSetName}',members:[{{_id:0,host:'127.0.0.1:{MongoPort}'}}]}})",
        ]);
        if (initiate.ExitCode != 0)
        {
            throw new InvalidOperationException($"rs.initiate failed (exit {initiate.ExitCode}): {initiate.Stderr}");
        }

        var awaitPrimary = await container.ExecAsync(
        [
            "mongosh", "--quiet", "--eval",
            "var attempts=0; while(!db.hello().isWritablePrimary && attempts<100){sleep(100);attempts++;} if(!db.hello().isWritablePrimary){quit(1);}",
        ]);
        if (awaitPrimary.ExitCode != 0)
        {
            throw new InvalidOperationException("The MongoDB replica set did not elect a primary in time.");
        }

        var mappedPort = container.GetMappedPublicPort(MongoPort);
        return string.Create(CultureInfo.InvariantCulture, $"mongodb://127.0.0.1:{mappedPort}/?directConnection=true");
    }
}
