using Godot;
using System.Collections.Generic;

public partial class Room : Resource
{
    [Export] public string Id = "";
    [Export] public string DisplayName = "";
    
    // Позиция зала на глобальной карте музея (без [Export], так как это Vector2I)
    public Vector2I GlobalPosition;
    
    // Размеры зала
    [Export] public int Width = 10;
    [Export] public int Height = 10;
    
    // Это главный зал (с дверью на улицу)
    [Export] public bool IsMainHall = false;
    
    // Сетка занятости (true = занята мебелью ИЛИ буферной зоной)
    private bool[,] _occupancyGrid;
    
    // Размещённая мебель
    public List<PlacedFurniture> PlacedFurnitureList = new();
    
    // Двери (4 стены)
    public Dictionary<Direction, Door> Doors = new();
    
    // ===== ИНИЦИАЛИЗАЦИЯ =====
    
    public void InitializeGrid()
    {
        _occupancyGrid = new bool[Width, Height];
    }
    
    // ===== ГЕТТЕРЫ =====
    
    public bool IsCellOccupied(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return true; // Стена = занята
        return _occupancyGrid[x, y];
    }
    
    public Door GetDoor(Direction dir)
    {
        return Doors.TryGetValue(dir, out var door) ? door : null;
    }
    
    // ===== РАЗМЕЩЕНИЕ МЕБЕЛИ =====
    
        public bool CanPlaceFurniture(Vector2I position, Vector2I size)
    {
        // 1. Проверка границ с отступом 1 клетка от стен
        // Зал имеет клетки от 0 до Width-1. Отступ 1 значит:
        if (position.X < 1 || position.Y < 1) return false;
        if (position.X + size.X > Width - 1) return false;
        if (position.Y + size.Y > Height - 1) return false;

        // 2. Проверка коллизий с другой мебелью (с учетом буфера ровно в 1 клетку)
        foreach (var existing in PlacedFurnitureList)
        {
            // Границы существующей мебели + 1 клетка буфера вокруг
            int exMinX = existing.Position.X - 1;
            int exMaxX = existing.Position.X + existing.Size.X; // Последняя клетка мебели + 1 клетка буфера
            int exMinY = existing.Position.Y - 1;
            int exMaxY = existing.Position.Y + existing.Size.Y;

            // Границы новой мебели
            int newX1 = position.X;
            int newX2 = position.X + size.X - 1;
            int newY1 = position.Y;
            int newY2 = position.Y + size.Y - 1;

            // Проверка пересечения прямоугольников
            bool overlapX = newX1 <= exMaxX && newX2 >= exMinX;
            bool overlapY = newY1 <= exMaxY && newY2 >= exMinY;

            if (overlapX && overlapY)
            {
                return false; // Пересечение с мебелью или её буферной зоной
            }
        }

        return true;
    }

    public void PlaceFurniture(PlacedFurniture placed)
    {
        PlacedFurnitureList.Add(placed);
        // Сетка _occupancyGrid больше не нужна для коллизий. 
        // Мы полагаемся на прямой перебор PlacedFurnitureList, что на 100% надежно.
    }

    public void RemoveFurniture(PlacedFurniture placed)
    {
        PlacedFurnitureList.Remove(placed);
    }
    
    // ===== ДЛЯ ПОИСКА ПУТИ (задел на посетителей) =====
    
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
        
        foreach (var placed in PlacedFurnitureList)
        {
            if (x >= placed.Position.X && x < placed.Position.X + placed.Size.X &&
                y >= placed.Position.Y && y < placed.Position.Y + placed.Size.Y)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // ===== СОХРАНЕНИЕ =====
    
    public RoomSaveData GetSaveData()
    {
        var data = new RoomSaveData
        {
            Id = Id,
            GlobalPositionX = GlobalPosition.X,
            GlobalPositionY = GlobalPosition.Y,
            Furniture = new List<PlacedFurnitureSaveData>()
        };
        
        foreach (var placed in PlacedFurnitureList)
        {
            data.Furniture.Add(new PlacedFurnitureSaveData
            {
                InstanceId = placed.InstanceId,
                FurnitureTypeId = placed.FurnitureTypeId,
                PositionX = placed.Position.X,
                PositionY = placed.Position.Y,
                SizeX = placed.Size.X,
                SizeY = placed.Size.Y,
                FurnitureSaveData =  new FurnitureSaveData
{
    FurnitureType = placed.Furniture.GetType().Name, // Или placed.FurnitureTypeId
    PedestalCollectionId = null, // Если нужно
    // БЕРЕМ ПРЕДМЕТЫ ИЗ ЭКЗЕМПЛЯРА, А НЕ ИЗ ШАБЛОНА!
    DisplayCaseItems = new List<FoundItem>(placed.Items) 
}
            });
        }
        
        data.Doors = new Dictionary<int, string>();
        foreach (var kvp in Doors)
        {
            data.Doors[(int)kvp.Key] = kvp.Value.ConnectedRoomId;
        }
        
        return data;
    }
}