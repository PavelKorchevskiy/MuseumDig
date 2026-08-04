using System.Collections.Generic;

/// <summary>
/// Класс для сохранения одного стака предметов.
/// </summary>
public class InventorySaveData
{
	public int Amount { get; set; }
	public int Quality { get; set; } // Храним enum Quality как число (0 = Damaged, 1 = Good)
}

/// <summary>
/// Данные для сохранения состояния локации.
/// </summary>
public class LocationSaveData
{
	public string CurrentLocationId { get; set; } = "canada";
	public List<string> UnlockedLocations { get; set; } = new();
}

/// <summary>
/// Основные данные сохранения игры.
/// </summary>
public class SaveData
{
	// ===== ВАЛЮТА =====
	public int Coins { get; set; } = 0;
	
	// ===== ГЛОБАЛЬНЫЕ УЛУЧШЕНИЯ =====
	public int PickaxeLevel { get; set; } = 1;
	public int CoinBonusLevel { get; set; } = 0;
	public int FossilChanceLevel { get; set; } = 0;
	
	// ===== УРОВНИ ИНСТРУМЕНТОВ =====
	public int ShovelLevel { get; set; } = 1;
	public int PickaxeToolLevel { get; set; } = 1; // Переименовано для избежания конфликта с PickaxeLevel
	
	// ===== ЭНЕРГИЯ =====
	public int CurrentEnergy { get; set; } = 20;
	public int MaxEnergy { get; set; } = 20;
	public int EnergyLevel { get; set; } = 1;
	public int RegenLevel { get; set; } = 0;
	
	// ===== ИНСТРУМЕНТЫ =====
	public int CurrentTool { get; set; } = 0; // 0 = Shovel, 1 = Pickaxe
	
	// ===== ЛОКАЦИИ =====
	public LocationSaveData LocationData { get; set; } = new();
	
	// ===== СТАРЫЕ ДАННЫЕ (для обратной совместимости) =====
	public Dictionary<string, List<int>> FossilPieces { get; set; } = new();
	public Dictionary<string, string> ExhibitedFossils { get; set; } = new();
	
	// ===== НОВЫЙ ИНВЕНТАРЬ: Ключ = "ResourceId_Quality", Значение = данные =====
	public Dictionary<string, InventorySaveData> Inventory { get; set; } = new();
	
	// ===== ВРЕМЯ ПОСЛЕДНЕГО СОХРАНЕНИЯ =====
	public long LastSaveTimestamp { get; set; } = 0;
}
