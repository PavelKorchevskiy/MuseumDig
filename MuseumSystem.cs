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
    private const int BaseVisitorTicketPrice = 50;



    public override void _Ready()
    {
        Instance = this;
        // Инициализируем Layout, если он еще не создан
        if (MuseumLayout.Instance == null) _ = new MuseumLayout();
    }

    public List<FurnitureTemplate> GetAvailableFurnitureTemplates()
    {
        return new List<FurnitureTemplate>
        {
            new FurnitureTemplate { TypeId = "display_case_1x1", DisplayName = "Малая витрина", Size = new Vector2I(1, 1), BuyPrice = 200, CreateFunc = () => new DisplayCase { TypeId = "display_case_1x1", DisplayName = "Малая витрина", Size = new Vector2I(1, 1), BuyPrice = 200, Capacity = 5 } },
            new FurnitureTemplate { TypeId = "display_case_2x2", DisplayName = "Большая витрина", Size = new Vector2I(2, 2), BuyPrice = 400, CreateFunc = () => new DisplayCase { TypeId = "display_case_2x2", DisplayName = "Большая витрина", Size = new Vector2I(2, 2), BuyPrice = 400, Capacity = 10 } },
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
    public void StartPlacementFromInventory(string collectionId)
    {
        var collectionDef = GameData.GetCollection(collectionId);
        if (collectionDef == null) 
        {
            GD.PrintErr($"[MuseumSystem] Коллекция {collectionId} не найдена в GameData!");
            return;
        }

        // 1. Создаем объект мебели (шаблон)
        var exhibit = new CollectionExhibit(collectionDef);

        // 2. Временно списываем из инвентаря (вернем, если игрок нажмет Esc)
        InventorySystem.Instance.RemoveItem(collectionId, 1);
        _pendingCollectionToReturn = new FoundItem(collectionId, 1);
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
        
        GD.Print($"[MuseumSystem] 🚀 Размещение коллекции '{collectionId}' запущено");
    }

    /// <summary>
    /// Обновленный метод отмены: теперь он возвращает коллекцию в инвентарь
    /// </summary>
    public void CancelPlacementMode() // 'new' не нужен, просто замените старый метод
    {
        // Если мы размещали коллекцию из инвентаря и отменили действие — возвращаем предмет
        if (_pendingCollectionToReturn != null)
        {
            InventorySystem.Instance.AddItem(_pendingCollectionToReturn.ResourceId, _pendingCollectionToReturn.Amount);
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
            Items = new List<FoundItem>(),
        };
        
        // Сбрасываем качество после использования, чтобы не повлияло на следующие покупки

        room.PlaceFurniture(placed);
        if (_pendingCollectionToReturn != null && furniture is CollectionExhibit)
        {
            _pendingCollectionToReturn = null;
            GD.Print("[MuseumSystem] ✅ Коллекция успешно размещена, возврат в инвентарь отменен.");
        }
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
            InventorySystem.Instance.AddItem(item.ResourceId, item.Amount);
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

        public bool SellFurnitureWithItems(string roomId, PlacedFurniture placed)
    {
        var room = MuseumLayout.Instance.GetRoom(roomId);
        if (room == null) return false;

        // 1. Возвращаем все предметы из витрины в инвентарь
        if (placed.Items != null)
        {
            foreach (var item in placed.Items)
            {
                InventorySystem.Instance.AddItem(item.ResourceId, item.Amount);
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

       /// <summary>
    /// Рассчитывает максимальное количество посетителей на основе ТОЛЬКО скелетов (CollectionExhibit)
    /// С учётом модификаторов из дерева навыков
    /// </summary>
    public int CalculateMaxVisitors()
    {
        int maxVisitors = 0;
        
        // Получаем модификаторы из дерева навыков
        float commonBonus = SkillSystem.Instance.GetModifier("max_visitors_common_uncommon");
        float rareBonus = SkillSystem.Instance.GetModifier("max_visitors_rare");
        float epicLegendaryBonus = SkillSystem.Instance.GetModifier("max_visitors_epic_legendary");
        
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            if (!room.IsUnlocked) continue;

            foreach (var placed in room.PlacedFurnitureList)
            {
                if (placed.Furniture is CollectionExhibit)
                {
                    var collection = GameData.GetCollection(placed.FurnitureTypeId);
                    if (collection != null)
                    {
                        int baseVisitors = collection.Rarity switch
                        {
                            Rarity.Common => 1,
                            Rarity.Uncommon => 3,
                            Rarity.Rare => 6,
                            Rarity.Epic => 9,
                            Rarity.Legendary => 20,
                            _ => 1
                        };
                        
                        // Применяем модификаторы
                        int bonus = 0;
                        if (collection.Rarity == Rarity.Common || collection.Rarity == Rarity.Uncommon)
                        {
                            bonus = (int)commonBonus;
                        }
                        else if (collection.Rarity == Rarity.Rare)
                        {
                            bonus = (int)rareBonus;
                        }
                        else if (collection.Rarity == Rarity.Epic || collection.Rarity == Rarity.Legendary)
                        {
                            bonus = (int)epicLegendaryBonus;
                        }
                        
                        maxVisitors += baseVisitors + bonus;
                    }
                }
            }
        }

        return 3 + maxVisitors; 
    }

        public void OnVisitorEntered()
    {
        float ticketPriceModifier = SkillSystem.Instance.GetModifier("visitor_ticket_price");
        int ticketPrice = ticketPriceModifier > 0 ? (int)ticketPriceModifier : BaseVisitorTicketPrice;
        
        Wallet.Instance.AddCoins(ticketPrice);
        
        // === Шанс получить билет на раскопки ===
        float ticketChance = SkillSystem.Instance.GetModifier("ticket_drop_chance");
        if (ticketChance > 0 && GD.Randf() < ticketChance)
        {
            // Пока просто логируем, позже добавим реальную систему билетов
            GD.Print($"[Museum] 🎫 Посетитель дал билет на раскопки! (шанс: {ticketChance * 100}%)");
            // TODO: InventorySystem.Instance.AddItem("digging_ticket", 1);
        }
        
        GD.Print($"[Museum] Посетитель вошел! +{ticketPrice} монет");
    }

    /// <summary>
    /// Создаёт всплывающий текст над главным входом
    /// </summary>
    private void SpawnFloatingText(string text, Color color)
    {
        // Временная реализация - просто выводим в консоль
        // Полноценный UI с анимацией добавим позже
        GD.Print($"[FloatingText] {text}");
    }

     /// <summary>
    /// Возвращает расчетный доход в секунду при полной загрузке музея.
    /// Используется для UI и расчета офлайн-наград.
    /// </summary>
    public int GetEstimatedIncomePerSecond()
    {
        int maxVisitors = CalculateMaxVisitors();
        
        // Формула: (Макс. посетителей / Интервал спавна) * Цена билета
        // Если интервал спавна 3 сек, а билет 50 монет:
        const int spawnInterval = 3;
        const int ticketPrice = 50;
        
        return (maxVisitors * ticketPrice) / spawnInterval;
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