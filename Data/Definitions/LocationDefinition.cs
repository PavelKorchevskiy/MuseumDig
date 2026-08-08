using Godot;
using Godot.Collections;
using System.Collections.Generic;

[GlobalClass]
public partial class LocationDefinition : Resource
{
	[Export] public string Id = "";
	[Export] public string DisplayName = "";
	[Export] public string Description = "";
	
	// Стоимость открытия локации
	[Export] public int UnlockCost = 0;
	
	// Требуемый уровень игрока
	[Export] public int RequiredPlayerLevel = 1;
	
	// Параметры сетки раскопок
	[Export] public int GridWidth = 8;
	[Export] public int GridHeight = 12;
	[Export] public float BaseTileHp = 3;
	[Export] public float TileHpGrowthPerRow = 1.15f;
	
	// Таблица лута
	public List<LootEntry> LootTable = new List<LootEntry>();
	
	// Уникальная находка локации (опционально)
	[Export] public FossilDefinition UniqueFossil;
}
