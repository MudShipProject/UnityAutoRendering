using System.ComponentModel;
using CoreOSC;

namespace AutoRenderingService;

public class AgentManager
{
    private static AgentManager? _instance;
    public static AgentManager Instance => _instance ??= new AgentManager();

    private TreeView _treeView = null!;
    private AppConfig _config = null!;
    private readonly BindingList<AgentEntry> _entries = new();

    public event Action<string>? OnLog;

    private AgentManager() { }

    public void Initialize(TreeView treeView)
    {
        _treeView = treeView;
        _treeView.AfterCheck += OnTreeAfterCheck;
        _treeView.NodeMouseDoubleClick += OnNodeDoubleClick;
        _config = AppConfig.Load();
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        if (_config.EndPoints.Count == 0)
        {
            AddEntry(new AgentEntry("127.0.0.1", 9000));
            SaveConfig();
            return;
        }

        foreach (var ep in _config.EndPoints)
        {
            AddEntry(new AgentEntry(ep.IpAddress, ep.Port), ep.CheckedScenes);
        }
    }

    public void SaveConfig()
    {
        _config.EndPoints.Clear();

        foreach (TreeNode rootNode in _treeView.Nodes)
        {
            if (rootNode.Tag is not AgentEntry entry) continue;

            var epConfig = new AgentEndPointConfig
            {
                IpAddress = entry.IpAddress,
                Port = entry.Port,
            };

            foreach (TreeNode child in rootNode.Nodes)
            {
                if (child.Checked)
                    epConfig.CheckedScenes.Add(child.Text);
            }

            _config.EndPoints.Add(epConfig);
        }

        _config.Save();
    }

    public void Add()
    {
        using var dialog = new AgentDialog("127.0.0.1", 9000);
        if (dialog.ShowDialog(_treeView.FindForm()) != DialogResult.OK) return;

        var entry = new AgentEntry(dialog.IpAddress, dialog.Port);
        AddEntry(entry);
        SaveConfig();
        _ = FetchScenesAsync(entry, _treeView.Nodes[^1]);
    }

    public void Remove()
    {
        TreeNode? target = _treeView.SelectedNode switch
        {
            { Level: 0 } node => node,
            { Level: 1, Parent: { } parent } => parent,
            _ => null,
        };

        if (target == null) return;

        var index = _treeView.Nodes.IndexOf(target);
        _entries.RemoveAt(index);
        _treeView.Nodes.Remove(target);
        SaveConfig();
    }

    public async Task RefreshAllAsync()
    {
        foreach (TreeNode rootNode in _treeView.Nodes)
        {
            if (rootNode.Tag is AgentEntry entry)
                await FetchScenesAsync(entry, rootNode);
        }
    }

    public List<(AgentEntry entry, List<string> scenes)> GetCheckedTargets()
    {
        var targets = new List<(AgentEntry, List<string>)>();

        foreach (TreeNode rootNode in _treeView.Nodes)
        {
            if (rootNode.Tag is not AgentEntry entry) continue;

            var checkedScenes = new List<string>();
            foreach (TreeNode child in rootNode.Nodes)
            {
                if (child.Checked)
                    checkedScenes.Add(child.Text);
            }

            if (checkedScenes.Count > 0)
                targets.Add((entry, checkedScenes));
        }

        return targets;
    }

    private void AddEntry(AgentEntry entry, List<string>? checkedScenes = null)
    {
        _entries.Add(entry);

        var rootNode = new TreeNode(entry.ToString()) { Tag = entry };

        if (checkedScenes is { Count: > 0 })
        {
            foreach (var scene in checkedScenes)
                rootNode.Nodes.Add(new TreeNode(scene) { Checked = true });
        }

        _treeView.Nodes.Add(rootNode);
        rootNode.Expand();
    }

    private async Task FetchScenesAsync(AgentEntry entry, TreeNode node)
    {
        OnLog?.Invoke($"Fetching scenes from {entry}...");

        try
        {
            var response = await OscManager.Instance.SendAndReceiveAsync(
                entry.IpAddress, entry.Port, "/scenes/list");

            if (response == null || response.Value.Address.Value != "/scenes/result")
            {
                OnLog?.Invoke("Unexpected response.");
                return;
            }

            var checkedScenes = new List<string>();
            foreach (TreeNode child in node.Nodes)
            {
                if (child.Checked)
                    checkedScenes.Add(child.Text);
            }

            node.Nodes.Clear();
            var scenes = response.Value.Arguments
                .Select(a => a?.ToString() ?? "")
                .Where(s => s.Length > 0)
                .ToList();

            foreach (var scene in scenes)
            {
                node.Nodes.Add(new TreeNode(scene)
                {
                    Checked = checkedScenes.Contains(scene),
                });
            }

            node.Expand();
            OnLog?.Invoke($"Got {scenes.Count} scene(s) from {entry}");
            SaveConfig();
        }
        catch (OperationCanceledException)
        {
            OnLog?.Invoke($"Timeout: {entry} did not respond.");
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"Failed: {ex.Message}");
        }
    }

    private void OnNodeDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (e.Node is not { Level: 0, Tag: AgentEntry entry }) return;

        using var dialog = new AgentDialog(entry.IpAddress, entry.Port);
        if (dialog.ShowDialog(_treeView.FindForm()) != DialogResult.OK) return;

        entry.IpAddress = dialog.IpAddress;
        entry.Port = dialog.Port;
        e.Node.Text = entry.ToString();
        SaveConfig();
        _ = FetchScenesAsync(entry, e.Node);
    }

    private void OnTreeAfterCheck(object? sender, TreeViewEventArgs e)
    {
        if (e.Action == TreeViewAction.Unknown) return;

        _treeView.AfterCheck -= OnTreeAfterCheck;
        try
        {
            if (e.Node is { Level: 0 } parent)
            {
                foreach (TreeNode child in parent.Nodes)
                    child.Checked = parent.Checked;
            }
        }
        finally
        {
            _treeView.AfterCheck += OnTreeAfterCheck;
        }

        SaveConfig();
    }
}
