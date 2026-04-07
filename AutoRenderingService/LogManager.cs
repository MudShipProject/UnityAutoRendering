namespace AutoRenderingService;

public class LogManager
{
    private static LogManager? _instance;
    public static LogManager Instance => _instance ??= new LogManager();

    private TextBox? _logBox;

    private LogManager() { }

    public void Initialize(TextBox logBox)
    {
        _logBox = logBox;
    }

    public void Log(string message)
    {
        if (_logBox == null || _logBox.IsDisposed) return;

        var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";

        if (_logBox.InvokeRequired)
            _logBox.Invoke(() => _logBox.AppendText(line));
        else
            _logBox.AppendText(line);
    }
}
