using System.Net;
using CoreOSC;

namespace AutoRenderingAgent;

static class Program
{
    static async Task Main(string[] args)
    {
        var config = AgentConfig.Load();
        StartupManager.Register();

        LogManager.Log($"Agent started. Listening on {config.IpAddress}:{config.Port}");
        LogManager.Log($"Unity project: {(config.UnityProjectPath is { Length: > 0 } p ? p : "(not set)")}");

        var oscServer = new OscServer();
        var processManager = new UnityProcessManager();
        var renderQueue = new RenderQueue(oscServer, processManager, config);

        oscServer.OnMessageReceived += (msg, sender) =>
        {
            LogManager.Log($"Received: {msg.Address.Value} from {sender}");
            HandleMessage(msg, sender, renderQueue, processManager);
        };

        oscServer.Start(config.Port);

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        await oscServer.ListenAsync(cts.Token);
        LogManager.Log("Agent shutting down.");
    }

    private static void HandleMessage(OscMessage message, IPEndPoint sender,
        RenderQueue renderQueue, UnityProcessManager processManager)
    {
        switch (message.Address.Value)
        {
            case "/render/start":
                LogManager.Log("Render start requested.");
                var scenes = message.Arguments
                    .Select(a => a?.ToString() ?? "")
                    .Where(s => s.Length > 0)
                    .ToList();
                foreach (var s in scenes)
                    LogManager.Log($"  Scene: {s}");
                _ = Task.Run(() => renderQueue.ProcessAll(scenes, sender));
                break;

            case "/render/stop":
                LogManager.Log("Render stop requested.");
                processManager.Kill();
                break;

            case "/scenes/list":
                LogManager.Log("Scene list requested.");
                _ = Task.Run(() => renderQueue.SendSceneList(sender));
                break;

            case "/ping":
                LogManager.Log("Ping received.");
                break;

            default:
                LogManager.Log($"Unknown address: {message.Address.Value}");
                break;
        }
    }
}
