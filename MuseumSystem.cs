using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class MuseumSystem : Node
{
	public static MuseumSystem Instance { get; private set; }
	
	// Выставленные экспонаты: ключ = "ResourceId_Quality"
	private Dictionary<string, string> _exhibitedItems = new();
	
	// Кэш для быстрого доступа
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
	
	// ===== ПРОВЕРКА ВОЗМОЖНОСТИ ВЫСТАВКИ =====
	
	public bool CanExhibit(string resourceId, Quality quality)
	{
		var resource = GameData.GetResource(resourceId);
		if (resource == null) return false;
		
		string key = $"{resourceId}_{(int)quality}";
		
		// Уже выставлено?
		if (_exhibitedItems.ContainsKey(key)) return false;
		
		// Проверяем, есть ли предмет в инвентаре
		var item = InventorySystem.Instance.GetItem(resourceId, quality);
		if (item == null || item.Amount <= 0) return false;
		
		// Если это часть коллекции — проверяем, собраны ли все части
		if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
		{
			return IsCollectionComplete(fossil.CollectionId, quality);
		}
		
		// Если это одиночная находка (CanExhibitAlone = true) — можно выставлять
		if (resource is FossilDefinition standaloneFossil && standaloneFossil.CanExhibitAlone)
		{
			return true;
		}
		
		return false;
	}
	
	// ===== ПРОВЕРКА ПОЛНОТЫ КОЛЛЕКЦИИ =====
	
	private bool IsCollectionComplete(string collectionId, Quality quality)
	{
		var collection = GameData.GetCollection(collectionId);
		if (collection == null) return false;
		
		// Проверяем, есть ли все 3 части в инвентаре (с нужным качеством)
		foreach (var piece in collection.Pieces)
		{
			var item = InventorySystem.Instance.GetItem(piece.Id, quality);
			if (item == null || item.Amount <= 0)
			{
				return false;
			}
		}
		
		return true;
	}
	
	// ===== ВЫСТАВКА ЭКСПОНАТА =====
	
	public void ExhibitItem(string resourceId, Quality quality)
	{
		if (!CanExhibit(resourceId, quality))
		{
			GD.PrintErr($"[Museum] Cannot exhibit {resourceId} ({quality})");
			return;
		}
		
		string key = $"{resourceId}_{(int)quality}";
		_exhibitedItems[key] = resourceId;
		
		// Удаляем предмет из инвентаря
		InventorySystem.Instance.SellItem(resourceId, quality, 1);
		
		GD.Print($"[Museum] Exhibited {resourceId} ({quality})");
		SaveSystem.Instance?.MarkDirty();
	}
	
	// ===== РАСЧЁТ ДОХОДА =====
	
	private int CalculateItemIncome(string resourceId, Quality quality)
	{
		var resource = GameData.GetResource(resourceId);
		if (resource == null) return 0;
		
		float multiplier = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(quality);
		int baseIncome = (int)(resource.BaseMuseumIncome * multiplier);
		
		// Если это часть коллекции — добавляем бонус за полный сбор
		if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
		{
			var collection = GameData.GetCollection(fossil.CollectionId);
			if (collection != null)
			{
				// Проверяем, выставлены ли все части этой коллекции (с тем же качеством)
				int exhibitedPieces = 0;
				foreach (var piece in collection.Pieces)
				{
					string pieceKey = $"{piece.Id}_{(int)quality}";
					if (_exhibitedItems.ContainsKey(pieceKey))
					{
						exhibitedPieces++;
					}
				}
				
				// Если все части выставлены — применяем бонус коллекции
				if (exhibitedPieces == collection.Pieces.Count)
				{
					baseIncome = (int)(baseIncome * collection.CollectionBonus);
				}
			}
		}
		
		return baseIncome;
	}
	
	private void GenerateIncome()
	{
		if (_exhibitedItems.Count == 0) return;
		
		int totalIncome = 0;
		foreach (var kvp in _exhibitedItems)
		{
			string key = kvp.Key;
			string resourceId = kvp.Value;
			
			// Извлекаем качество из ключа
			var parts = key.Split('_');
			Quality quality = (Quality)int.Parse(parts[1]);
			
			totalIncome += CalculateItemIncome(resourceId, quality);
		}
		
		if (totalIncome > 0)
		{
			Wallet.Instance.AddCoins(totalIncome);
		}
	}
	
	// ===== ГЕТТЕРЫ =====
	
	public int GetTotalIncomePerSecond()
	{
		int total = 0;
		foreach (var kvp in _exhibitedItems)
		{
			string key = kvp.Key;
			string resourceId = kvp.Value;
			
			var parts = key.Split('_');
			Quality quality = (Quality)int.Parse(parts[1]);
			
			total += CalculateItemIncome(resourceId, quality);
		}
		return total;
	}
	
	public Dictionary<string, string> GetExhibitedItems()
	{
		return _exhibitedItems;
	}
	
	public int GetExhibitedCount()
	{
		return _exhibitedItems.Count;
	}
	
	// ===== УДАЛЕНИЕ ЭКСПОНАТА =====
	
	public void RemoveExhibit(string resourceId, Quality quality)
	{
		string key = $"{resourceId}_{(int)quality}";
		
		if (_exhibitedItems.ContainsKey(key))
		{
			_exhibitedItems.Remove(key);
			GD.Print($"[Museum] Removed {resourceId} ({quality}) from exhibition");
			SaveSystem.Instance?.MarkDirty();
		}
	}
	
	// ===== ДЛЯ СОХРАНЕНИЯ =====
	
	public Dictionary<string, string> GetSaveData()
	{
		return new Dictionary<string, string>(_exhibitedItems);
	}
	
	public void LoadFromSaveData(Dictionary<string, string> data)
	{
		_exhibitedItems.Clear();
		if (data == null) return;
		
		foreach (var kvp in data)
		{
			_exhibitedItems[kvp.Key] = kvp.Value;
		}
		GD.Print($"[Museum] Loaded {_exhibitedItems.Count} exhibited items");
	}
}
