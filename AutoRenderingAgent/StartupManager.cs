using Microsoft.Win32;

namespace AutoRenderingAgent;

public static class StartupManager
{
    private const string AppName = "AutoRenderingAgent";

    public static void Register()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) return;

            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);

            if (key == null) return;

            var current = key.GetValue(AppName) as string;
            if (current == exePath) return;

            key.SetValue(AppName, exePath);
            LogManager.Log("Registered to Windows startup.");
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to register startup: {ex.Message}");
        }
    }
}
