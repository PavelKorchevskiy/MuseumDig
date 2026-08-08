using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DisplayCase : Furniture
{
    [Export] public int Capacity = 10;
    
    private List<FoundItem> _items = new();
    
    public override bool CanAccept(ResourceDefinition resource, Quality quality)
    {
        if (_items.Count >= Capacity) return false;
        if (resource is FossilDefinition fossil) return fossil.CanExhibitAlone;
        return false;
    }
    
    public override List<FoundItem> GetAllItems() => new List<FoundItem>(_items);
    
    public override bool AddItem(FoundItem item)
    {
        if (_items.Count >= Capacity) return false;
        if (!CanAccept(GameData.GetResource(item.ResourceId), item.Quality)) return false;
        _items.Add(item);
        return true;
    }
    
    public override FoundItem RemoveItem(string resourceId, Quality quality)
    {
        var item = _items.FirstOrDefault(i => i.ResourceId == resourceId && i.Quality == quality);
        if (item != null) _items.Remove(item);
        return item;
    }
    
    public override FurnitureSaveData GetSaveData()
    {
        return new FurnitureSaveData
        {
            FurnitureType = nameof(DisplayCase),
            DisplayCaseItems = new List<FoundItem>(_items)
        };
    }
    
    public override void LoadFromSaveData(FurnitureSaveData data)
    {
        _items.Clear();
        if (data?.DisplayCaseItems != null) _items.AddRange(data.DisplayCaseItems);
    }
}