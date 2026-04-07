using System.Net.Sockets;
using CoreOSC;
using CoreOSC.IO;

namespace AutoRenderingService;

public class OscManager
{
    private static OscManager? _instance;
    public static OscManager Instance => _instance ??= new OscManager();

    private OscManager() { }

    public async Task SendMessageAsync(string ip, int port, string address, IEnumerable<object>? args = null)
    {
        using var client = new UdpClient();
        client.Connect(ip, port);
        var message = args != null
            ? new OscMessage(new Address(address), args)
            : new OscMessage(new Address(address));
        await client.SendMessageAsync(message);
    }

    public async Task<OscMessage?> SendAndReceiveAsync(string ip, int port, string address, int timeoutMs = 5000)
    {
        using var client = new UdpClient();
        client.Connect(ip, port);
        var message = new OscMessage(new Address(address));
        await client.SendMessageAsync(message);

        using var cts = new CancellationTokenSource(timeoutMs);
        var response = await client.ReceiveMessageAsync().WaitAsync(cts.Token);
        return response;
    }

    public OscConnection CreateConnection(string ip, int port)
    {
        var client = new UdpClient();
        client.Connect(ip, port);
        return new OscConnection(client);
    }
}

public class OscConnection : IDisposable
{
    private readonly UdpClient _client;

    public OscConnection(UdpClient client)
    {
        _client = client;
    }

    public async Task SendAsync(string address)
    {
        var message = new OscMessage(new Address(address));
        await _client.SendMessageAsync(message);
    }

    public async Task SendAsync(string address, IEnumerable<object> args)
    {
        var message = new OscMessage(new Address(address), args);
        await _client.SendMessageAsync(message);
    }

    public async Task ListenAsync(Action<OscMessage> onMessage, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var msg = await _client.ReceiveMessageAsync();
                if (ct.IsCancellationRequested) break;
                onMessage(msg);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (System.Net.Sockets.SocketException) { }
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
