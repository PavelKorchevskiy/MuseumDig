using Godot;
using System.Collections.Generic;

public partial class MuseumSystem : Node
{
	public static MuseumSystem Instance { get; private set; }
	
	private Dictionary<string, int> _exhibitedFossils = new();
	
	private const int IncomePerFossil = 10;
	
	private double _incomeTimer = 0;
	private const double IncomeInterval = 1.0;
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	public override void _Process(double delta)
	{
		_incomeTimer += delta;
		
		if (_incomeTimer >= IncomeInterval)
		{
			_incomeTimer = 0;
			GenerateIncome();
		}
	}
	
	public bool CanExhibit(string fossilId)
	{
		return FossilInventory.Instance.IsFossilComplete(fossilId) 
			&& !_exhibitedFossils.ContainsKey(fossilId);
	}
	
	public void ExhibitFossil(string fossilId)
	{
		if (!CanExhibit(fossilId))
		{
			GD.Print($"Cannot exhibit {fossilId}");
			return;
		}
		
		_exhibitedFossils[fossilId] = IncomePerFossil;
		GD.Print($"Exhibited {fossilId}! Income: {IncomePerFossil} coins/sec");
		SaveSystem.Instance?.MarkDirty();
	}
	
	private void GenerateIncome()
	{
		if (_exhibitedFossils.Count == 0) return;
		
		int totalIncome = 0;
		foreach (var kvp in _exhibitedFossils)
		{
			totalIncome += kvp.Value;
		}
		
		Wallet.Instance.AddCoins(totalIncome);
	}
	
	public int GetTotalIncomePerSecond()
	{
		int total = 0;
		foreach (var kvp in _exhibitedFossils)
		{
			total += kvp.Value;
		}
		return total;
	}
	
	public Dictionary<string, int> GetExhibitedFossils()
	{
		return _exhibitedFossils;
	}
	
	public int GetExhibitedCount()
	{
		return _exhibitedFossils.Count;
	}
	
	// ===== МЕТОДЫ ДЛЯ СОХРАНЕНИЯ =====
	
	public Dictionary<string, int> GetSaveData()
	{
		return new Dictionary<string, int>(_exhibitedFossils);
	}
	
	public void LoadFromSaveData(Dictionary<string, int> data)
	{
		_exhibitedFossils.Clear();
		if (data == null) return;
		
		foreach (var kvp in data)
		{
			_exhibitedFossils[kvp.Key] = kvp.Value;
		}
		GD.Print($"Loaded {_exhibitedFossils.Count} exhibited fossils");
	}
}
