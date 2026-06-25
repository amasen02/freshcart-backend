namespace FreshCart.BuildingBlocks.Messaging.Events;

/// <summary>
/// Resolves the CLR type for a persisted integration-event contract name (see
/// <see cref="IntegrationEvent.EventType"/>). Because the contract name is the version-independent
/// <see cref="System.Type.FullName"/>, resolution must not depend on the assembly version that wrote the
/// message. The loaded-assembly scan also frees the lookup from depending on which assembly defines the
/// event or hosts the publisher; <see cref="System.Type.GetType(string)"/> alone only searches the
/// calling assembly and corelib.
/// </summary>
public static class EventContractTypeResolver
{
    public static Type? Resolve(string eventContractName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventContractName);

        var directMatch = Type.GetType(eventContractName, throwOnError: false);
        if (directMatch is not null)
        {
            return directMatch;
        }

        foreach (var loadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var assemblyMatch = loadedAssembly.GetType(eventContractName, throwOnError: false);
            if (assemblyMatch is not null)
            {
                return assemblyMatch;
            }
        }

        return null;
    }
}
