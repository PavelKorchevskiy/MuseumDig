using Godot;
using System.Collections.Generic;

public partial class Museum : Node2D
{
    public RoomViewUI _roomView;
    private CanvasLayer _offlineReward;
    private CanvasLayer _shop;
    private CanvasLayer _inventory;    
    private VBoxContainer _buttonPanel;
    
    private bool _offlineRewardChecked = false;
    private bool _initialDisplayUpdated = false;

    public override void _Ready()
    {
        _offlineReward = GetNodeOrNull<CanvasLayer>("UI/OfflineReward");
        if (_offlineReward != null) _offlineReward.Visible = false;

        var museumShop = new MuseumShopUI();
        museumShop.Name = "MuseumShop";
        AddChild(museumShop);

        if (_shop != null) _shop.Visible = false;
        
        _inventory = GetNodeOrNull<CanvasLayer>("UI/Inventory");
        if (_inventory != null) _inventory.Visible = false;

        // Создаём визуализацию зала
        _roomView = new RoomViewUI();
        AddChild(_roomView);
        
        // Создаём панель кнопок
        CreateButtonPanel();
    }
    
    private void CreateButtonPanel()
    {
        var uiLayer = GetNodeOrNull<CanvasLayer>("UI");
        if (uiLayer == null)
        {
            uiLayer = new CanvasLayer { Name = "UI", Layer = 10 };
            AddChild(uiLayer);
        }
        
        _buttonPanel = new VBoxContainer();
        _buttonPanel.Position = new Vector2(20, 60);
        _buttonPanel.AddThemeConstantOverride("separation", 10);
        uiLayer.AddChild(_buttonPanel);
        
        // Кнопка магазина
        var shopBtn = new Button();
        shopBtn.Text = "🏪 Магазин";
        shopBtn.CustomMinimumSize = new Vector2(150, 40);
        shopBtn.Pressed += OnShopPressed;
        _buttonPanel.AddChild(shopBtn);
        
        // Кнопка инвентаря
        var invBtn = new Button();
        invBtn.Text = "🎒 Инвентарь";
        invBtn.CustomMinimumSize = new Vector2(150, 40);
        invBtn.Pressed += OnInventoryPressed;
        _buttonPanel.AddChild(invBtn);
        
        // Кнопка раскопок
        var digBtn = new Button();
        digBtn.Text = "⛏️ Раскопки";
        digBtn.CustomMinimumSize = new Vector2(150, 40);
        digBtn.Pressed += OnDigPressed;
        _buttonPanel.AddChild(digBtn);
        
        // Кнопка сохранения
        var saveBtn = new Button();
        saveBtn.Text = "💾 Сохранить и выйти";
        saveBtn.CustomMinimumSize = new Vector2(150, 40);
        saveBtn.Pressed += OnSaveQuitPressed;
        _buttonPanel.AddChild(saveBtn);
    }

    public override void _Process(double delta)
{
    // При первом запуске сразу обновляем визуализацию
    if (!_initialDisplayUpdated)
    {
        _initialDisplayUpdated = true;
        UpdateDisplay();
        
        // Проверяем офлайн-награду только если есть сохранение
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

    private void UpdateDisplay()
    {
        if (MuseumSystem.Instance == null || _roomView == null) return;
        
        var room = MuseumSystem.Instance.GetCurrentRoom();
        _roomView.DisplayRoom(room);
    }
    
    private void OnShopPressed()
{
    var museumShop = GetNodeOrNull<MuseumShopUI>("MuseumShop");
    if (museumShop != null) museumShop.Visible = true;
}
    
    private void OnInventoryPressed()
    {
        if (_inventory != null) _inventory.Visible = true;
    }
    
    private void OnDigPressed()
    {
        GetTree().ChangeSceneToFile("res://DigSite.tscn");
    }
    
    private void OnSaveQuitPressed()
    {
        SaveSystem.Instance?.ForceSaveAndQuit();
    }

    private void CheckOfflineReward()
    {
        if (OfflineRewardSystem.Instance == null || SaveSystem.Instance == null) return;
        OfflineRewardSystem.Instance.CalculateOfflineReward(SaveSystem.Instance.GetLastSaveTimestamp());
        
        if (OfflineRewardSystem.Instance.HasReward() && _offlineReward != null)
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
                foreach (var conn in btn.GetSignalConnectionList("pressed")) btn.Disconnect("pressed", conn["callable"].AsCallable());
                btn.Pressed += () => { OfflineRewardSystem.Instance.CollectReward(); _offlineReward.Visible = false; };
            }
            _offlineReward.Visible = true;
        }
    }

    public void RefreshRoomView()
{
    if (_roomView != null && MuseumSystem.Instance != null)
    {
        _roomView.DisplayRoom(MuseumSystem.Instance.GetCurrentRoom());
        GD.Print("[Museum] Room view refreshed");
    }
}
}