using FreshCart.Inventory.Api.Protos;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;

namespace FreshCart.Ordering.Infrastructure.Inventory;

/// <summary>
/// Owns the single gRPC channel to the Inventory service. Grpc.Net.ClientFactory is not part of the
/// pinned package set, so this small factory fills the same role: one shared, lazily created channel
/// (channels multiplex requests and are safe to share) and cheap client instances on top.
/// </summary>
public sealed class InventoryGrpcChannelFactory : IDisposable
{
    public const string AddressConfigurationKey = "Services:Inventory:Address";

    private readonly Lazy<GrpcChannel> lazyChannel;

    public InventoryGrpcChannelFactory(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var inventoryAddress = configuration[AddressConfigurationKey]
            ?? throw new InvalidOperationException($"Configuration value \"{AddressConfigurationKey}\" is required.");

        lazyChannel = new Lazy<GrpcChannel>(
            () => GrpcChannel.ForAddress(inventoryAddress),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public InventoryService.InventoryServiceClient CreateClient() => new(lazyChannel.Value);

    public void Dispose()
    {
        if (lazyChannel.IsValueCreated)
        {
            lazyChannel.Value.Dispose();
        }
    }
}
