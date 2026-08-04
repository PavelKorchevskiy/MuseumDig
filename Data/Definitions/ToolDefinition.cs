using Godot;

[GlobalClass]
public partial class ToolDefinition : Resource
{
	[Export] public ToolType Type = ToolType.Shovel;
	[Export] public string DisplayName = "";
	[Export] public string Description = "";
	
	// Базовый урон по блоку
	[Export] public int Damage = 1;
	
	// Может ли этот инструмент повредить находку?
	[Export] public bool CanDamageFossil = false;
	
	// Скорость использования (задержка между ударами в секундах)
	[Export] public float UseDelay = 0.3f;
	
	// Стоимость улучшения (если применимо)
	[Export] public int UpgradeCost = 100;
}
