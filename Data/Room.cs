using Godot;
using System.Collections.Generic;

public partial class Room : Resource
{
    [Export] public string Id = "";
    [Export] public string DisplayName = "";
    [Export] public Vector2I GlobalOffset;
    [Export] public int Width;
    [Export] public int Height;
    [Export] public bool IsMainHall;
    
    // === МЕХАНИКА ОТКРЫТИЯ КОМНАТ ===
    [Export] public bool IsUnlocked = true; 

    // Для системы освещения
    public List<string> AdjacentRoomIds = new();

    public bool[,] _occupancyGrid;
    public List<PlacedFurniture> PlacedFurnitureList = new();

    public void InitializeGrid()
    {
        _occupancyGrid = new bool[Width, Height];
    }

    public bool CanPlaceFurniture(Vector2I position, Vector2I size)
    {
        if (position.X < 1 || position.Y < 1) return false;
        if (position.X + size.X > Width - 1) return false;
        if (position.Y + size.Y > Height - 1) return false;

        foreach (var existing in PlacedFurnitureList)
        {
            int exMinX = existing.Position.X - 1;
            int exMaxX = existing.Position.X + existing.Size.X;
            int exMinY = existing.Position.Y - 1;
            int exMaxY = existing.Position.Y + existing.Size.Y;

            int newX1 = position.X;
            int newX2 = position.X + size.X - 1;
            int newY1 = position.Y;
            int newY2 = position.Y + size.Y - 1;

            if (newX1 <= exMaxX && newX2 >= exMinX && newY1 <= exMaxY && newY2 >= exMinY)
                return false;
        }
        return true;
    }

        /// <summary>
    /// Проверяет, занята ли указанная глобальная клетка мебелью
    /// </summary>
    public bool IsCellOccupiedByFurniture(Vector2I globalPos)
    {
        int localX = globalPos.X - GlobalOffset.X;
        int localY = globalPos.Y - GlobalOffset.Y;

        foreach (var placed in PlacedFurnitureList)
        {
            // Проверяем, попадает ли клетка в footprint мебели
            if (localX >= placed.Position.X && localX < placed.Position.X + placed.Size.X &&
                localY >= placed.Position.Y && localY < placed.Position.Y + placed.Size.Y)
            {
                return true;
            }
        }
        return false;
    }

    public void PlaceFurniture(PlacedFurniture placed)
    {
        PlacedFurnitureList.Add(placed);
    }

    public void RemoveFurniture(PlacedFurniture placed)
    {
        PlacedFurnitureList.Remove(placed);
    }

    public RoomSaveData GetSaveData()
    {
        var data = new RoomSaveData
        {
            Id = Id,
            GlobalPositionX = GlobalOffset.X,
            GlobalPositionY = GlobalOffset.Y,
            IsUnlocked = IsUnlocked, // Сохраняем состояние открытия
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
                FurnitureSaveData = new FurnitureSaveData
                {
                    FurnitureType = placed.Furniture.GetType().Name,
                    IsFlipped = placed.IsFlipped,
                    DisplayCaseItems = new List<FoundItem>(placed.Items ?? new List<FoundItem>())
                }
            });
        }
        return data;
    }
}