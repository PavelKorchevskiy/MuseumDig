using Godot;

public partial class Museum : Node2D
{
    private RoomViewUI _roomView;
    private CanvasLayer _offlineReward;
    private MuseumShopUI _shopUI;
    private VBoxContainer _buttonPanel;
    
    private bool _initialDisplayUpdated = false;
    private bool _offlineRewardChecked = false;

    public override void _Ready()
    {
        // 1. Создаем или получаем UI-слой с высоким приоритетом (чтобы быть поверх комнаты)
        var uiLayer = GetNodeOrNull<CanvasLayer>("UI");
        if (uiLayer == null)
        {
            uiLayer = new CanvasLayer { Name = "UI", Layer = 100 };
            AddChild(uiLayer);
        }

        // 2. Офлайн-награда (если она есть в сцене)
        _offlineReward = GetNodeOrNull<CanvasLayer>("UI/OfflineReward");
        if (_offlineReward != null) _offlineReward.Visible = false;

        // 3. Магазин
        _shopUI = new MuseumShopUI { Name = "MuseumShop" };
        uiLayer.AddChild(_shopUI);
        _shopUI.Visible = false;

        // 4. Комната музея
        _roomView = new RoomViewUI();
        AddChild(_roomView);
        
        // 5. Панель кнопок
        CreateButtonPanel(uiLayer);
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
    }

    private void CreateButtonPanel(CanvasLayer uiLayer)
    {
        _buttonPanel = new VBoxContainer();
        _buttonPanel.Position = new Vector2(20, 60);
        _buttonPanel.AddThemeConstantOverride("separation", 10);
        uiLayer.AddChild(_buttonPanel);
        
        // Кнопка магазина
        var shopBtn = new Button { Text = "🏪 Магазин", CustomMinimumSize = new Vector2(150, 40) };
        shopBtn.Pressed += () => _shopUI.Visible = true;
        _buttonPanel.AddChild(shopBtn);
        
        // Кнопка инвентаря
var invBtn = new Button { Text = "🎒 Инвентарь", CustomMinimumSize = new Vector2(150, 40) };
invBtn.Pressed += () => {
    var invUI = InventoryUI.Instance;
    
    if (invUI != null && GodotObject.IsInstanceValid(invUI))
    {
        invUI.Visible = true;
    }
    else
    {
        GD.PrintErr("[Museum] Инвентарь недоступен. Проверьте настройки Autoload!");
    }
};
_buttonPanel.AddChild(invBtn);
        
        // Кнопка раскопок
        var digBtn = new Button { Text = "⛏️ Раскопки", CustomMinimumSize = new Vector2(150, 40) };
        digBtn.Pressed += () => GetTree().ChangeSceneToFile("res://DigSite.tscn");
        _buttonPanel.AddChild(digBtn);
        
        // Кнопка сохранения
        var saveBtn = new Button { Text = "💾 Сохранить и выйти", CustomMinimumSize = new Vector2(150, 40) };
        saveBtn.Pressed += () => SaveSystem.Instance?.ForceSaveAndQuit();
        _buttonPanel.AddChild(saveBtn);

        var debugBtn = new Button();
    debugBtn.Text = "🔍 Занятые клетки";
    debugBtn.CustomMinimumSize = new Vector2(150, 0);
    debugBtn.Pressed += () => {
        var museum = GetTree().CurrentScene as Museum;
        museum?.ToggleOccupancyDebug();
    };
            _buttonPanel.AddChild(debugBtn);

    }

    private void UpdateDisplay()
    {
        if (MuseumSystem.Instance != null && _roomView != null)
        {
            _roomView.DisplayRoom(MuseumSystem.Instance.GetCurrentRoom());
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

    public void RefreshRoomView()
    {
        if (_roomView != null && MuseumSystem.Instance != null)
        {
            _roomView.DisplayRoom(MuseumSystem.Instance.GetCurrentRoom());
        }
        
    }

            public void ToggleOccupancyDebug()
    {
        // Рекурсивный поиск RoomViewUI по всей сцене
        var roomViewUI = FindNodeOfType<RoomViewUI>(this);
        
        if (roomViewUI != null)
        {
            roomViewUI.ToggleOccupancyDebug();
        }
        else
        {
            GD.PrintErr("[Museum] RoomViewUI не найден! Вывожу структуру сцены:");
            PrintSceneTree(this, 0);
        }
    }
    
    // Рекурсивный поиск узла нужного типа
    private T FindNodeOfType<T>(Node node) where T : class
    {
        if (node is T result) return result;
        
        foreach (var child in node.GetChildren())
        {
            var found = FindNodeOfType<T>(child);
            if (found != null) return found;
        }
        
        return null;
    }
    
    // Вывод структуры сцены в консоль (для отладки)
    private void PrintSceneTree(Node node, int depth)
    {
        string indent = new string(' ', depth * 2);
        GD.Print($"{indent}- {node.Name} ({node.GetType().Name})");
        
        foreach (var child in node.GetChildren())
        {
            PrintSceneTree(child, depth + 1);
        }
    }
}