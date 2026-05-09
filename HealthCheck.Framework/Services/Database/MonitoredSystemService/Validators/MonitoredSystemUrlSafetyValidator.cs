using System.Net;
using System.Net.Sockets;

namespace HealthCheck.Framework.Services.Database.MonitoredSystemService.Validators;

public static class MonitoredSystemUrlSafetyValidator
{
    public static async Task<bool> IsAllowedAsync(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return true;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (uri.IsLoopback)
            return false;

        if (IPAddress.TryParse(uri.Host, out var ipAddress))
            return !IsBlockedIp(ipAddress);

        IPAddress[] addresses;

        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host);
        }
        catch
        {
            return false;
        }

        if (addresses.Length == 0)
            return false;

        return !addresses.Any(IsBlockedIp);
    }

    private static bool IsBlockedIp(IPAddress ipAddress)
    {
        if (IPAddress.IsLoopback(ipAddress))
            return true;

        if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ipAddress.GetAddressBytes();

            return bytes[0] == 10 ||
                   bytes[0] == 127 ||
                   (bytes[0] == 169 && bytes[1] == 254) ||
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                   (bytes[0] == 192 && bytes[1] == 168);
        }

        if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ipAddress.IsIPv6LinkLocal || ipAddress.IsIPv6SiteLocal)
                return true;

            var bytes = ipAddress.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
    }
}
