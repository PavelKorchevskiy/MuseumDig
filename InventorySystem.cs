using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class InventorySystem : Node
{
    public static InventorySystem Instance { get; private set; }
    
    // Ключом теперь является просто ResourceId
    private Dictionary<string, FoundItem> _items = new();
    
    public override void _Ready()
    {
        Instance = this;
    }
    
        public void AddItem(string resourceId, int amount = 1)
    {
        if (amount <= 0) return;
        
        var resource = GameData.GetResource(resourceId);
        if (resource == null)
        {
            GD.PrintErr($"[Inventory] Unknown resource: {resourceId}");
            return;
        }
        
        if (_items.ContainsKey(resourceId))
        {
            _items[resourceId].Amount += amount;
        }
        else
        {
            _items[resourceId] = new FoundItem(resourceId, amount);
        }
        
        // === ЕДИНАЯ ТОЧКА: Сообщаем системе прогресса о получении предмета ===
        ProgressSystem.Instance.RegisterDiscovery(resourceId);
        
        SaveSystem.Instance?.MarkDirty();
    }
    
    public FoundItem GetItem(string resourceId)
    {
        return _items.TryGetValue(resourceId, out var item) ? item : null;
    }
    
    public int GetTotalAmount(string resourceId)
    {
        return _items.TryGetValue(resourceId, out var item) ? item.Amount : 0;
    }
    
    public List<FoundItem> GetAllItems()
    {
        return _items.Values.Where(item => item.Amount > 0).ToList();
    }
    
    public int SellItem(string resourceId, int amount)
    {
        if (!_items.ContainsKey(resourceId) || _items[resourceId].Amount < amount)
        {
            return 0;
        }
        
        var resource = GameData.GetResource(resourceId);
        if (resource == null) return 0;
        
        float multiplier = resource.GetRarityMultiplier(); 
        int pricePerUnit = (int)(resource.BaseSellPrice * multiplier);
        int totalEarned = pricePerUnit * amount;
        
        _items[resourceId].Amount -= amount;
        if (_items[resourceId].Amount <= 0)
        {
            _items.Remove(resourceId);
        }
        
        Wallet.Instance.AddCoins(totalEarned);
        SaveSystem.Instance?.MarkDirty();
        
        return totalEarned;
    }
    
    // ===== ДЛЯ СОХРАНЕНИЯ =====
    public Dictionary<string, int> GetSaveData()
    {
        var saveData = new Dictionary<string, int>();
        foreach (var kvp in _items)
        {
            if (kvp.Value.Amount > 0)
            {
                saveData[kvp.Key] = kvp.Value.Amount;
            }
        }
        return saveData;
    }
    
    public void LoadFromSaveData(Dictionary<string, int> data)
    {
        _items.Clear();
        if (data == null) return;
        
        foreach (var kvp in data)
        {
            try
            {
                string resourceId = kvp.Key;
                
                if (GameData.GetResource(resourceId) != null)
                {
                    _items[resourceId] = new FoundItem(resourceId, kvp.Value);
                }
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"[Inventory] Error loading item '{kvp.Key}': {e.Message}");
            }
        }
        GD.Print($"[Inventory] Loaded {_items.Count} unique item stacks");
    }

    public void RemoveItem(string resourceId, int amount = 1)
    {
        if (!_items.ContainsKey(resourceId) || _items[resourceId].Amount < amount)
        {
            GD.PrintErr($"[Inventory] Cannot remove: not enough {resourceId}");
            return;
        }
        
        _items[resourceId].Amount -= amount;
        if (_items[resourceId].Amount <= 0)
        {
            _items.Remove(resourceId);
        }
        
        SaveSystem.Instance?.MarkDirty();
    }

    // ===== ЛОГИКА СБОРА КОЛЛЕКЦИЙ (УПРОЩЕННАЯ) =====
    public bool CanAssembleCollection(CollectionDefinition collection)
    {
        if (collection == null || collection.Pieces == null) return false;

        foreach (var piece in collection.Pieces)
        {
            if (GetTotalAmount(piece.Id) < 1)
            {
                return false;
            }
        }
        return true;
    }

    public bool AssembleCollection(CollectionDefinition collection)
    {
        if (!CanAssembleCollection(collection))
        {
            GD.PrintErr($"[Inventory] Невозможно собрать коллекцию: {collection.DisplayName}");
            return false;
        }

        // Списываем по 1 фрагменту
        foreach (var piece in collection.Pieces)
        {
            RemoveItem(piece.Id, 1);
        }

        // Добавляем собранную коллекцию
        AddItem(collection.Id, 1);
        GD.Print($"[Inventory] Успешно собрана коллекция: {collection.DisplayName}");
        
        return true;
    }
}