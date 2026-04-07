using System.Diagnostics;

namespace AutoRenderingAgent;

public class UnityProcessManager
{
    private Process? _currentProcess;
    private readonly object _lock = new();

    public int LaunchAndWait(string unityExe, string projectPath, string sceneName, int take)
    {
        var arguments = $"-projectPath \"{projectPath}\" " +
                        $"-executeMethod UnityAutoRendering.RenderBootstrap.Run " +
                        $"-scene \"{sceneName}\" " +
                        $"-take {take}";

        LogManager.Log($"Launching Unity: {unityExe}");
        LogManager.Log($"  Scene: {sceneName}, Take: {take}");

        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = unityExe,
                Arguments = arguments,
                UseShellExecute = false,
            });

            if (process == null)
            {
                LogManager.Log("Error: Failed to start Unity process.");
                return -1;
            }

            lock (_lock)
            {
                _currentProcess = process;
            }

            LogManager.Log($"Unity started (PID: {process.Id}). Waiting for exit...");
            process.WaitForExit();

            lock (_lock)
            {
                _currentProcess = null;
            }

            LogManager.Log($"Unity exited (code: {process.ExitCode})");
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to launch Unity: {ex.Message}");
            return -1;
        }
    }

    public void Kill()
    {
        lock (_lock)
        {
            if (_currentProcess is { HasExited: false } process)
            {
                LogManager.Log($"Killing Unity process (PID: {process.Id})...");
                process.Kill();
            }
        }
    }

    public static string? FindUnityEditor(string projectPath)
    {
        var versionFile = Path.Combine(projectPath, "ProjectSettings", "ProjectVersion.txt");
        if (File.Exists(versionFile))
        {
            var lines = File.ReadAllLines(versionFile);
            foreach (var line in lines)
            {
                if (!line.StartsWith("m_EditorVersion:")) continue;

                var version = line.Split(':')[1].Trim();
                var editorPath = Path.Combine(
                    @"C:\Program Files\Unity\Hub\Editor", version, "Editor", "Unity.exe");

                if (File.Exists(editorPath))
                {
                    LogManager.Log($"Found Unity {version}");
                    return editorPath;
                }

                LogManager.Log($"Unity {version} not found at: {editorPath}");
            }
        }

        var hubEditorRoot = @"C:\Program Files\Unity\Hub\Editor";
        if (Directory.Exists(hubEditorRoot))
        {
            var dirs = Directory.GetDirectories(hubEditorRoot)
                .OrderByDescending(d => d)
                .ToArray();

            foreach (var dir in dirs)
            {
                var exe = Path.Combine(dir, "Editor", "Unity.exe");
                if (File.Exists(exe))
                {
                    LogManager.Log($"Fallback: using {Path.GetFileName(dir)}");
                    return exe;
                }
            }
        }

        return null;
    }
}
