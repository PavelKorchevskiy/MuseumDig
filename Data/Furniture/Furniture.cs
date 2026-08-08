using Godot;
using System.Collections.Generic;

public abstract partial class Furniture : Resource
{
    [Export] public string TypeId = "";       // ID типа (например, "display_case_2x1")
    [Export] public string DisplayName = "";
    
    // Размер мебели в клетках
    public Vector2I Size = new(1, 1);
    
    // Цена покупки
    [Export] public int BuyPrice = 100;
    
    // Цена продажи (половина от покупки)
    public int SellPrice => BuyPrice / 2;
    
    // Может ли этот предмет мебели принять данный ресурс?
    public abstract bool CanAccept(ResourceDefinition resource, Quality quality);
    
    // Получить все предметы в этой мебели
    public abstract List<FoundItem> GetAllItems();
    
    // Добавить предмет
    public abstract bool AddItem(FoundItem item);
    
    // Удалить предмет
    public abstract FoundItem RemoveItem(string resourceId, Quality quality);
    
    // Для сохранения (переопределяется в наследниках)
    public abstract FurnitureSaveData GetSaveData();
    public abstract void LoadFromSaveData(FurnitureSaveData data);
}