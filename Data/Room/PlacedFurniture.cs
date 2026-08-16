using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class PlacedFurniture : Resource
{
    // === СУЩЕСТВУЮЩИЕ СВОЙСТВА (восстановлены) ===
    [Export] public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    [Export] public Vector2I Position { get; set; }
    [Export] public Vector2I Size { get; set; }
    [Export] public Furniture Furniture { get; set; } // Ссылка на шаблон мебели
    [Export] public string FurnitureTypeId { get; set; }

    // === НОВАЯ ЛОГИКА ИНВЕНТАРЯ ===
    // Список предметов, которые находятся именно в ЭТОЙ расставленной мебели
    public List<FoundItem> Items { get; set; } = new List<FoundItem>();

    // Конструктор по умолчанию (требуется для Resource)
    public PlacedFurniture() { }

    // Основной конструктор
    public PlacedFurniture(Furniture furniture, Vector2I position)
    {
        Furniture = furniture;
        Position = position;
        Size = furniture.Size; // Размер берется из шаблона
        InstanceId = Guid.NewGuid().ToString();
    }

    // Получить все предметы
    public List<FoundItem> GetAllItems()
    {
        return Items;
    }

    public bool AddItem(FoundItem item)
{
    int maxCapacity = GetMaxCapacity();
    if (Items.Count >= maxCapacity)
    {
        GD.PrintErr($"[PlacedFurniture] Лимит! Максимум: {maxCapacity}");
        return false;
    }
    
    Items.Add(item);
    // ДОБАВЬТЕ ЭТУ СТРОКУ:
    GD.Print($"[DEBUG ADD] Витрина ID:{InstanceId} | Добавлен {item.ResourceId} | Всего предметов: {Items.Count}");
    return true;
}

    // Метод для расчета вместимости
    private int GetMaxCapacity()
    {
        if (Furniture is DisplayCase)
        {
            // Большая витрина (2x1) вмещает 2 предмета, малая (1x1) — 1
            return (Size.X == 2 && Size.Y == 1) ? 2 : 1; 
        }
        
        if (Furniture is Pedestal)
        {
            // Пьедестал обычно вмещает 1 скелет (поправьте, если у вас иначе)
            return 1; 
        }
        
        return 1; // По умолчанию
    }

    // Удалить предмет (реализация, которую мы добавляли)
    public FoundItem RemoveItem(string resourceId, Quality quality)
    {
        var item = Items.FirstOrDefault(i => i.ResourceId == resourceId && i.Quality == quality);
        
        if (item != null)
        {
            Items.Remove(item);
            GD.Print($"[PlacedFurniture] Удален предмет: {resourceId} ({quality})");
            return item;
        }
        
        GD.PrintErr($"[PlacedFurniture] Не удалось найти предмет {resourceId} ({quality}) для удаления!");
        return null;
    }
}