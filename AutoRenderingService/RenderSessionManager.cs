using CoreOSC;

namespace AutoRenderingService;

public class RenderSessionManager
{
    private static RenderSessionManager? _instance;
    public static RenderSessionManager Instance => _instance ??= new RenderSessionManager();

    private readonly List<OscConnection> _connections = new();
    private CancellationTokenSource? _cts;
    private int _activeCount;

    public bool IsRecording { get; private set; }

    public event Action? OnRecordingStarted;
    public event Action? OnRecordingStopped;
    public event Action<string>? OnLog;

    private RenderSessionManager() { }

    public async Task StartRenderAsync(List<(AgentEntry entry, List<string> scenes)> targets)
    {
        if (IsRecording) return;

        _cts = new CancellationTokenSource();
        _activeCount = 0;
        var sent = 0;

        foreach (var (entry, scenes) in targets)
        {
            try
            {
                var connection = OscManager.Instance.CreateConnection(entry.IpAddress, entry.Port);
                await connection.SendAsync("/render/start", scenes.Cast<object>());

                _connections.Add(connection);
                _activeCount++;
                sent++;
                OnLog?.Invoke($"Sent to {entry} -> {string.Join(", ", scenes)}");

                _ = connection.ListenAsync(msg => HandleCallback(msg), _cts.Token);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed: {entry} -> {ex.Message}");
            }
        }

        if (sent > 0)
        {
            IsRecording = true;
            OnRecordingStarted?.Invoke();
        }
        else
        {
            OnLog?.Invoke("No endpoints with checked scenes.");
        }
    }

    public async Task StopRenderAsync()
    {
        if (!IsRecording) return;

        foreach (var connection in _connections)
        {
            try
            {
                await connection.SendAsync("/render/stop");
                OnLog?.Invoke("Stop requested.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed to send stop: {ex.Message}");
            }
        }
    }

    private void HandleCallback(OscMessage msg)
    {
        switch (msg.Address.Value)
        {
            case "/render/started":
                var scene = msg.Arguments.FirstOrDefault()?.ToString() ?? "";
                OnLog?.Invoke($"Rendering: {scene}");
                break;

            case "/render/stopped":
                var stoppedScene = msg.Arguments.FirstOrDefault()?.ToString() ?? "";
                OnLog?.Invoke($"Stopped: {stoppedScene}");
                break;

            case "/render/finished":
                _activeCount--;
                OnLog?.Invoke("Render queue finished.");
                if (_activeCount <= 0)
                    Reset();
                break;
        }
    }

    private void Reset()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        foreach (var c in _connections)
            c.Dispose();
        _connections.Clear();
        _activeCount = 0;

        IsRecording = false;
        OnRecordingStopped?.Invoke();
    }
}
