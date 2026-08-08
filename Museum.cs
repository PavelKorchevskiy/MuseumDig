using Godot;
using System.Collections.Generic;

public partial class Museum : Node2D
{
    private Label _roomNameLabel;
    private VBoxContainer _furnitureList;
    private CanvasLayer _worldMap;
    private CanvasLayer _offlineReward;
    
    private bool _offlineRewardChecked = false;
    private bool _initialDisplayUpdated = false;

    public override void _Ready()
    {
        _worldMap = GetNodeOrNull<CanvasLayer>("UI/WorldMap");
        if (_worldMap != null) _worldMap.Visible = false;
        
        _offlineReward = GetNodeOrNull<CanvasLayer>("UI/OfflineReward");
        if (_offlineReward != null) _offlineReward.Visible = false;

        var uiLayer = GetNodeOrNull<CanvasLayer>("UI");
        if (uiLayer == null)
        {
            uiLayer = new CanvasLayer { Name = "UI", Layer = 10 };
            AddChild(uiLayer);
        }

        var mainContainer = new VBoxContainer();
        mainContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        mainContainer.OffsetLeft = 30;
        mainContainer.OffsetTop = 30;
        mainContainer.OffsetRight = -30;
        mainContainer.OffsetBottom = -30;
        mainContainer.AddThemeConstantOverride("separation", 20);
        uiLayer.AddChild(mainContainer);

        var roomControls = new HBoxContainer();
        roomControls.AddThemeConstantOverride("separation", 15);
        roomControls.Alignment = BoxContainer.AlignmentMode.Center;

        var prevBtn = new Button { Text = "◀ Пред. зал" };
        prevBtn.Pressed += OnPrevRoomPressed;
        
        _roomNameLabel = new Label();
        _roomNameLabel.AddThemeFontSizeOverride("font_size", 24);
        _roomNameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        
        var nextBtn = new Button { Text = "След. зал ▶" };
        nextBtn.Pressed += OnNextRoomPressed;

        roomControls.AddChild(prevBtn);
        roomControls.AddChild(_roomNameLabel);
        roomControls.AddChild(nextBtn);
        mainContainer.AddChild(roomControls);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _furnitureList = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _furnitureList.AddThemeConstantOverride("separation", 15);
        scroll.AddChild(_furnitureList);
        mainContainer.AddChild(scroll);

        var bottomRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        var digBtn = new Button { Text = "⛏️ Start Digging", CustomMinimumSize = new Vector2(200, 40) };
        digBtn.Pressed += OnDigPressed;
        bottomRow.AddChild(digBtn);
        mainContainer.AddChild(bottomRow);
    }

    public override void _Process(double delta)
    {
        if (!_initialDisplayUpdated && SaveSystem.Instance != null && SaveSystem.Instance.GetLastSaveTimestamp() > 0)
        {
            _initialDisplayUpdated = true;
            UpdateDisplay();
            if (!_offlineRewardChecked)
            {
                _offlineRewardChecked = true;
                CheckOfflineReward();
            }
        }
    }

