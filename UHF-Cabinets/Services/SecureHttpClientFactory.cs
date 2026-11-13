using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using UHFCabinets.Data.Configuration;

namespace UHFCabinets.Services;

public static class SecureHttpClientFactory
{
    private static SocketsHttpHandler CreateHandler(AppSettings settings)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(30),
            AutomaticDecompression = DecompressionMethods.All
        };

        if (settings.AllowInsecureTls)
        {
            handler.SslOptions.RemoteCertificateValidationCallback =
                static (_, _, _, _) => true;
        }

        return handler;
    }

    public static HttpClient CreateApiClient(AppSettings settings)
    {
        var client = new HttpClient(CreateHandler(settings), disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(45)
        };
        return client;
    }

    /// <summary>Separate client for the local hardware bridge (no cabinet API headers).</summary>
    public static HttpClient CreateHardwareBridgeClient(AppSettings settings, string bridgeBaseUrl)
    {
        var trimmed = bridgeBaseUrl.TrimEnd('/');
        var baseUri = new Uri(trimmed + "/", UriKind.Absolute);
        var client = new HttpClient(CreateHandler(settings), disposeHandler: true)
        {
            BaseAddress = baseUri,
            Timeout = TimeSpan.FromMinutes(3)
        };
        return client;
    }
}
