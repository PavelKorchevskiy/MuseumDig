using Godot;
using System.Collections.Generic;

// Конкретная реализация Furniture для собранных коллекций
public partial class CollectionExhibit : Furniture
{
    public CollectionExhibit(CollectionDefinition collection)
    {
        this.TypeId = collection.Id;
        this.DisplayName = collection.DisplayName;
        this.Size = collection.Size; // Например, 2x2
        this.BuyPrice = 0; // Коллекция уже "оплачена" усилиями по сбору
        // BaseMuseumIncome установим отдельно, если нужно, или через свойство
    }

    // Коллекция не может принять дополнительные предметы (она уже полная)
    public override bool CanAccept(ResourceDefinition resource, Quality quality)
    {
        return false;
    }

    // Внутри собранной коллекции нет отдельных предметов инвентаря
    public override List<FoundItem> GetAllItems()
    {
        return new List<FoundItem>();
    }

    // Добавить предмет в собранную коллекцию нельзя
    public override bool AddItem(FoundItem item)
    {
        return false;
    }

    // Сохранение: возвращаем базовые данные, без предметов
    public override FurnitureSaveData GetSaveData()
    {
        return new FurnitureSaveData
        {
            FurnitureType = "CollectionExhibit",
            DisplayCaseItems = new List<FoundItem>(),
        };
    }

    // Загрузка: ничего делать не нужно, предметов внутри нет
    public override void LoadFromSaveData(FurnitureSaveData data)
    {
        // Пусто, так как коллекция не хранит внутренние предметы
    }
}