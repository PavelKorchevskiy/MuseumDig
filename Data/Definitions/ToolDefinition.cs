using Godot;

[GlobalClass]
public partial class ToolDefinition : Resource
{
	[Export] public ToolType Type = ToolType.Shovel;
	[Export] public string DisplayName = "";
	[Export] public string Description = "";
	
	// Базовый урон по блоку
	[Export] public int BaseDamage = 1;
	
	// Может ли этот инструмент повредить находку?
	[Export] public bool CanDamageFossil = false;
	
	// Базовая скорость использования (задержка между ударами в секундах)
	[Export] public float BaseUseDelay = 0.3f;
	
	// Стоимость улучшения за уровень
	[Export] public int UpgradeCostPerLevel = 100;
	
	// Прирост урона за уровень улучшения
	[Export] public int DamagePerLevel = 1;
	
	// Снижение задержки за уровень (в секундах, вычитается из BaseUseDelay)
	[Export] public float UseDelayReductionPerLevel = 0.02f;
	
	// Минимальная задержка (нижний предел)
	[Export] public float MinUseDelay = 0.1f;
	
	// Для лопаты: базовый шанс повреждения окаменелости (0.0 - 1.0)
	[Export] public float BaseFossilDamageChance = 0.5f;
	
	// Для лопаты: снижение шанса повреждения за уровень
	[Export] public float FossilDamageChanceReductionPerLevel = 0.05f;
	
	// Максимальный уровень улучшения
	[Export] public int MaxUpgradeLevel = 10;
	
	// ===== ГЕТТЕРЫ ХАРАКТЕРИСТИК ПО УРОВНЮ =====
	
	public int GetDamageAtLevel(int level)
	{
		return BaseDamage + (level - 1) * DamagePerLevel;
	}
	
	public float GetUseDelayAtLevel(int level)
	{
		float delay = BaseUseDelay - (level - 1) * UseDelayReductionPerLevel;
		return Mathf.Max(delay, MinUseDelay);
	}
	
	public float GetFossilDamageChanceAtLevel(int level)
	{
		if (!CanDamageFossil) return 0f;
		
		float chance = BaseFossilDamageChance - (level - 1) * FossilDamageChanceReductionPerLevel;
		return Mathf.Max(chance, 0f);
	}
	
	public int GetUpgradeCostForLevel(int level)
	{
		// Стоимость для перехода на следующий уровень
		if (level >= MaxUpgradeLevel) return -1; // Нельзя улучшить дальше
		return UpgradeCostPerLevel * level; // Линейная прогрессия: 100, 200, 300...
	}
}
