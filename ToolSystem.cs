using Godot;

/// <summary>
/// Система управления инструментами: переключение, получение характеристик.
/// </summary>
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
	
	/// <summary>Получить тип текущего инструмента.</summary>
	public ToolType GetCurrentToolType() => _currentTool;
	
	/// <summary>Получить определение текущего инструмента.</summary>
	public ToolDefinition GetCurrentTool()
	{
		return GameData.GetTool(_currentTool);
	}
	
	// ===== ПЕРЕКЛЮЧЕНИЕ =====
	
	/// <summary>Переключить текущий инструмент.</summary>
	public void SetCurrentTool(ToolType tool)
	{
		if (_currentTool == tool) return;
		
		_currentTool = tool;
		var def = GetCurrentTool();
		GD.Print($"[ToolSystem] Switched to {def.DisplayName}");
		SaveSystem.Instance?.MarkDirty();
	}
	
	// ===== УТИЛИТЫ =====
	
	/// <summary>Проверить, может ли текущий инструмент повредить окаменелость.</summary>
	public bool CanDamageFossil()
	{
		var tool = GetCurrentTool();
		return tool != null && tool.CanDamageFossil;
	}
	
	/// <summary>Получить урон текущего инструмента с учётом улучшений.</summary>
	public int GetDamage()
	{
		if (UpgradeSystem.Instance != null)
		{
			return UpgradeSystem.Instance.GetToolDamage(_currentTool);
		}
		
		var tool = GetCurrentTool();
		return tool?.BaseDamage ?? 1;
	}
	
	/// <summary>Получить задержку между ударами текущего инструмента с учётом улучшений.</summary>
	public float GetUseDelay()
	{
		if (UpgradeSystem.Instance != null)
		{
			return UpgradeSystem.Instance.GetToolUseDelay(_currentTool);
		}
		
		var tool = GetCurrentTool();
		return tool?.BaseUseDelay ?? 0.3f;
	}
	
	/// <summary>Получить шанс повреждения окаменелости для текущего инструмента с учётом улучшений.</summary>
	public float GetFossilDamageChance()
	{
		if (UpgradeSystem.Instance != null)
		{
			return UpgradeSystem.Instance.GetToolFossilDamageChance(_currentTool);
		}
		
		var tool = GetCurrentTool();
		return tool?.CanDamageFossil == true ? tool.BaseFossilDamageChance : 0f;
	}
	
	/// <summary>Получить отображаемое имя текущего инструмента.</summary>
	public string GetToolDisplayName()
	{
		var tool = GetCurrentTool();
		return tool?.DisplayName ?? "Unknown";
	}
	
	/// <summary>Получить уровень текущего инструмента.</summary>
	public int GetToolLevel()
	{
		if (UpgradeSystem.Instance == null) return 1;
		
		return _currentTool == ToolType.Shovel 
			? UpgradeSystem.Instance.GetShovelLevel() 
			: UpgradeSystem.Instance.GetPickaxeToolLevel();
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
