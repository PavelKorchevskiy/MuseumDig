using Godot;

/// <summary>
/// Определение инструмента с параметрами улучшения.
/// </summary>
[GlobalClass]
public partial class ToolDefinition : Resource
{
	[Export] public ToolType Type { get; set; } = ToolType.Shovel;
	[Export] public string DisplayName { get; set; } = "";
	[Export] public string Description { get; set; } = "";
	
	// ===== БАЗОВЫЕ ХАРАКТЕРИСТИКИ =====
	
	/// <summary>Базовый урон по блоку.</summary>
	[Export] public int BaseDamage { get; set; } = 1;
	
	/// <summary>Может ли этот инструмент повредить находку?</summary>
	[Export] public bool CanDamageFossil { get; set; } = false;
	
	/// <summary>Базовая задержка между ударами (в секундах).</summary>
	[Export] public float BaseUseDelay { get; set; } = 0.3f;
	
	// ===== ПАРАМЕТРЫ УЛУЧШЕНИЯ =====
	
	/// <summary>Стоимость улучшения за уровень (базовая).</summary>
	[Export] public int UpgradeCostPerLevel { get; set; } = 100;
	
	/// <summary>Прирост урона за каждый уровень улучшения.</summary>
	[Export] public int DamagePerLevel { get; set; } = 1;
	
	/// <summary>Снижение задержки за уровень (в секундах).</summary>
	[Export] public float UseDelayReductionPerLevel { get; set; } = 0.02f;
	
	/// <summary>Минимальная возможная задержка (нижний предел).</summary>
	[Export] public float MinUseDelay { get; set; } = 0.1f;
	
	/// <summary>Базовый шанс повреждения окаменелости (0.0 - 1.0). Только для лопаты.</summary>
	[Export] public float BaseFossilDamageChance { get; set; } = 0.5f;
	
	/// <summary>Снижение шанса повреждения за уровень улучшения.</summary>
	[Export] public float FossilDamageChanceReductionPerLevel { get; set; } = 0.05f;
	
	/// <summary>Максимальный уровень улучшения инструмента.</summary>
	[Export] public int MaxUpgradeLevel { get; set; } = 10;
	
	// ===== ГЕТТЕРЫ ХАРАКТЕРИСТИК ПО УРОВНЮ =====
	
	/// <summary>Получить урон инструмента на указанном уровне.</summary>
	public int GetDamageAtLevel(int level)
	{
		level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
		return BaseDamage + (level - 1) * DamagePerLevel;
	}
	
	/// <summary>Получить задержку между ударами на указанном уровне.</summary>
	public float GetUseDelayAtLevel(int level)
	{
		level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
		float delay = BaseUseDelay - (level - 1) * UseDelayReductionPerLevel;
		return Mathf.Max(delay, MinUseDelay);
	}
	
	/// <summary>Получить шанс повреждения окаменелости на указанном уровне.</summary>
	public float GetFossilDamageChanceAtLevel(int level)
	{
		if (!CanDamageFossil) return 0f;
		
		level = Mathf.Clamp(level, 1, MaxUpgradeLevel);
		float chance = BaseFossilDamageChance - (level - 1) * FossilDamageChanceReductionPerLevel;
		return Mathf.Max(chance, 0f);
	}
	
	/// <summary>Получить стоимость улучшения до указанного уровня. Возвращает -1, если улучшение невозможно.</summary>
	public int GetUpgradeCostForLevel(int level)
	{
		if (level < 1 || level >= MaxUpgradeLevel) return -1;
		return UpgradeCostPerLevel * level; // Линейная прогрессия: 100, 200, 300...
	}
	
	/// <summary>Проверить, можно ли улучшить инструмент с текущего уровня.</summary>
	public bool CanUpgrade(int currentLevel)
	{
		return currentLevel < MaxUpgradeLevel;
	}
}
