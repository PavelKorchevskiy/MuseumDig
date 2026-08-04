using Godot;

public partial class ToolSystem : Node
{
	public static ToolSystem Instance { get; private set; }
	
	// Текущий выбранный инструмент
	private ToolType _currentTool = ToolType.Shovel;
	
	public override void _Ready()
	{
		Instance = this;
		GD.Print($"[ToolSystem] Starting with {_currentTool}");
	}
	
	// ===== ГЕТТЕРЫ =====
	
	public ToolType GetCurrentToolType() => _currentTool;
	
	public ToolDefinition GetCurrentTool()
	{
		return GameData.GetTool(_currentTool);
	}
	
	// ===== ПЕРЕКЛЮЧЕНИЕ =====
	
	public void SetCurrentTool(ToolType tool)
	{
		if (_currentTool == tool) return;
		
		_currentTool = tool;
		var def = GetCurrentTool();
		GD.Print($"[ToolSystem] Switched to {def.DisplayName}");
		SaveSystem.Instance?.MarkDirty();
	}
	
	// ===== УТИЛИТЫ =====
	
	public bool CanDamageFossil()
	{
		var tool = GetCurrentTool();
		return tool != null && tool.CanDamageFossil;
	}
	
	public int GetDamage()
	{
		// Используем UpgradeSystem для получения урона с учётом уровня инструмента
		if (UpgradeSystem.Instance != null)
		{
			return UpgradeSystem.Instance.GetToolDamage(_currentTool);
		}
		
		// Fallback к базовому значению
		var tool = GetCurrentTool();
		return tool?.BaseDamage ?? 1;
	}
	
	public float GetUseDelay()
	{
		// Используем UpgradeSystem для получения задержки с учётом уровня инструмента
		if (UpgradeSystem.Instance != null)
		{
			return UpgradeSystem.Instance.GetToolUseDelay(_currentTool);
		}
		
		// Fallback к базовому значению
		var tool = GetCurrentTool();
		return tool?.BaseUseDelay ?? 0.3f;
	}
	
	public float GetFossilDamageChance()
	{
		// Используем UpgradeSystem для получения шанса повреждения с учётом уровня инструмента
		if (UpgradeSystem.Instance != null)
		{
			return UpgradeSystem.Instance.GetToolFossilDamageChance(_currentTool);
		}
		
		// Fallback к базовому значению
		var tool = GetCurrentTool();
		return tool?.CanDamageFossil == true ? tool.BaseFossilDamageChance : 0f;
	}
	
	public string GetToolDisplayName()
	{
		var tool = GetCurrentTool();
		return tool?.DisplayName ?? "Unknown";
	}
	
	// ===== ДЛЯ СОХРАНЕНИЯ =====
	
	public int GetSaveData()
	{
		return (int)_currentTool;
	}
	
	public void LoadFromSaveData(int data)
	{
		_currentTool = (ToolType)data;
		GD.Print($"[ToolSystem] Loaded tool: {_currentTool}");
	}
}
