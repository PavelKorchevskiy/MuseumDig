using System.Collections.Generic;


public class LocationSaveData
{
	public string CurrentLocationId { get; set; } = "canada";
	public List<string> UnlockedLocations { get; set; } = new();
}

public class SaveData
{
	public int Coins { get; set; } = 0;
	
	public int PickaxeLevel { get; set; } = 1;
	
	public int CurrentEnergy { get; set; } = 20;
	public int MaxEnergy { get; set; } = 20;
	public int EnergyLevel { get; set; } = 1;
	public int RegenLevel { get; set; } = 0;
	public int CurrentTool { get; set; } = 0; // 0 = Shovel, 1 = Pickaxe
	public LocationSaveData LocationData { get; set; } = new();

	public int ShovelLevel { get; set; } = 1;
	
	// Старые данные (можно удалить позже, когда полностью мигрируем)
	public Dictionary<string, List<int>> FossilPieces { get; set; } = new();
	public MuseumSaveData MuseumData { get; set; }
	
	public Dictionary<string, int> Inventory { get; set; } = new();

	    // Новые поля для отслеживания открытых предметов
       public Dictionary<string, object> ProgressData { get; set; } = new();

    public Dictionary<string, int> SkillLevels { get; set; } = new();
    public int AvailableSkillPoints { get; set; } = 0;
	public HashSet<string> UnlockedCollections { get; set; } = new();
    public HashSet<string> UnlockedResources { get; set; } = new();
	
	public long LastSaveTimestamp { get; set; } = 0;
}
