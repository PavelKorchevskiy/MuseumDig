using Godot;
using System;
using System.Collections.Generic;

public partial class Museum : Node2D
{
    private GlobalRoomViewUI _globalRoomView;

    private MuseumShopUI _shopUI;
    private bool _initialDisplayUpdated = false;
    private DisplayCaseUI _displayCaseUI;
    private CollectionExhibitUI _collectionExhibitUI;
    private ProgressUI _progressUI;
    private MuseumBackground _background;
    private MuseumTopBar _topBar;
private MuseumBottomBar _bottomBar;
private OfflineIncomeUI _offlineIncomeUI;

    // Таймстамп выхода со сцены музея (при переходе на раскопки) — хранится в
    // static-поле, т.к. сам Node уничтожается при смене сцены. Используется
    // для догонки дохода посетителей, пока игрок был на раскопках: та же
    // идея, что и офлайн-доход (SaveSystem/MuseumSystem.CalculateOfflineIncome),
    // но без деления пополам и привязана не к закрытию приложения, а к
    // переключению сцены внутри сессии.
    private static long _lastExitTimestamp = 0;


    public override void _Ready()
    {
         _background = new MuseumBackground { Name = "MuseumBackground" };
        AddChild(_background);
        _background.Setup(this);
        // 1. Создаем или получаем UI-слой с высоким приоритетом
        var uiLayer = GetNodeOrNull<CanvasLayer>("UI");
        if (uiLayer == null)
        {
            uiLayer = new CanvasLayer { Name = "UI", Layer = 100 };
            AddChild(uiLayer);
        }

        // 3. Магазин
        _shopUI = new MuseumShopUI { Name = "MuseumShop" };
        uiLayer.AddChild(_shopUI);
        _shopUI.Visible = false;

        // 4. ГЛОБАЛЬНЫЙ вид комнаты (теперь он сам управляет отрисовкой мебели внутри себя)
        _globalRoomView = new GlobalRoomViewUI { Name = "GlobalRoomView" };
        AddChild(_globalRoomView);

        // 5. Камера (используем синглтон MuseumLayout)
        var camera = GetNodeOrNull<CameraController>("Camera2D");
        if (camera != null)
        {
            Vector2I mainHallCenter = MuseumLayout.Instance.GetRoomGlobalCenter("main_hall");
            camera.SnapToRoomCenter(mainHallCenter);
        }
        

        _displayCaseUI = new DisplayCaseUI { Name = "DisplayCaseUI" };
        uiLayer.AddChild(_displayCaseUI);
        _collectionExhibitUI = new CollectionExhibitUI { Name = "CollectionExhibitUI" };
        uiLayer.AddChild(_collectionExhibitUI);

         LocalizationManager.SetLanguage("ru"); // или "en"

         _progressUI = new ProgressUI { Name = "ProgressUI" };
        uiLayer.AddChild(_progressUI);

        // Экран ежедневных заданий и достижений (открывается кнопкой в нижней панели)
        uiLayer.AddChild(new QuestsUI { Name = "QuestsUI" });

         _bottomBar = new MuseumBottomBar();
        uiLayer.AddChild(_bottomBar);

        _topBar = new MuseumTopBar();
        uiLayer.AddChild(_topBar);

        // Попап офлайн-дохода - создаём поверх всего остального в этом же UI-слое
        _offlineIncomeUI = new OfflineIncomeUI(uiLayer);

        // === Догонка дохода посетителей за время на сцене раскопок ===
        // Пока игрок копал, музей не тикал (сцена была выгружена) — считаем,
        // сколько посетителей зашло бы и доплачиваем монетами одним куском,
        // ровно как делает офлайн-доход при закрытии приложения.
        if (_lastExitTimestamp > 0)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long elapsed = Math.Max(0, now - _lastExitTimestamp);
            _lastExitTimestamp = 0;

            int catchUpIncome = MuseumSystem.Instance?.CalculateVisitorCatchUpIncome(elapsed) ?? 0;
            if (catchUpIncome > 0)
            {
                Wallet.Instance?.AddCoins(catchUpIncome);

                // Догонка идёт в зачёт заданий так же, как обычные входы посетителей
                int ticketPrice = Math.Max(1, MuseumSystem.Instance.GetVisitorTicketPrice());
                QuestSystem.Instance.OnVisitorIncome(catchUpIncome, catchUpIncome / ticketPrice);

                SaveSystem.Instance?.MarkDirty();
                GD.Print($"[Museum] 🚶 Догонка посетителей: +{catchUpIncome} монет (прошло {elapsed} сек на раскопках)");
            }

            // === Сразу заселяем музей, а не ждём, пока новые посетители дойдут от входа ===
            // Старые Visitor-узлы были уничтожены вместе со сценой музея при переходе
            // на раскопки. Чем дольше игрок был на раскопках, тем ближе музей к своей
            // обычной заполненности (при elapsed >= BaseVisitDuration он уже был бы полон).
            int maxVisitors = MuseumSystem.Instance?.CalculateMaxVisitors() ?? 0;
            float fillFraction = Mathf.Min(1f, (float)elapsed / Visitor.BaseVisitDuration);
            int targetVisitorCount = Mathf.RoundToInt(maxVisitors * fillFraction);
            VisitorManager.Instance?.SpawnCatchUpVisitors(targetVisitorCount);
        }
    }

    public override void _ExitTree()
    {
        // Запоминаем момент выхода со сцены музея — используется при следующем
        // входе, чтобы досчитать доход посетителей (см. _Ready выше).
        _lastExitTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public override void _Process(double delta)
    {
        if (!_initialDisplayUpdated)
        {
            _initialDisplayUpdated = true;
            UpdateDisplay();
            RefreshRoomView();

            int offlineIncome = SaveSystem.Instance?.ConsumePendingOfflineIncome() ?? 0;
            if (offlineIncome > 0)
            {
                _offlineIncomeUI?.Show(offlineIncome);
            }
        }
            VisitorManager.Instance?.ProcessDelta((float)delta);
    }

    private void UpdateDisplay()
    {
        if (_globalRoomView != null)
        {
            // Устанавливаем активную комнату в синглтоне и обновляем вид
            MuseumLayout.Instance.ActiveRoomId = "main_hall";
            _globalRoomView.UpdateRoomVisibility();
        }
    }
    

    /// <summary>
    /// Вызывается для обновления затемнения комнат
    /// </summary>
    public void SetActiveRoom(string roomId)
    {
        if (_globalRoomView != null)
        {
            MuseumLayout.Instance.ActiveRoomId = roomId;
             _globalRoomView.UpdateRoomVisibility();
        }
    }

    /// <summary>
    /// Обновляет глобальный вид (включая мебель, которая теперь рендерится внутри GlobalRoomViewUI)
    /// </summary>
    public void RefreshRoomView()
    {
        if (_globalRoomView != null)
        {
            _globalRoomView.RenderAllRooms();
        }
    }

    /// <summary>
    /// Устаревший метод, оставлен для совместимости, если где-то вызывается.
    /// Теперь логика перехода обрабатывается кликом по полу в GlobalRoomViewUI.
    /// </summary>
    public void OnDoorClicked(string targetRoomId)
    {
        if (string.IsNullOrEmpty(targetRoomId)) return;

        SetActiveRoom(targetRoomId);

        var camera = GetNodeOrNull<CameraController>("Camera2D"); 
        if (camera != null)
        {
            Vector2I center = MuseumLayout.Instance.GetRoomGlobalCenter(targetRoomId);
            camera.MoveToRoomCenter(center);
        }
    }

    /// <summary>
    /// Показывает/скрывает нижнюю навигационную панель музея. Прячем её на
    /// время режима размещения: её кнопки лежат на CanvasLayer с Layer=100,
    /// выше внутреннего слоя PlacementModeUI (Layer=50) — физически
    /// перекрывают его "Отмена"/"Поставить" и перехватывают тапы по ним.
    /// </summary>
    public void SetBottomBarVisible(bool visible)
    {
        if (_bottomBar != null) _bottomBar.Visible = visible;
    }

    /// <summary>
    /// Открывает боковую панель управления витриной
    /// </summary>
    public void OpenDisplayCaseUI(Room room, PlacedFurniture placed)
    {
        if (_displayCaseUI != null && placed.Furniture is DisplayCase)
        {
            _displayCaseUI.Open(room, placed);
        }
    }

    /// <summary>
    /// Вызывается из DisplayCaseUI для продажи
    /// </summary>
    public void SellCurrentDisplayCase(string roomId, PlacedFurniture placed)
    {
        if (MuseumSystem.Instance.SellFurnitureWithItems(roomId, placed))
        {
            RefreshRoomView(); // Перерисовываем, чтобы витрина исчезла
        }
    }

        /// <summary>
    /// Запускает режим перемещения для УЖЕ СУЩЕСТВУЮЩЕЙ витрины
    /// </summary>
    public void StartMovingFurniture(string roomId, PlacedFurniture placedToMove)
    {
        GD.Print($"[Museum] 🚀 Получен объект для перемещения. TypeId: {placedToMove.FurnitureTypeId}, Экспонатов: {placedToMove.Items.Count}");

        var currentScene = GetTree().CurrentScene;
        var placementUI = currentScene.GetNodeOrNull<PlacementModeUI>("PlacementModeUI");
        
        if (placementUI == null)
        {
            placementUI = new PlacementModeUI();
            placementUI.Name = "PlacementModeUI";
            currentScene.AddChild(placementUI);
        }
        
        var room = MuseumLayout.Instance.GetRoom(roomId);
        // Передаем мебель и сам объект PlacedFurniture
        placementUI.StartPlacementForMoving(room, placedToMove.Furniture, placedToMove);
    }

     public void OnInventoryPlaceCollection(string collectionId)
    {
        GD.Print($"[Museum] Запрошено размещение коллекции: {collectionId}");
        MuseumSystem.Instance.StartPlacementFromInventory(collectionId);
    }

    /// <summary>
    /// Открывает панель управления для собранной коллекции (скелета)
    /// </summary>
    public void OpenCollectionExhibitUI(Room room, PlacedFurniture placed)
    {
        if (_collectionExhibitUI != null && placed.Furniture is CollectionExhibit)
        {
            _collectionExhibitUI.Open(room, placed);
        }
    }

    /// <summary>
    /// Убирает коллекцию со стены и возвращает её в инвентарь
    /// </summary>
    public void StoreCollectionExhibit(string roomId, PlacedFurniture placed)
    {
        var room = MuseumLayout.Instance.GetRoom(roomId);
        if (room != null)
        {
            // Возвращаем предмет в инвентарь с сохраненным качеством
            InventorySystem.Instance.AddItem(placed.FurnitureTypeId, 1);
            
            // Удаляем со стены
            room.RemoveFurniture(placed);
            
            GD.Print($"[Museum] Экспонат '{placed.Furniture.DisplayName}' убран на склад.");
            SaveSystem.Instance?.MarkDirty();
            
            RefreshRoomView(); // Перерисовываем, чтобы скелет исчез
        }
    }
}