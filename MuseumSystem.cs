using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MuseumSystem : Node
{
    public static MuseumSystem Instance { get; private set; }
    
    // Глобальная карта залов: координаты -> зал
    private Dictionary<Vector2I, Room> _rooms = new();
    
    // Позиция главного зала
    private Vector2I _mainHallPosition = new(0, 0);
    
    // Текущий зал, в котором находится игрок
    private Vector2I _currentRoomPosition = new(0, 0);
    
    // Инвентарь купленной, но ещё не размещённой мебели
    private List<Furniture> _pendingFurniture = new();
    
    // Режим размещения мебели
    private bool _isPlacementMode = false;
    private Furniture _furnitureToPlace;
    
    // Счётчик для уникальных ID
    private int _instanceCounter = 0;
    
    private double _incomeTimer = 0;
    private const double IncomeInterval = 1.0;
    
    public override void _Ready()
    {
        Instance = this;
        InitializeMainHall();
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
    
    // ===== ИНИЦИАЛИЗАЦИЯ ГЛАВНОГО ЗАЛА =====
    
    private void InitializeMainHall()
    {
        if (_rooms.Count > 0) return; // Уже инициализировано
        
        var mainHall = CreateRoom("main_hall", "Главный зал", _mainHallPosition, isMainHall: true);
        _rooms[_mainHallPosition] = mainHall;
        
        GD.Print("[Museum] Main hall initialized");
    }
    
    private Room CreateRoom(string id, string displayName, Vector2I globalPos, bool isMainHall = false)
    {
        var room = new Room
        {
            Id = id,
            DisplayName = displayName,
            GlobalPosition = globalPos,
            Width = 10,
            Height = 10,
            IsMainHall = isMainHall
        };
        
        room.InitializeGrid();
        
        // Создаём 4 двери (по середине каждой стены)
        room.Doors[Direction.Top] = new Door
        {
            Direction = Direction.Top,
            Position = new Vector2I(room.Width / 2, 0)
        };
        room.Doors[Direction.Right] = new Door
        {
            Direction = Direction.Right,
            Position = new Vector2I(room.Width - 1, room.Height / 2)
        };
        room.Doors[Direction.Bottom] = new Door
        {
            Direction = Direction.Bottom,
            Position = new Vector2I(room.Width / 2, room.Height - 1),
            IsExitToStreet = isMainHall // Только у главного зала есть выход на улицу
        };
        room.Doors[Direction.Left] = new Door
        {
            Direction = Direction.Left,
            Position = new Vector2I(0, room.Height / 2)
        };
        
        // Соединяем двери с соседними залами, если они есть
        ConnectDoorsWithNeighbors(room);
        
        return room;
    }
    
    private void ConnectDoorsWithNeighbors(Room room)
    {
        // Проверяем 4 соседних позиции
        var neighbors = new Dictionary<Direction, Vector2I>
        {
            { Direction.Top, new Vector2I(room.GlobalPosition.X, room.GlobalPosition.Y - 1) },
            { Direction.Right, new Vector2I(room.GlobalPosition.X + 1, room.GlobalPosition.Y) },
            { Direction.Bottom, new Vector2I(room.GlobalPosition.X, room.GlobalPosition.Y + 1) },
            { Direction.Left, new Vector2I(room.GlobalPosition.X - 1, room.GlobalPosition.Y) }
        };
        
        foreach (var kvp in neighbors)
        {
            if (_rooms.TryGetValue(kvp.Value, out var neighbor))
            {
                // Соединяем двери
                room.Doors[kvp.Key].ConnectedRoomId = neighbor.Id;
                
                // Противоположное направление у соседа
                Direction opposite = kvp.Key switch
                {
                    Direction.Top => Direction.Bottom,
                    Direction.Bottom => Direction.Top,
                    Direction.Left => Direction.Right,
                    Direction.Right => Direction.Left,
                    _ => Direction.Top
                };
                
                neighbor.Doors[opposite].ConnectedRoomId = room.Id;
            }
        }
    }
    
    // ===== ПРАВИЛА СТРОИТЕЛЬСТВА =====
    
    public bool CanBuildRoomAt(Vector2I globalPos)
    {
        // Уже занятая позиция
        if (_rooms.ContainsKey(globalPos)) return false;
        
        // Нельзя строить ниже главного зала и его горизонтальных соседей
        if (IsForbiddenZone(globalPos)) return false;
        
        // Зал должен примыкать к существующему залу (иметь хотя бы одного соседа)
        if (!HasAdjacentRoom(globalPos)) return false;
        
        return true;
    }
    
    private bool IsForbiddenZone(Vector2I pos)
    {
        // Запретная зона: все клетки с Y > 0, которые находятся
        // под главным залом или под его горизонтальными соседями
        
        // Главный зал
        Vector2I mainPos = _mainHallPosition;
        
        // Если позиция ниже главного зала
        if (pos.Y > mainPos.Y && pos.X == mainPos.X) return true;
        
        // Если позиция ниже левого соседа главного зала
        Vector2I leftNeighbor = new(mainPos.X - 1, mainPos.Y);
        if (_rooms.ContainsKey(leftNeighbor) && pos.Y > leftNeighbor.Y && pos.X == leftNeighbor.X) return true;
        
        // Если позиция ниже правого соседа главного зала
        Vector2I rightNeighbor = new(mainPos.X + 1, mainPos.Y);
        if (_rooms.ContainsKey(rightNeighbor) && pos.Y > rightNeighbor.Y && pos.X == rightNeighbor.X) return true;
        
        return false;
    }
    
    private bool HasAdjacentRoom(Vector2I pos)
    {
        return _rooms.ContainsKey(new Vector2I(pos.X + 1, pos.Y)) ||
               _rooms.ContainsKey(new Vector2I(pos.X - 1, pos.Y)) ||
               _rooms.ContainsKey(new Vector2I(pos.X, pos.Y + 1)) ||
               _rooms.ContainsKey(new Vector2I(pos.X, pos.Y - 1));
    }
    
    // ===== ПОКУПКА И РАЗМЕЩЕНИЕ ЗАЛОВ =====
    
    public const int RoomBuyPrice = 5000;
    
    public bool TryBuyRoom(Vector2I globalPos)
    {
        if (!CanBuildRoomAt(globalPos)) return false;
        if (!Wallet.Instance.SpendCoins(RoomBuyPrice)) return false;
        
        string id = $"room_{globalPos.X}_{globalPos.Y}";
        string displayName = $"Зал ({globalPos.X}, {globalPos.Y})";
        
        var room = CreateRoom(id, displayName, globalPos);
        _rooms[globalPos] = room;
        
        GD.Print($"[Museum] Bought new room at {globalPos}");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }
    
    // ===== ПОКУПКА И РАЗМЕЩЕНИЕ МЕБЕЛИ =====
    
    public List<FurnitureTemplate> GetAvailableFurnitureTemplates()
    {
        return new List<FurnitureTemplate>
        {
            new FurnitureTemplate { TypeId = "display_case_1x1", DisplayName = "Малая витрина", Size = new Vector2I(1, 1), BuyPrice = 200, CreateFunc = () => new DisplayCase { TypeId = "display_case_1x1", DisplayName = "Малая витрина", Size = new Vector2I(1, 1), BuyPrice = 200, Capacity = 5 } },
            new FurnitureTemplate { TypeId = "display_case_2x1", DisplayName = "Большая витрина", Size = new Vector2I(2, 1), BuyPrice = 400, CreateFunc = () => new DisplayCase { TypeId = "display_case_2x1", DisplayName = "Большая витрина", Size = new Vector2I(2, 1), BuyPrice = 400, Capacity = 10 } },
            new FurnitureTemplate { TypeId = "pedestal_2x2", DisplayName = "Малый пьедестал", Size = new Vector2I(2, 2), BuyPrice = 600, CreateFunc = () => new Pedestal { TypeId = "pedestal_2x2", DisplayName = "Малый пьедестал", Size = new Vector2I(2, 2), BuyPrice = 600 } },
            new FurnitureTemplate { TypeId = "pedestal_3x3", DisplayName = "Большой пьедестал", Size = new Vector2I(3, 3), BuyPrice = 1500, CreateFunc = () => new Pedestal { TypeId = "pedestal_3x3", DisplayName = "Большой пьедестал", Size = new Vector2I(3, 3), BuyPrice = 1500 } }
        };
    }
    
    public bool TryBuyFurniture(string typeId)
    {
        var template = GetAvailableFurnitureTemplates().Find(t => t.TypeId == typeId);
        if (template == null) return false;
        if (!Wallet.Instance.SpendCoins(template.BuyPrice)) return false;
        
        var furniture = template.CreateFunc();
        _pendingFurniture.Add(furniture);
        
        GD.Print($"[Museum] Bought {template.DisplayName}");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }
    
    public List<Furniture> GetPendingFurniture() => _pendingFurniture;
    
    // Режим размещения
    public void StartPlacementMode(Furniture furniture)
    {
        _isPlacementMode = true;
        _furnitureToPlace = furniture;
        GD.Print($"[Museum] Placement mode started for {furniture.DisplayName}");
    }
    
    public void CancelPlacementMode()
    {
        _isPlacementMode = false;
        _furnitureToPlace = null;
    }
    
    public bool IsInPlacementMode() => _isPlacementMode;
    public Furniture GetFurnitureToPlace() => _furnitureToPlace;
    
    public bool CanPlaceFurnitureAt(Room room, Vector2I position, Vector2I size)
    {
        return room.CanPlaceFurniture(position, size);
    }
    
    public bool PlaceFurniture(Room room, Furniture furniture, Vector2I position)
    {
        if (!room.CanPlaceFurniture(position, furniture.Size)) return false;
        
        var placed = new PlacedFurniture
        {
            InstanceId = $"furn_{_instanceCounter++}",
            FurnitureTypeId = furniture.TypeId,
            Position = position,
            Size = furniture.Size,
            Furniture = furniture
        };
        
        room.PlaceFurniture(placed);
        _pendingFurniture.Remove(furniture);
        
        GD.Print($"[Museum] Placed {furniture.DisplayName} at ({position.X}, {position.Y}) in {room.DisplayName}");
        SaveSystem.Instance?.MarkDirty();
        
        _isPlacementMode = false;
        _furnitureToPlace = null;
        
        return true;
    }
    
    public bool SellFurniture(Room room, PlacedFurniture placed)
    {
        int refund = placed.Furniture.SellPrice;
        
        // Возвращаем все экспонаты в инвентарь
        foreach (var item in placed.Furniture.GetAllItems())
        {
            InventorySystem.Instance.AddItem(item.ResourceId, item.Quality, item.Amount);
        }
        
        Wallet.Instance.AddCoins(refund);
        room.RemoveFurniture(placed);
        
        GD.Print($"[Museum] Sold {placed.Furniture.DisplayName} for {refund} coins");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }
    
    // ===== НАВИГАЦИЯ =====
    
    public Room GetCurrentRoom() => _rooms[_currentRoomPosition];
    
    public void SetCurrentRoom(Vector2I pos)
    {
        if (_rooms.ContainsKey(pos))
        {
            _currentRoomPosition = pos;
        }
    }
    
    public void EnterDoor(Room room, Direction direction)
    {
        var door = room.GetDoor(direction);
        if (door == null) return;
        
        if (door.IsExitToStreet)
        {
            GD.Print("[Museum] Exit to street (not implemented yet)");
            return;
        }
        
        if (!string.IsNullOrEmpty(door.ConnectedRoomId))
        {
            // Находим зал по ID
            var targetRoom = _rooms.Values.FirstOrDefault(r => r.Id == door.ConnectedRoomId);
            if (targetRoom != null)
            {
                _currentRoomPosition = targetRoom.GlobalPosition;
                GD.Print($"[Museum] Entered room {targetRoom.DisplayName}");
            }
        }
        else
        {
            // Двери нет — можно купить зал
            Vector2I newPos = direction switch
            {
                Direction.Top => new Vector2I(room.GlobalPosition.X, room.GlobalPosition.Y - 1),
                Direction.Bottom => new Vector2I(room.GlobalPosition.X, room.GlobalPosition.Y + 1),
                Direction.Left => new Vector2I(room.GlobalPosition.X - 1, room.GlobalPosition.Y),
                Direction.Right => new Vector2I(room.GlobalPosition.X + 1, room.GlobalPosition.Y),
                _ => room.GlobalPosition
            };
            
            if (CanBuildRoomAt(newPos))
            {
                GD.Print($"[Museum] Can buy room at {newPos} for {RoomBuyPrice} coins");
                // UI должен показать окно покупки
            }
        }
    }
    
    // ===== РАСЧЁТ ДОХОДА =====
    
    public int GetTotalIncomePerSecond()
    {
        int total = 0;
        
        foreach (var room in _rooms.Values)
        {
            foreach (var placed in room.PlacedFurnitureList)
            {
                foreach (var item in placed.Furniture.GetAllItems())
                {
                    var resource = GameData.GetResource(item.ResourceId);
                    if (resource == null) continue;
                    
                    float multiplier = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(item.Quality);
                    int baseIncome = (int)(resource.BaseMuseumIncome * multiplier);
                    
                    if (placed.Furniture is Pedestal pedestal && pedestal.IsComplete())
                    {
                        var collection = GameData.GetCollection(pedestal.CurrentCollectionId);
                        if (collection != null) baseIncome = (int)(baseIncome * collection.CollectionBonus);
                    }
                    
                    total += baseIncome;
                }
            }
        }
        
        return total;
    }
    
    private void GenerateIncome()
    {
        int totalIncome = GetTotalIncomePerSecond();
        if (totalIncome > 0) Wallet.Instance.AddCoins(totalIncome);
    }
    
    // ===== ГЕТТЕРЫ =====
    
    public List<Room> GetAllRooms() => _rooms.Values.ToList();
    public Room GetRoomAt(Vector2I pos) => _rooms.TryGetValue(pos, out var room) ? room : null;
    
    // ===== СОХРАНЕНИЕ =====
    
    public MuseumSaveData GetSaveData()
    {
        var data = new MuseumSaveData
        {
            CurrentRoomX = _currentRoomPosition.X,
            CurrentRoomY = _currentRoomPosition.Y,
            Rooms = _rooms.Values.Select(r => r.GetSaveData()).ToList()
        };
        return data;
    }
    
    public void LoadFromSaveData(MuseumSaveData data)
    {
        if (data == null) return;
        
        _rooms.Clear();
        _currentRoomPosition = new Vector2I(data.CurrentRoomX, data.CurrentRoomY);
        
        foreach (var roomData in data.Rooms)
        {
            var room = new Room
            {
                Id = roomData.Id,
                DisplayName = $"Зал ({roomData.GlobalPositionX}, {roomData.GlobalPositionY})",
                GlobalPosition = new Vector2I(roomData.GlobalPositionX, roomData.GlobalPositionY),
                Width = 10,
                Height = 10,
                IsMainHall = roomData.Id == "main_hall"
            };
            
            room.InitializeGrid();
            
            // Восстанавливаем двери
            room.Doors[Direction.Top] = new Door { Direction = Direction.Top, Position = new Vector2I(5, 0) };
            room.Doors[Direction.Right] = new Door { Direction = Direction.Right, Position = new Vector2I(9, 5) };
            room.Doors[Direction.Bottom] = new Door { Direction = Direction.Bottom, Position = new Vector2I(5, 9), IsExitToStreet = room.IsMainHall };
            room.Doors[Direction.Left] = new Door { Direction = Direction.Left, Position = new Vector2I(0, 5) };
            
            if (roomData.Doors != null)
            {
                foreach (var kvp in roomData.Doors)
                {
                    var dir = (Direction)kvp.Key;
                    if (room.Doors.ContainsKey(dir))
                    {
                        room.Doors[dir].ConnectedRoomId = kvp.Value;
                    }
                }
            }
            
            // Восстанавливаем мебель
            if (roomData.Furniture != null)
            {
                foreach (var furnData in roomData.Furniture)
                {
                    Furniture furniture = null;
                    
                    if (furnData.FurnitureTypeId.StartsWith("display_case"))
                    {
                        var dc = new DisplayCase
                        {
                            TypeId = furnData.FurnitureTypeId,
                            DisplayName = furnData.FurnitureTypeId == "display_case_1x1" ? "Малая витрина" : "Большая витрина",
                            Size = new Vector2I(furnData.SizeX, furnData.SizeY),
                            BuyPrice = furnData.FurnitureTypeId == "display_case_1x1" ? 200 : 400,
                            Capacity = furnData.SizeX * 5
                        };
                        furniture = dc;
                    }
                    else if (furnData.FurnitureTypeId.StartsWith("pedestal"))
                    {
                        var ped = new Pedestal
                        {
                            TypeId = furnData.FurnitureTypeId,
                            DisplayName = furnData.FurnitureTypeId == "pedestal_2x2" ? "Малый пьедестал" : "Большой пьедестал",
                            Size = new Vector2I(furnData.SizeX, furnData.SizeY),
                            BuyPrice = furnData.FurnitureTypeId == "pedestal_2x2" ? 600 : 1500
                        };
                        furniture = ped;
                    }
                    
                    if (furniture != null)
                    {
                        var placed = new PlacedFurniture
                        {
                            InstanceId = furnData.InstanceId,
                            FurnitureTypeId = furnData.FurnitureTypeId,
                            Position = new Vector2I(furnData.PositionX, furnData.PositionY),
                            Size = new Vector2I(furnData.SizeX, furnData.SizeY),
                            Furniture = furniture
                        };

                        if (furnData.FurnitureSaveData?.DisplayCaseItems != null)
            {
                placed.Items.Clear(); // На всякий случай очищаем
                placed.Items.AddRange(furnData.FurnitureSaveData.DisplayCaseItems);
            }
                        
                        room.PlacedFurnitureList.Add(placed);
                        
                        // Обновляем счётчик ID
                        if (int.TryParse(furnData.InstanceId.Replace("furn_", ""), out int id))
                        {
                            _instanceCounter = Math.Max(_instanceCounter, id + 1);
                        }
                    }
                }
                
                // Пересчитываем occupancy grid
                foreach (var placed in room.PlacedFurnitureList)
                {
                    for (int x = placed.Position.X - 1; x <= placed.Position.X + placed.Size.X; x++)
                    {
                        for (int y = placed.Position.Y - 1; y <= placed.Position.Y + placed.Size.Y; y++)
                        {
                            if (x >= 0 && x < room.Width && y >= 0 && y < room.Height)
                            {
                                // Используем прямой доступ через IsCellOccupied-подобную логику
                                // (тут можно оптимизировать, но для простоты оставим так)
                            }
                        }
                    }
                }
            }
            
            _rooms[room.GlobalPosition] = room;
        }
        
        GD.Print($"[Museum] Loaded {_rooms.Count} rooms");
    }
        // ===== ВЗАИМОДЕЙСТВИЕ UI С МЕБЕЛЬЮ =====
    
    public bool TryAddItemToFurniture(Room room, PlacedFurniture placed, string resourceId, Quality quality)
{
    // 1. Находим РЕАЛЬНЫЙ объект в списке комнаты, чтобы исключить работу с копией
    var realPlaced = room.PlacedFurnitureList.FirstOrDefault(p => p.InstanceId == placed.InstanceId);
    
    if (realPlaced == null)
    {
        GD.PrintErr($"[MuseumSystem] КРИТИЧЕСКАЯ ОШИБКА: Не удалось найти мебель с ID {placed.InstanceId} в комнате!");
        return false;
    }

    // 2. Создаем новый предмет
    var newItem = new FoundItem(resourceId, quality, 1);

    // 3. Пытаемся добавить его в РЕАЛЬНЫЙ объект
    bool added = realPlaced.AddItem(newItem);
    
    if (added)
    {
        GD.Print($"[MuseumSystem] УСПЕХ: Добавлен {resourceId} в витрину {realPlaced.InstanceId}. Всего предметов: {realPlaced.GetAllItems().Count}");
    }
    else
    {
        GD.PrintErr($"[MuseumSystem] ОТКАЗ: Метод AddItem вернул false для витрины {realPlaced.InstanceId} (возможно, превышен лимит)");
    }
    
    return added;
}

    public bool TryReturnItemFromFurniture(Room room, PlacedFurniture placed, string resourceId, Quality quality)
    {
        var removed = placed.RemoveItem(resourceId, quality);
        if (removed != null)
        {
            InventorySystem.Instance.AddItem(removed.ResourceId, removed.Quality, removed.Amount);
            SaveSystem.Instance?.MarkDirty();
            return true;
        }
        return false;
    }
}

// ===== ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ =====

public class FurnitureTemplate
{
    public string TypeId;
    public string DisplayName;
    public Vector2I Size;
    public int BuyPrice;
    public Func<Furniture> CreateFunc;
}