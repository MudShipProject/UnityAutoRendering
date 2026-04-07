using System.Net;

namespace AutoRenderingAgent;

public class RenderQueue
{
    private readonly OscServer _oscServer;
    private readonly UnityProcessManager _processManager;
    private readonly AgentConfig _config;

    public RenderQueue(OscServer oscServer, UnityProcessManager processManager, AgentConfig config)
    {
        _oscServer = oscServer;
        _processManager = processManager;
        _config = config;
    }

    public void ProcessAll(List<string> scenes, IPEndPoint sender)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.UnityProjectPath))
            {
                LogManager.Log("Error: UnityProjectPath is not set in agent-config.json");
                return;
            }

            if (!Directory.Exists(_config.UnityProjectPath))
            {
                LogManager.Log($"Error: Unity project not found: {_config.UnityProjectPath}");
                return;
            }

            if (scenes.Count == 0)
            {
                LogManager.Log("Error: No scenes specified.");
                return;
            }

            var unityExe = UnityProcessManager.FindUnityEditor(_config.UnityProjectPath);
            if (unityExe == null)
            {
                LogManager.Log("Error: Could not find Unity Editor.");
                return;
            }

            LogManager.Log($"Starting render queue: {scenes.Count} scene(s), Take: {_config.Take}");

            for (var i = 0; i < scenes.Count; i++)
            {
                var sceneName = scenes[i];
                var take = _config.Take;
                LogManager.Log($"[{i + 1}/{scenes.Count}] Rendering: {sceneName} (Take {take})");

                _oscServer.SendAsync(sender, "/render/started", sceneName).Wait();

                var exitCode = _processManager.LaunchAndWait(unityExe, _config.UnityProjectPath, sceneName, take);

                if (exitCode != 0)
                {
                    LogManager.Log($"[{i + 1}/{scenes.Count}] Unity exited with code {exitCode}.");
                    _oscServer.SendAsync(sender, "/render/stopped", sceneName).Wait();

                    if (exitCode == -1)
                        return;

                    continue;
                }

                LogManager.Log($"[{i + 1}/{scenes.Count}] Completed: {sceneName}");

                _config.Take++;
                _config.Save();
            }

            LogManager.Log("All scenes rendered successfully.");
        }
        finally
        {
            _oscServer.SendAsync(sender, "/render/finished").Wait();
        }
    }

    public void SendSceneList(IPEndPoint target)
    {
        if (string.IsNullOrEmpty(_config.UnityProjectPath))
        {
            _oscServer.SendAsync(target, "/scenes/result").Wait();
            return;
        }

        var assetsDir = Path.Combine(_config.UnityProjectPath, "Assets");
        if (!Directory.Exists(assetsDir))
        {
            _oscServer.SendAsync(target, "/scenes/result").Wait();
            return;
        }

        var sceneFiles = Directory.GetFiles(assetsDir, "*.unity", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<object>()
            .ToArray();

        LogManager.Log($"Sending {sceneFiles.Length} scene(s) to {target}");
        _oscServer.SendAsync(target, "/scenes/result", sceneFiles).Wait();
    }
}
