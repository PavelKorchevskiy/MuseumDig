using Godot;

public partial class ToolDefinition : Resource
{
    [Export] public ToolType Type = ToolType.Shovel;
    [Export] public string DisplayName = "";
    [Export] public string Description = "";
    
    // Базовый урон по блоку
    [Export] public int Damage = 1;
    
    // Может ли этот инструмент повредить находку?
    [Export] public bool CanDamageFossil = false;
    
    // Шанс повреждения находки при ударе (0.0 = 0%, 1.0 = 100%)
    // Для лопаты ставим 0.5f (50%), для кирки 0.0f (0%)
    [Export] public float DamageChance = 0.0f; 
    
    // Скорость использования (задержка между ударами в секундах)
    [Export] public float UseDelay = 0.3f;
}