using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class MuseumSystem : Node
{
    public static MuseumSystem Instance { get; private set; }
    
    // Выставленные экспонаты: ключ = "ResourceId_Quality"
    private Dictionary<string, string> _exhibitedItems = new();
    
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
        
        string key = GetKey(resourceId, quality);
        if (_exhibitedItems.ContainsKey(key)) return false;
        
        var item = InventorySystem.Instance.GetItem(resourceId, quality);
        if (item == null || item.Amount <= 0) return false;
        
        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            return IsCollectionComplete(fossil.CollectionId, quality);
        }
        
        if (resource is FossilDefinition standalone && standalone.CanExhibitAlone)
        {
            return true;
        }
        
        return false;
    }
    
    private bool IsCollectionComplete(string collectionId, Quality quality)
    {
        var collection = GameData.GetCollection(collectionId);
        if (collection == null) return false;
        
        foreach (var piece in collection.Pieces)
        {
            var item = InventorySystem.Instance.GetItem(piece.Id, quality);
            if (item == null || item.Amount <= 0) return false;
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
        
        string key = GetKey(resourceId, quality);
        _exhibitedItems[key] = resourceId;
        
        // ВАЖНО: Удаляем предмет, а не продаём его!
        InventorySystem.Instance.RemoveItem(resourceId, quality, 1);
        
        GD.Print($"[Museum] Exhibited {resourceId} ({quality})");
        SaveSystem.Instance?.MarkDirty();
    }
    
    // ===== РАСЧЁТ ДОХОДА =====
    
    public int CalculateItemIncome(string resourceId, string key)
    {
        var resource = GameData.GetResource(resourceId);
        if (resource == null) return 0;
        
        Quality quality = GetQualityFromKey(key);
        float multiplier = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(quality);
        int baseIncome = (int)(resource.BaseMuseumIncome * multiplier);
        
        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            var collection = GameData.GetCollection(fossil.CollectionId);
            if (collection != null)
            {
                int exhibitedPieces = 0;
                foreach (var piece in collection.Pieces)
                {
                    if (_exhibitedItems.ContainsKey(GetKey(piece.Id, quality)))
                    {
                        exhibitedPieces++;
                    }
                }
                
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
        int totalIncome = GetTotalIncomePerSecond();
        if (totalIncome > 0)
        {
            Wallet.Instance.AddCoins(totalIncome);
        }
    }
    
    public int GetTotalIncomePerSecond()
    {
        int total = 0;
        foreach (var kvp in _exhibitedItems)
        {
            total += CalculateItemIncome(kvp.Value, kvp.Key);
        }
        return total;
    }
    
    // ===== ГЕТТЕРЫ =====
    
    public Dictionary<string, string> GetExhibitedItems()
    {
        return new Dictionary<string, string>(_exhibitedItems);
    }
    
    public int GetExhibitedCount()
    {
        return _exhibitedItems.Count;
    }
    
    // ===== УТИЛИТЫ ДЛЯ КЛЮЧЕЙ =====
    
    private string GetKey(string resourceId, Quality quality) => $"{resourceId}_{(int)quality}";
    
    public Quality GetQualityFromKey(string key)
    {
        var parts = key.Split('_');
        return (Quality)int.Parse(parts[parts.Length - 1]);
    }
    
    // ===== ДЛЯ СОХРАНЕНИЯ =====
    
    public Dictionary<string, string> GetSaveData() => new Dictionary<string, string>(_exhibitedItems);
    
    public void LoadFromSaveData(Dictionary<string, string> data)
    {
        _exhibitedItems.Clear();
        if (data == null) return;
        
        foreach (var kvp in data)
        {
            try
            {
                var parts = kvp.Key.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[parts.Length - 1], out int q))
                {
                    if (GameData.GetResource(kvp.Value) != null)
                    {
                        _exhibitedItems[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    GD.PrintErr($"[Museum] Пропущена устаревшая запись: '{kvp.Key}'");
                }
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"[Museum] Ошибка загрузки {kvp.Key}: {e.Message}");
            }
        }
        GD.Print($"[Museum] Loaded {_exhibitedItems.Count} exhibited items");
    }
}