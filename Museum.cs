using Godot;
using System.Collections.Generic;

public partial class Museum : Node2D
{
    private GlobalRoomViewUI _globalRoomView;
    
    private CanvasLayer _offlineReward;
    private MuseumShopUI _shopUI;
    private VBoxContainer _buttonPanel;
    
    private bool _initialDisplayUpdated = false;
    private bool _offlineRewardChecked = false;
    private DisplayCaseUI _displayCaseUI;

    public override void _Ready()
    {
        // 1. Создаем или получаем UI-слой с высоким приоритетом
        var uiLayer = GetNodeOrNull<CanvasLayer>("UI");
        if (uiLayer == null)
        {
            uiLayer = new CanvasLayer { Name = "UI", Layer = 100 };
            AddChild(uiLayer);
        }

        // 2. Офлайн-награда
        _offlineReward = GetNodeOrNull<CanvasLayer>("UI/OfflineReward");
        if (_offlineReward != null) _offlineReward.Visible = false;

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
        
        // 6. Панель кнопок
        CreateButtonPanel(uiLayer);

        _displayCaseUI = new DisplayCaseUI { Name = "DisplayCaseUI" };
        uiLayer.AddChild(_displayCaseUI);

    }

    public override void _Process(double delta)
    {
        if (!_initialDisplayUpdated)
        {
            _initialDisplayUpdated = true;
            UpdateDisplay();
            
            if (SaveSystem.Instance != null && SaveSystem.Instance.GetLastSaveTimestamp() > 0)
            {
                if (!_offlineRewardChecked)
                {
                    _offlineRewardChecked = true;
                    CheckOfflineReward();
                }
            }
        }
        RefreshRoomView();
    }

    private void CreateButtonPanel(CanvasLayer uiLayer)
    {
        _buttonPanel = new VBoxContainer();
        _buttonPanel.Position = new Vector2(20, 60);
        _buttonPanel.AddThemeConstantOverride("separation", 10);
        uiLayer.AddChild(_buttonPanel);
        
        var shopBtn = new Button { Text = "🏪 Магазин", CustomMinimumSize = new Vector2(150, 40) };
        shopBtn.Pressed += () => _shopUI.Visible = true;
        _buttonPanel.AddChild(shopBtn);
        
        var invBtn = new Button { Text = "🎒 Инвентарь", CustomMinimumSize = new Vector2(150, 40) };
        invBtn.Pressed += () => {
            var invUI = InventoryUI.Instance;
            if (invUI != null && GodotObject.IsInstanceValid(invUI))
            {
                invUI.Visible = true;
            }
        };
        _buttonPanel.AddChild(invBtn);
        
        var digBtn = new Button { Text = "⛏️ Раскопки", CustomMinimumSize = new Vector2(150, 40) };
        digBtn.Pressed += () => GetTree().ChangeSceneToFile("res://DigSite.tscn");
        _buttonPanel.AddChild(digBtn);
        
        var saveBtn = new Button { Text = "💾 Сохранить и выйти", CustomMinimumSize = new Vector2(150, 40) };
        saveBtn.Pressed += () => SaveSystem.Instance?.ForceSaveAndQuit();
        _buttonPanel.AddChild(saveBtn);
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
    
    private void CheckOfflineReward()
    {
        if (OfflineRewardSystem.Instance == null || SaveSystem.Instance == null || _offlineReward == null) return;
        
        OfflineRewardSystem.Instance.CalculateOfflineReward(SaveSystem.Instance.GetLastSaveTimestamp());
        
        if (OfflineRewardSystem.Instance.HasReward())
        {
            var label = _offlineReward.GetNodeOrNull<Label>("RewardPanel/Content/RewardsTitleLabel");
            var btn = _offlineReward.GetNodeOrNull<Button>("RewardPanel/Content/CollectButton");
            
            if (label != null)
            {
                int c = OfflineRewardSystem.Instance.OfflineCoins;
                int e = OfflineRewardSystem.Instance.OfflineEnergy;
                string t = "Welcome back!\n";
                if (c > 0) t += $"You earned {c} coins!\n";
                if (e > 0) t += $"You recovered {e} energy!";
                label.Text = t.Trim();
            }
            
            if (btn != null)
            {
                foreach (var conn in btn.GetSignalConnectionList("pressed")) 
                    btn.Disconnect("pressed", conn["callable"].AsCallable());
                
                btn.Pressed += () => { 
                    OfflineRewardSystem.Instance.CollectReward(); 
                    _offlineReward.Visible = false; 
                };
            }
            _offlineReward.Visible = true;
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

        public void StartMovingFurniture(string roomId, Furniture furniture, List<FoundItem> keptItems)
    {
        // Создаем временный объект мебели с сохраненными предметами
        var tempPlaced = new PlacedFurniture(furniture, Vector2I.Zero);
        tempPlaced.Items = new List<FoundItem>(keptItems); // Создаем копию списка

        GD.Print($"[Museum] Начинаем перемещение витрины с {keptItems.Count} экспонатами");

        // Запускаем режим размещения
        MuseumSystem.Instance.StartPlacementModeWithItems(furniture, tempPlaced);
        
        var currentScene = GetTree().CurrentScene;
        var placementUI = currentScene.GetNodeOrNull<PlacementModeUI>("PlacementModeUI");
        
        if (placementUI == null)
        {
            placementUI = new PlacementModeUI();
            placementUI.Name = "PlacementModeUI";
            currentScene.AddChild(placementUI);
        }
        
        var room = MuseumLayout.Instance.GetRoom(roomId);
        placementUI.StartPlacementForMoving(room, furniture, tempPlaced);
    }

     public void OnInventoryPlaceCollection(string collectionId, Quality quality)
    {
        GD.Print($"[Museum] Запрошено размещение коллекции: {collectionId}");
        MuseumSystem.Instance.StartPlacementFromInventory(collectionId, quality);
    }
}