    private void UpdateDisplay()
    {
        if (MuseumSystem.Instance == null) return;

        var room = MuseumSystem.Instance.GetCurrentRoom();
        _roomNameLabel.Text = $"{room.DisplayName} ({room.GlobalPosition.X}, {room.GlobalPosition.Y})";

        foreach (var child in _furnitureList.GetChildren()) child.QueueFree();

        if (room.PlacedFurnitureList.Count == 0)
        {
            var emptyLabel = new Label { Text = "В этом зале пока нет мебели. Купите её в магазине!", HorizontalAlignment = HorizontalAlignment.Center };
            emptyLabel.AddThemeFontSizeOverride("font_size", 18);
            emptyLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
            _furnitureList.AddChild(emptyLabel);
            return;
        }

        foreach (var placed in room.PlacedFurnitureList)
        {
            var furn = placed.Furniture;
            var panel = new VBoxContainer();
            panel.AddThemeConstantOverride("separation", 8);
            
            var style = new StyleBoxFlat { BgColor = new Color(0.15f, 0.15f, 0.2f), CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8, ContentMarginLeft = 15, ContentMarginTop = 12, ContentMarginRight = 15, ContentMarginBottom = 12 };
            panel.AddThemeStyleboxOverride("panel", style);

            var header = new Label();
            int income = CalculateFurnitureIncome(placed);
            string collectionInfo = (furn is Pedestal ped && !string.IsNullOrEmpty(ped.CurrentCollectionId)) ? $" [{GameData.GetCollection(ped.CurrentCollectionId)?.DisplayName}]" : "";
            header.Text = $"{furn.DisplayName} ({placed.Size.X}x{placed.Size.Y}){collectionInfo} | Доход: +{income}/сек";
            header.AddThemeFontSizeOverride("font_size", 18);
            header.AddThemeColorOverride("font_color", new Color(0.8f, 1f, 0.8f));
            panel.AddChild(header);

            var items = furn.GetAllItems();
            if (items.Count == 0)
            {
                panel.AddChild(new Label { Text = "  Пусто", Modulate = new Color(0.6f, 0.6f, 0.6f) });
            }
            else
            {
                foreach (var item in items)
                {
                    var res = GameData.GetResource(item.ResourceId);
                    var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
                    
                    var nameLabel = new Label { Text = $"  • {res.DisplayName} ({item.Quality})", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
                    row.AddChild(nameLabel);

                    var returnBtn = new Button { Text = "Вернуть", CustomMinimumSize = new Vector2(120, 0) };
                    string rId = item.ResourceId;
                    Quality q = item.Quality;
                    returnBtn.Pressed += () => OnReturnPressed(room, placed, rId, q);
                    row.AddChild(returnBtn);
                    panel.AddChild(row);
                }
            }

            var addBtn = new Button { Text = "+ Добавить из инвентаря" };
            addBtn.AddThemeColorOverride("font_color", new Color(0.7f, 1f, 0.7f));
            addBtn.Pressed += () => OnAddFromInventoryPressed(room, placed);
            panel.AddChild(addBtn);

            _furnitureList.AddChild(panel);
        }
    }

    private int CalculateFurnitureIncome(PlacedFurniture placed)
    {
        int total = 0;
        foreach (var item in placed.Furniture.GetAllItems())
        {
            var res = GameData.GetResource(item.ResourceId);
            if (res == null) continue;
            float mult = res.GetRarityMultiplier() * res.GetQualityMultiplier(item.Quality);
            int baseInc = (int)(res.BaseMuseumIncome * mult);
            if (placed.Furniture is Pedestal ped && ped.IsComplete())
            {
                var col = GameData.GetCollection(ped.CurrentCollectionId);
                if (col != null) baseInc = (int)(baseInc * col.CollectionBonus);
            }
            total += baseInc;
        }
        return total;
    }

    private void OnNextRoomPressed()
    {
        var rooms = MuseumSystem.Instance.GetAllRooms();
        int idx = rooms.FindIndex(r => r.GlobalPosition == MuseumSystem.Instance.GetCurrentRoom().GlobalPosition);
        MuseumSystem.Instance.SetCurrentRoom(rooms[(idx + 1) % rooms.Count].GlobalPosition);
        UpdateDisplay();
    }

    private void OnPrevRoomPressed()
    {
        var rooms = MuseumSystem.Instance.GetAllRooms();
        int idx = rooms.FindIndex(r => r.GlobalPosition == MuseumSystem.Instance.GetCurrentRoom().GlobalPosition);
        MuseumSystem.Instance.SetCurrentRoom(rooms[(idx - 1 + rooms.Count) % rooms.Count].GlobalPosition);
        UpdateDisplay();
    }

    private void OnAddFromInventoryPressed(Room room, PlacedFurniture placed)
    {
        foreach (var invItem in InventorySystem.Instance.GetAllItems())
        {
            if (MuseumSystem.Instance.TryAddItemToFurniture(room, placed, invItem.ResourceId, invItem.Quality))
            {
                UpdateDisplay();
                return; // Добавляем по одному за раз
            }
        }
        GD.Print("[Museum] Нет подходящих предметов для этой мебели.");
    }

    private void OnReturnPressed(Room room, PlacedFurniture placed, string resourceId, Quality quality)
    {
        MuseumSystem.Instance.TryReturnItemFromFurniture(room, placed, resourceId, quality);
        UpdateDisplay();
    }

    private void OnDigPressed() => GetTree().ChangeSceneToFile("res://DigSite.tscn");

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
}