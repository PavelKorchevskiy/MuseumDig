using Godot;

public partial class ToolSystem : Node
{
    public static ToolSystem Instance { get; private set; }
    
    private ToolType _currentTool = ToolType.Shovel;
    
    public override void _Ready()
    {
        Instance = this;
        GD.Print($"[ToolSystem] Starting with {GetCurrentTool().DisplayName}");
    }
    
    public ToolType GetCurrentToolType() => _currentTool;
    
    public ToolDefinition GetCurrentTool() => GameData.GetTool(_currentTool);
    
    public void SetCurrentTool(ToolType tool)
    {
        if (_currentTool == tool) return;
        _currentTool = tool;
        GD.Print($"[ToolSystem] Switched to {GetCurrentTool().DisplayName}");
        SaveSystem.Instance?.MarkDirty();
    }
    
    // ===== ДИНАМИЧЕСКИЕ ХАРАКТЕРИСТИКИ (с учётом улучшений) =====
    
    public int GetDamage()
    {
        return UpgradeSystem.Instance.GetToolDamage(_currentTool);
    }
    
    public float GetDelay()
    {
        return UpgradeSystem.Instance.GetToolDelay(_currentTool);
    }
    
    public float GetDamageChance()
    {
        return UpgradeSystem.Instance.GetToolDamageChance(_currentTool);
    }
    
    public bool CanDamageFossil()
    {
        var tool = GetCurrentTool();
        return tool != null && tool.CanDamageFossil;
    }
    
    public string GetToolDisplayName() => GetCurrentTool()?.DisplayName ?? "Unknown";
    
    // ===== ДЛЯ СОХРАНЕНИЯ =====
    public int GetSaveData() => (int)_currentTool;
    
    public void LoadFromSaveData(int data)
    {
        _currentTool = (ToolType)data;
        GD.Print($"[ToolSystem] Loaded tool: {_currentTool}");
    }
}