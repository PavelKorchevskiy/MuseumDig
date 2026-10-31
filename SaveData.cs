using System.Collections.Generic;


public class LocationSaveData
{
    // Хранит ID локации, на которой прямо сейчас идет сессия раскопок.
    // Если пусто ("") или null, значит, активной сессии нет.
    public string ActiveDiggingLocationId { get; set; } = "";
}

public class SaveData
{
	public int Coins { get; set; } = 0;
	
	public LocationSaveData LocationData { get; set; } = new();
	
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

	    public Dictionary<string, Dictionary<string, DiggingTileData>> DiggingProgressByLocation { get; set; } = new();

	
	public long LastSaveTimestamp { get; set; } = 0;
}
