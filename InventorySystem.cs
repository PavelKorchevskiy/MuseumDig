using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class InventorySystem : Node
{
	public static InventorySystem Instance { get; private set; }
	
	private Dictionary<string, FoundItem> _items = new();
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	private string GetItemKey(string resourceId, Quality quality)
	{
		return $"{resourceId}_{(int)quality}";
	}
	
	private void ParseItemKey(string key, out string resourceId, out Quality quality)
	{
		var parts = key.Split('_');
		resourceId = parts[0];
		quality = (Quality)int.Parse(parts[1]);
	}
	
	public void AddItem(string resourceId, Quality quality = Quality.Good, int amount = 1)
	{
		if (amount <= 0) return;
		
		var resource = GameData.GetResource(resourceId);
		if (resource == null)
		{
			GD.PrintErr($"[Inventory] Unknown resource: {resourceId}");
			return;
		}
		
		if (!resource.HasQuality)
		{
			quality = Quality.Good;
		}
		
		string key = GetItemKey(resourceId, quality);
		
		if (_items.ContainsKey(key))
		{
			_items[key].Amount += amount;
		}
		else
		{
			_items[key] = new FoundItem(resourceId, quality, amount);
		}
		
		SaveSystem.Instance?.MarkDirty();
	}
	
	public FoundItem GetItem(string resourceId, Quality quality)
	{
		string key = GetItemKey(resourceId, quality);
		return _items.TryGetValue(key, out var item) ? item : null;
	}
	
	public int GetTotalAmount(string resourceId)
	{
		return _items.Values.Where(item => item.ResourceId == resourceId).Sum(item => item.Amount);
	}
	
	public List<FoundItem> GetAllItems()
	{
		return _items.Values.Where(item => item.Amount > 0).ToList();
	}
	
	public int SellItem(string resourceId, Quality quality, int amount)
	{
		string key = GetItemKey(resourceId, quality);
		
		if (!_items.ContainsKey(key) || _items[key].Amount < amount)
		{
			return 0;
		}
		
		var resource = GameData.GetResource(resourceId);
		if (resource == null) return 0;
		
		float multiplier = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(quality);
		int pricePerUnit = (int)(resource.BaseSellPrice * multiplier);
		int totalEarned = pricePerUnit * amount;
		
		_items[key].Amount -= amount;
		if (_items[key].Amount <= 0)
		{
			_items.Remove(key);
		}
		
		Wallet.Instance.AddCoins(totalEarned);
		SaveSystem.Instance?.MarkDirty();
		
		return totalEarned;
	}
	
	// ===== ДЛЯ СОХРАНЕНИЯ =====
	
	public Dictionary<string, InventorySaveData> GetSaveData()
	{
		var saveData = new Dictionary<string, InventorySaveData>();
		foreach (var kvp in _items)
		{
			if (kvp.Value.Amount > 0)
			{
				saveData[kvp.Key] = new InventorySaveData
				{
					Amount = kvp.Value.Amount,
					Quality = (int)kvp.Value.Quality
				};
			}
		}
		return saveData;
	}
	
	public void LoadFromSaveData(Dictionary<string, InventorySaveData> data)
{
	_items.Clear();
	if (data == null) return;
	
	foreach (var kvp in data)
	{
		try
		{
			// Пытаемся распарсить ключ в формате "ResourceId_Quality"
			var parts = kvp.Key.Split('_');
			
			// Проверяем, что есть хотя бы 2 части, и последняя часть - число
			if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int qualityValue))
			{
				// Восстанавливаем ResourceId (может содержать подчёркивания!)
				string resourceId = string.Join("_", parts.Take(parts.Length - 1));
				Quality quality = (Quality)qualityValue;
				
				// Проверяем, что ресурс существует в GameData
				if (GameData.GetResource(resourceId) != null)
				{
					_items[kvp.Key] = new FoundItem(resourceId, quality, kvp.Value.Amount);
				}
				else
				{
					GD.PrintErr($"[Inventory] Resource '{resourceId}' not found in GameData, skipping.");
				}
			}
			else
			{
				// Старый формат (например, "gold_nugget") - игнорируем
				GD.PrintErr($"[Inventory] Skipping old format key: '{kvp.Key}'");
			}
		}
		catch (System.Exception e)
		{
			GD.PrintErr($"[Inventory] Error loading item '{kvp.Key}': {e.Message}");
		}
	}
	GD.Print($"[Inventory] Loaded {_items.Count} unique item stacks");
}

    // ===== УДАЛЕНИЕ ПРЕДМЕТА (для выставки в музее) =====
    public void RemoveItem(string resourceId, Quality quality, int amount = 1)
    {
        string key = GetItemKey(resourceId, quality);
        
        if (!_items.ContainsKey(key) || _items[key].Amount < amount)
        {
            GD.PrintErr($"[Inventory] Cannot remove: not enough {resourceId} ({quality})");
            return;
        }
        
        _items[key].Amount -= amount;
        if (_items[key].Amount <= 0)
        {
            _items.Remove(key);
        }
        
        SaveSystem.Instance?.MarkDirty();
        GD.Print($"[Inventory] Removed {amount}x {resourceId} ({quality})");
    }

	    // ===== ЛОГИКА СБОРА КОЛЛЕКЦИЙ =====

    /// <summary>
    /// Проверяет, можно ли собрать хотя бы 1 экземпляр коллекции
    /// </summary>
    public bool CanAssembleCollection(CollectionDefinition collection)
    {
        if (collection == null || collection.Pieces == null) return false;

        foreach (var piece in collection.Pieces)
        {
            // Нам нужна хотя бы 1 штука любого качества
            if (GetTotalAmount(piece.Id) < 1)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Собирает 1 экземпляр коллекции: списывает по 1 фрагменту и добавляет собранную коллекцию
    /// </summary>
    public bool AssembleCollection(CollectionDefinition collection)
    {
        if (!CanAssembleCollection(collection))
        {
            GD.PrintErr($"[Inventory] Невозможно собрать коллекцию: {collection.DisplayName}");
            return false;
        }

        // 1. Списываем фрагменты
        foreach (var piece in collection.Pieces)
        {
            // Ищем, какое качество этого фрагмента у нас есть
            foreach (Quality q in System.Enum.GetValues(typeof(Quality)))
            {
                var item = GetItem(piece.Id, q);
                if (item != null && item.Amount >= 1)
                {
                    RemoveItem(piece.Id, q, 1);
                    break; // Переходим к следующему фрагменту
                }
            }
        }

        // 2. Добавляем собранную коллекцию в инвентарь (всегда качества Good)
        AddItem(collection.Id, Quality.Good, 1);
        GD.Print($"[Inventory] Успешно собрана коллекция: {collection.DisplayName}");
        
        return true;
    }

    /// <summary>
    /// Возвращает количество предмета конкретного качества (вспомогательный метод)
    /// </summary>
    public int GetAmountByQuality(string resourceId, Quality quality)
    {
        var item = GetItem(resourceId, quality);
        return item != null ? item.Amount : 0;
    }
}
