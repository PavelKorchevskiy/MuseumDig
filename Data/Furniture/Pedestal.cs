using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Pedestal : Furniture
{
    // ID коллекции, которую сейчас выставили на этом пьедестале ("" если пуст)
    public string CurrentCollectionId = "";
    
    private Dictionary<string, Quality> _parts = new();
    
    // Пьедестал принимает ЛЮБУЮ коллекцию, если он пуст
    public override bool CanAccept(ResourceDefinition resource, Quality quality)
    {
        if (resource is not FossilDefinition fossil) return false;
        if (string.IsNullOrEmpty(fossil.CollectionId)) return false;
        
        // Если пьедестал пуст — принимает любую коллекцию
        if (string.IsNullOrEmpty(CurrentCollectionId))
        {
            CurrentCollectionId = fossil.CollectionId;
            return true;
        }
        
        // Если уже выбрана коллекция — принимает только её части
        if (fossil.CollectionId != CurrentCollectionId) return false;
        
        return !_parts.ContainsKey(fossil.Id);
    }
    
    public override List<FoundItem> GetAllItems()
    {
        var items = new List<FoundItem>();
        foreach (var kvp in _parts)
        {
            items.Add(new FoundItem(kvp.Key, kvp.Value, 1));
        }
        return items;
    }
    
    public override bool AddItem(FoundItem item)
    {
        if (!CanAccept(GameData.GetResource(item.ResourceId), item.Quality)) return false;
        _parts[item.ResourceId] = item.Quality;
        return true;
    }
    
    public override FoundItem RemoveItem(string resourceId, Quality quality)
    {
        if (_parts.ContainsKey(resourceId) && _parts[resourceId] == quality)
        {
            _parts.Remove(resourceId);
            
            // Если пьедестал опустел — сбрасываем выбранную коллекцию
            if (_parts.Count == 0) CurrentCollectionId = "";
            
            return new FoundItem(resourceId, quality, 1);
        }
        return null;
    }
    
    public bool IsComplete()
    {
        if (string.IsNullOrEmpty(CurrentCollectionId)) return false;
        var collection = GameData.GetCollection(CurrentCollectionId);
        return collection != null && _parts.Count == collection.Pieces.Count;
    }
    
    public override FurnitureSaveData GetSaveData()
    {
        var data = new Dictionary<string, int>();
        foreach (var kvp in _parts) data[kvp.Key] = (int)kvp.Value;
        
        return new FurnitureSaveData
        {
            FurnitureType = nameof(Pedestal),
            PedestalCollectionId = CurrentCollectionId,
            PedestalParts = data
        };
    }
    
    public override void LoadFromSaveData(FurnitureSaveData data)
    {
        _parts.Clear();
        if (data == null) return;
        
        CurrentCollectionId = data.PedestalCollectionId ?? "";
        if (data.PedestalParts != null)
        {
            foreach (var kvp in data.PedestalParts)
            {
                _parts[kvp.Key] = (Quality)kvp.Value;
            }
        }
    }
}