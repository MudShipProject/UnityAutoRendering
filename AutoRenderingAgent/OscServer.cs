using System.Net;
using System.Net.Sockets;
using CoreOSC;
using CoreOSC.Types;

namespace AutoRenderingAgent;

public class OscServer
{
    private UdpClient _udpClient = null!;

    public event Action<OscMessage, IPEndPoint>? OnMessageReceived;

    public void Start(int port)
    {
        _udpClient = new UdpClient(port);
    }

    public async Task ListenAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(ct);
                var msg = ParseMessage(result.Buffer);
                if (msg is { } message)
                    OnMessageReceived?.Invoke(message, result.RemoteEndPoint);
            }
        }
        catch (OperationCanceledException) { }
    }

    public async Task SendAsync(IPEndPoint target, string address, params object[] args)
    {
        try
        {
            var message = new OscMessage(new Address(address), args);
            var converter = new OscMessageConverter();
            var dwords = converter.Serialize(message).ToArray();
            var bytes = dwords.SelectMany(d => d.Bytes).ToArray();
            await _udpClient.SendAsync(bytes, bytes.Length, target);
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to send OSC: {ex.Message}");
        }
    }

    private static OscMessage? ParseMessage(byte[] data)
    {
        try
        {
            var padded = data;
            if (data.Length % 4 != 0)
            {
                padded = new byte[(data.Length + 3) / 4 * 4];
                Array.Copy(data, padded, data.Length);
            }

            var dwords = new List<DWord>();
            for (var i = 0; i < padded.Length; i += 4)
                dwords.Add(new DWord(padded[i], padded[i + 1], padded[i + 2], padded[i + 3]));

            var converter = new OscMessageConverter();
            converter.Deserialize(dwords, out var msg);
            return msg;
        }
        catch
        {
            return null;
        }
    }
}
