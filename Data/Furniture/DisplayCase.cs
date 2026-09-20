using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DisplayCase : Furniture
{
    [Export] public int Capacity = 10;
    
    private List<FoundItem> _items = new();
    
        public override bool CanAccept(ResourceDefinition resource)
    {
        if (_items.Count >= Capacity) return false;
        if (resource is FossilDefinition fossil) 
        {
            return fossil.CanExhibitAlone && !fossil.IsCollection;
        }
        return false;
    }
    
    public override List<FoundItem> GetAllItems() => new List<FoundItem>(_items);
    
    public override bool AddItem(FoundItem item)
    {
        if (_items.Count >= Capacity) return false;
        if (!CanAccept(GameData.GetResource(item.ResourceId))) return false;
        _items.Add(item);
        return true;
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