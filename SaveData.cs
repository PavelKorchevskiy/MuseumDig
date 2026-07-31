using System.Collections.Generic;

public class SaveData
{
	 // Валюта
	public int Coins { get; set; } = 0;
	
	// Улучшения
	public int PickaxeLevel { get; set; } = 1;
	public int CoinBonusLevel { get; set; } = 0;
	public int FossilChanceLevel { get; set; } = 0;
	// Энергия
public int CurrentEnergy { get; set; } = 20;
public int MaxEnergy { get; set; } = 20;
public int EnergyLevel { get; set; } = 1;
public int RegenLevel { get; set; } = 0;
	
	// Инвентарь находок: fossilId -> список pieceIndex
	public Dictionary<string, List<int>> FossilPieces { get; set; } = new();
	
	// Выставленные экспонаты: fossilId -> доход
	public Dictionary<string, int> ExhibitedFossils { get; set; } = new();
	
	// Время последнего сохранения
	public long LastSaveTimestamp { get; set; } = 0;
}
