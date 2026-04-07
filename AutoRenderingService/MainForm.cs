namespace AutoRenderingService;

public class MainForm : Form
{
    private readonly TreeView _treeView;
    private readonly Button _addButton;
    private readonly Button _removeButton;
    private readonly Button _refreshButton;
    private readonly Button _startRenderButton;
    private readonly TextBox _logBox;

    public MainForm()
    {
        Text = "Auto Rendering Service";
        Size = new Size(550, 500);
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(450, 350);

        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            CheckBoxes = true,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            HideSelection = false,
            ItemHeight = 24,
            Font = new Font("Segoe UI", 9.5f),
        };

        _addButton = new Button { Text = "Add", Size = new Size(90, 32) };
        _removeButton = new Button { Text = "Remove", Size = new Size(90, 32) };
        _refreshButton = new Button { Text = "Refresh", Size = new Size(90, 32) };

        _startRenderButton = new Button
        {
            Text = "▶ Start Rendering",
            Size = new Size(140, 36),
            BackColor = Color.FromArgb(40, 120, 40),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Right,
        };

        _logBox = new TextBox
        {
            Dock = DockStyle.Bottom,
            Height = 100,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(200, 200, 200),
            Font = new Font("Consolas", 9f),
        };

        BuildLayout();
        InitializeManagers();
        BindEvents();
    }

    private void BuildLayout()
    {
        var buttonPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            ColumnCount = 2,
            Padding = new Padding(6),
        };
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var leftButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
        };
        leftButtons.Controls.Add(_addButton);
        leftButtons.Controls.Add(_removeButton);
        leftButtons.Controls.Add(_refreshButton);

        buttonPanel.Controls.Add(leftButtons, 0, 0);
        buttonPanel.Controls.Add(_startRenderButton, 1, 0);

        Controls.Add(_treeView);
        Controls.Add(_logBox);
        Controls.Add(buttonPanel);
    }

    private void InitializeManagers()
    {
        LogManager.Instance.Initialize(_logBox);

        AgentManager.Instance.OnLog += msg => LogManager.Instance.Log(msg);
        AgentManager.Instance.Initialize(_treeView);

        var session = RenderSessionManager.Instance;
        session.OnLog += msg =>
        {
            if (InvokeRequired)
                Invoke(() => LogManager.Instance.Log(msg));
            else
                LogManager.Instance.Log(msg);
        };
        session.OnRecordingStarted += () =>
        {
            if (InvokeRequired)
                Invoke(() => SetRecordingState(true));
            else
                SetRecordingState(true);
        };
        session.OnRecordingStopped += () =>
        {
            if (InvokeRequired)
                Invoke(() => SetRecordingState(false));
            else
                SetRecordingState(false);
        };
    }

    private void BindEvents()
    {
        _addButton.Click += (_, _) => AgentManager.Instance.Add();
        _removeButton.Click += (_, _) => AgentManager.Instance.Remove();
        _refreshButton.Click += async (_, _) => await AgentManager.Instance.RefreshAllAsync();
        _startRenderButton.Click += OnStartRenderClicked;
    }

    private async void OnStartRenderClicked(object? sender, EventArgs e)
    {
        var session = RenderSessionManager.Instance;

        if (session.IsRecording)
        {
            await session.StopRenderAsync();
            return;
        }

        var targets = AgentManager.Instance.GetCheckedTargets();
        await session.StartRenderAsync(targets);
    }

    private void SetRecordingState(bool recording)
    {
        if (recording)
        {
            _startRenderButton.Text = "⏹ Recording";
            _startRenderButton.BackColor = Color.FromArgb(180, 40, 40);
        }
        else
        {
            _startRenderButton.Text = "▶ Start Rendering";
            _startRenderButton.BackColor = Color.FromArgb(40, 120, 40);
        }
    }
}
