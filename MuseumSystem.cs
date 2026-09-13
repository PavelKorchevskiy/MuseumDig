using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class MuseumSystem : Node
{
    public static MuseumSystem Instance { get; private set; }

    private List<Furniture> _pendingFurniture = new();
    private bool _isPlacementMode = false;
    private Furniture _furnitureToPlace;
    private PlacedFurniture _furnitureToMove;
    private FoundItem _pendingCollectionToReturn = null;
    private int _instanceCounter = 0;
    private double _incomeTimer = 0;

    public override void _Ready()
    {
        Instance = this;
        // Инициализируем Layout, если он еще не создан
        if (MuseumLayout.Instance == null) _ = new MuseumLayout();
    }

    public override void _Process(double delta)
    {
        _incomeTimer += delta;
        if (_incomeTimer >= 1.0)
        {
            _incomeTimer = 0;
            GenerateIncome();
        }
    }

    public List<FurnitureTemplate> GetAvailableFurnitureTemplates()
    {
        return new List<FurnitureTemplate>
        {
            new FurnitureTemplate { TypeId = "display_case_1x1", DisplayName = "Малая витрина", Size = new Vector2I(1, 1), BuyPrice = 200, CreateFunc = () => new DisplayCase { TypeId = "display_case_1x1", DisplayName = "Малая витрина", Size = new Vector2I(1, 1), BuyPrice = 200, Capacity = 5 } },
            new FurnitureTemplate { TypeId = "display_case_2x1", DisplayName = "Большая витрина", Size = new Vector2I(2, 1), BuyPrice = 400, CreateFunc = () => new DisplayCase { TypeId = "display_case_2x1", DisplayName = "Большая витрина", Size = new Vector2I(2, 1), BuyPrice = 400, Capacity = 10 } },
        };
    }

    public bool TryBuyFurniture(string typeId)
    {
        var template = GetAvailableFurnitureTemplates().Find(t => t.TypeId == typeId);
        if (template == null || !Wallet.Instance.SpendCoins(template.BuyPrice)) return false;

        _pendingFurniture.Add(template.CreateFunc());
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    public List<Furniture> GetPendingFurniture()
    {
        return _pendingFurniture;
    }

    public Room GetCurrentRoom()
    {
        // Получаем текущую активную комнату напрямую из Layout по её ID
        return MuseumLayout.Instance.GetRoom(MuseumLayout.Instance.ActiveRoomId);
    }

    public void StartPlacementMode(Furniture furniture)
    {
        _isPlacementMode = true;
        _furnitureToPlace = furniture;
    }

    


        /// <summary>
    /// Запускает режим размещения для собранной коллекции из инвентаря
    /// </summary>
    public void StartPlacementFromInventory(string collectionId, Quality quality)
    {
        var collectionDef = GameData.GetCollection(collectionId);
        if (collectionDef == null) 
        {
            GD.PrintErr($"[MuseumSystem] Коллекция {collectionId} не найдена в GameData!");
            return;
        }

        // 1. Создаем объект мебели
        var exhibit = new CollectionExhibit(collectionDef);

        // 2. Временно списываем из инвентаря (вернем, если игрок нажмет Esc)
        InventorySystem.Instance.RemoveItem(collectionId, quality, 1);
        _pendingCollectionToReturn = new FoundItem(collectionId, quality, 1);

        // 3. Запускаем режим размещения
        _isPlacementMode = true;
        _furnitureToPlace = exhibit;

        // 4. Активируем UI размещения
        var currentScene = GetTree().CurrentScene;
        var placementUI = currentScene.GetNodeOrNull<PlacementModeUI>("PlacementModeUI");
        
        if (placementUI == null)
        {
            placementUI = new PlacementModeUI();
            placementUI.Name = "PlacementModeUI";
            currentScene.AddChild(placementUI);
            GD.Print("[MuseumSystem] UI размещения создан");
        }
        
        var currentRoom = GetCurrentRoom();
        placementUI.StartPlacementForCollection(currentRoom, exhibit);
    }

    /// <summary>
    /// Обновленный метод отмены: теперь он возвращает коллекцию в инвентарь
    /// </summary>
    public void CancelPlacementMode() // 'new' не нужен, просто замените старый метод
    {
        // Если мы размещали коллекцию из инвентаря и отменили действие — возвращаем предмет
        if (_pendingCollectionToReturn != null)
        {
            InventorySystem.Instance.AddItem(_pendingCollectionToReturn.ResourceId, _pendingCollectionToReturn.Quality, _pendingCollectionToReturn.Amount);
            _pendingCollectionToReturn = null;
            GD.Print("[MuseumSystem] Коллекция возвращена в инвентарь после отмены");
        }

        _isPlacementMode = false;
        _furnitureToPlace = null;
    }

    // Принимает roomId, находит комнату в Layout и размещает мебель
    public bool PlaceFurniture(string roomId, Furniture furniture, Vector2I localPosition)
    {
        var room = MuseumLayout.Instance.GetRoom(roomId);
        if (room == null || !room.IsUnlocked || !room.CanPlaceFurniture(localPosition, furniture.Size)) return false;

        var placed = new PlacedFurniture
        {
            InstanceId = $"furn_{_instanceCounter++}",
            FurnitureTypeId = furniture.TypeId,
            Position = localPosition,
            Size = furniture.Size,
            Furniture = furniture,
            Items = new List<FoundItem>()
        };

        room.PlaceFurniture(placed);
        _pendingFurniture.Remove(furniture);
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    public void StartPlacementModeWithItems(Furniture furniture, PlacedFurniture placedToMove)
    {
        _isPlacementMode = true;
        _furnitureToPlace = furniture;
        _furnitureToMove = placedToMove; // Сохраняем для переноса
    }

    public bool SellFurniture(string roomId, PlacedFurniture placed)
    {
        var room = MuseumLayout.Instance.GetRoom(roomId);
        if (room == null) return false;

        int refund = placed.Furniture.SellPrice;
        foreach (var item in placed.Furniture.GetAllItems())
        {
            InventorySystem.Instance.AddItem(item.ResourceId, item.Quality, item.Amount);
        }

        Wallet.Instance.AddCoins(refund);
        room.RemoveFurniture(placed);
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    public MuseumSaveData GetSaveData()
    {
        return new MuseumSaveData
        {
            CurrentRoomId = MuseumLayout.Instance.ActiveRoomId,
            Rooms = MuseumLayout.Instance.Rooms.Select(r => r.GetSaveData()).ToList()
        };
    }

    public void LoadFromSaveData(MuseumSaveData data)
    {
        if (data == null) return;

        MuseumLayout.Instance.ActiveRoomId = string.IsNullOrEmpty(data.CurrentRoomId) ? "main_hall" : data.CurrentRoomId;

        foreach (var roomData in data.Rooms)
        {
            var room = MuseumLayout.Instance.GetRoom(roomData.Id);
            if (room == null) continue;

            room.IsUnlocked = roomData.IsUnlocked; // Восстанавливаем состояние открытия

            if (roomData.Furniture != null)
            {
                foreach (var furnData in roomData.Furniture)
                {
                    Furniture furniture = null;
                    if (furnData.FurnitureTypeId.StartsWith("display_case"))
                    {
                        furniture = new DisplayCase { TypeId = furnData.FurnitureTypeId, Size = new Vector2I(furnData.SizeX, furnData.SizeY), Capacity = furnData.SizeX * 5 };
                    }
                    else if (GameData.GetCollection(furnData.FurnitureTypeId) != null)
                    {
                        furniture = new CollectionExhibit(GameData.GetCollection(furnData.FurnitureTypeId));
                    }

                    if (furniture != null)
                    {
                        var placed = new PlacedFurniture
                        {
                            InstanceId = furnData.InstanceId,
                            FurnitureTypeId = furnData.FurnitureTypeId,
                            Position = new Vector2I(furnData.PositionX, furnData.PositionY),
                            Size = new Vector2I(furnData.SizeX, furnData.SizeY),
                            IsFlipped = furnData.FurnitureSaveData?.IsFlipped ?? false,
                            Furniture = furniture,
                            Items = furnData.FurnitureSaveData?.DisplayCaseItems ?? new List<FoundItem>()
                        };
                        room.PlacedFurnitureList.Add(placed);

                        if (int.TryParse(furnData.InstanceId.Replace("furn_", ""), out int id))
                            _instanceCounter = Math.Max(_instanceCounter, id + 1);
                    }
                }
            }
        }
        GD.Print($"[MuseumSystem] Загружено {MuseumLayout.Instance.Rooms.Count} комнат.");
    }

        // ===== РАСЧЁТ ДОХОДА (Публичный метод для UI) =====

    public int GetTotalIncomePerSecond()
    {
        int total = 0;
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            if (!room.IsUnlocked) continue; // Закрытые комнаты не приносят доход

            foreach (var placed in room.PlacedFurnitureList)
            {
                foreach (var item in placed.Furniture.GetAllItems())
                {
                    var resource = GameData.GetResource(item.ResourceId);
                    if (resource == null) continue;

                    float mult = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(item.Quality);
                    int baseIncome = (int)(resource.BaseMuseumIncome * mult);

                    var collection = GameData.GetCollection(placed.FurnitureTypeId);
                    if (collection != null)
                    {
                        baseIncome = (int)(baseIncome * collection.CollectionBonus);
                    }
                    total += baseIncome;
                }
            }
        }
        return total;
    }

    private void GenerateIncome()
    {
        // Теперь этот метод просто использует публичный расчёт и выплачивает деньги
        int totalIncome = GetTotalIncomePerSecond();
        if (totalIncome > 0) 
        {
            Wallet.Instance.AddCoins(totalIncome);
        }
    }

        public bool SellFurnitureWithItems(string roomId, PlacedFurniture placed)
    {
        var room = MuseumLayout.Instance.GetRoom(roomId);
        if (room == null) return false;

        // 1. Возвращаем все предметы из витрины в инвентарь
        if (placed.Items != null)
        {
            foreach (var item in placed.Items)
            {
                InventorySystem.Instance.AddItem(item.ResourceId, item.Quality, item.Amount);
            }
        }

        // 2. Возвращаем деньги за саму витрину
        int refund = placed.Furniture.SellPrice;
        Wallet.Instance.AddCoins(refund);

        // 3. Удаляем витрину из комнаты
        room.RemoveFurniture(placed);
        
        GD.Print($"[MuseumSystem] Витрина продана за {refund} монет. Предметы возвращены в инвентарь.");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }
}

public class FurnitureTemplate
{
    public string TypeId;
    public string DisplayName;
    public Vector2I Size;
    public int BuyPrice;
    public Func<Furniture> CreateFunc;
}