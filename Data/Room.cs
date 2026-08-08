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
        // Проверка границ (с учётом отступа 1 клетка от стен)
        if (position.X < 1 || position.Y < 1) return false;
        if (position.X + size.X > Width - 1) return false;
        if (position.Y + size.Y > Height - 1) return false;
        
        // Проверка занятости (с буферной зоной 1 клетка вокруг)
        for (int x = position.X - 1; x <= position.X + size.X; x++)
        {
            for (int y = position.Y - 1; y <= position.Y + size.Y; y++)
            {
                if (IsCellOccupied(x, y)) return false;
            }
        }
        
        return true;
    }
    
    public void PlaceFurniture(PlacedFurniture placed)
    {
        PlacedFurnitureList.Add(placed);
        
        // Помечаем клетки как занятые (с буферной зоной)
        for (int x = placed.Position.X - 1; x <= placed.Position.X + placed.Size.X; x++)
        {
            for (int y = placed.Position.Y - 1; y <= placed.Position.Y + placed.Size.Y; y++)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    _occupancyGrid[x, y] = true;
                }
            }
        }
    }
    
    public void RemoveFurniture(PlacedFurniture placed)
    {
        PlacedFurnitureList.Remove(placed);
        
        // Освобождаем клетки (с буферной зоной)
        for (int x = placed.Position.X - 1; x <= placed.Position.X + placed.Size.X; x++)
        {
            for (int y = placed.Position.Y - 1; y <= placed.Position.Y + placed.Size.Y; y++)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    // Проверяем, не занята ли клетка другой мебелью
                    bool stillOccupied = false;
                    foreach (var other in PlacedFurnitureList)
                    {
                        if (x >= other.Position.X - 1 && x <= other.Position.X + other.Size.X &&
                            y >= other.Position.Y - 1 && y <= other.Position.Y + other.Size.Y)
                        {
                            stillOccupied = true;
                            break;
                        }
                    }
                    if (!stillOccupied) _occupancyGrid[x, y] = false;
                }
            }
        }
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
                FurnitureSaveData = placed.Furniture.GetSaveData()
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