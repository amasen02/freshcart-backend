using System.Globalization;

namespace FreshCart.CustomerSupport.Api.Realtime;

/// <summary>
/// Builds the per-user SignalR group name. Reusing the <c>user:{userId}</c> convention from
/// Notification means a user reaches their messages from any of their open connections (tabs,
/// devices) without the server tracking individual connection ids.
/// </summary>
public static class SupportGroupNames
{
    public static string ForUser(Guid userId) =>
        string.Create(CultureInfo.InvariantCulture, $"user:{userId}");
}
