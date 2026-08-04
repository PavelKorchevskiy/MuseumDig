using System.Collections.Generic;

// Класс для сохранения одного стака предметов
public class InventorySaveData
{
	public int Amount { get; set; }
	public int Quality { get; set; } // Храним enum Quality как число (0 = Damaged, 1 = Good)
}

public class LocationSaveData
{
	public string CurrentLocationId { get; set; } = "canada";
	public List<string> UnlockedLocations { get; set; } = new();
}

public class SaveData
{
	public int Coins { get; set; } = 0;
	
	public int PickaxeLevel { get; set; } = 1;
	public int CoinBonusLevel { get; set; } = 0;
	public int FossilChanceLevel { get; set; } = 0;
	
	public int CurrentEnergy { get; set; } = 20;
	public int MaxEnergy { get; set; } = 20;
	public int EnergyLevel { get; set; } = 1;
	public int RegenLevel { get; set; } = 0;
	public int CurrentTool { get; set; } = 0; // 0 = Shovel, 1 = Pickaxe
	public LocationSaveData LocationData { get; set; } = new();
	
	// Старые данные (можно удалить позже, когда полностью мигрируем)
	public Dictionary<string, List<int>> FossilPieces { get; set; } = new();
	public Dictionary<string, string> ExhibitedFossils { get; set; } = new();
	
	// НОВЫЙ инвентарь: Ключ = "ResourceId_Quality", Значение = данные
	public Dictionary<string, InventorySaveData> Inventory { get; set; } = new();
	
	public long LastSaveTimestamp { get; set; } = 0;
}
