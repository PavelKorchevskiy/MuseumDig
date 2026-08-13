using Godot;
using System.Collections.Generic;

public partial class RoomViewUI : CanvasLayer
{
    private Control _roomContainer;
    private ColorRect _background;
    private List<Control> _gridCells = new();
    private List<Control> _furnitureRects = new();
    private List<Label> _furnitureLabels = new();
    private List<Control> _doorRects = new();
    private Label _roomNameLabel;
    
    // Используем изометрические размеры
    private const int CellWidth = IsoUtils.TileWidth;
    private const int CellHeight = IsoUtils.TileHeight;
    private const int GridOffsetX = 500; // Центрирование сетки
    private const int GridOffsetY = 100;
    
    public override void _Ready()
    {
        Layer = 5;
        
        _roomContainer = new Control();
        _roomContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _roomContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_roomContainer);
        
        _background = new ColorRect();
        _background.Color = new Color(0.1f, 0.1f, 0.15f);
        _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _background.MouseFilter = Control.MouseFilterEnum.Ignore;
        _roomContainer.AddChild(_background);
        
        _roomNameLabel = new Label();
        _roomNameLabel.Position = new Vector2(20, 20);
        _roomNameLabel.AddThemeFontSizeOverride("font_size", 24);
        _roomNameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        _roomNameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _roomContainer.AddChild(_roomNameLabel);
        
        GD.Print("[RoomViewUI] Ready (Isometric Mode)");
    }
    
    public void DisplayRoom(Room room)
    {
        GD.Print($"[RoomViewUI] DisplayRoom: {room.DisplayName}, furniture: {room.PlacedFurnitureList.Count}");
        
        ClearView();
        
        _roomNameLabel.Text = room.DisplayName;
        
        DrawGrid(room);
        DrawFurniture(room);
        DrawDoors(room);
    }
    
    private void DrawGrid(Room room)
{
    GD.Print($"[RoomViewUI] Drawing isometric grid {room.Width}x{room.Height}");
    
    Texture2D floorTexture = null;
    if (ResourceLoader.Exists("res://assets/museum/floor.png"))
    {
        floorTexture = GD.Load<Texture2D>("res://assets/museum/floor.png");
    }
    
    for (int x = 0; x < room.Width; x++)
    {
        for (int y = 0; y < room.Height; y++)
        {
            Control cell;
            
            var isoPos = IsoUtils.GridToIso(x, y);
            var position = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y);
            
            if (floorTexture != null)
            {
                var textureRect = new TextureRect();
                textureRect.Position = position;
                
                // ИСПРАВЛЕНИЕ: Используем точный размер тайла без зазоров
                textureRect.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
                textureRect.Texture = floorTexture;
                
                // ИСПРАВЛЕНИЕ: StretchMode.Covered заполняет всю область без зазоров
                textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
                
                cell = textureRect;
            }
            else
            {
                var colorRect = new ColorRect();
                colorRect.Position = position;
                colorRect.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
                bool isEven = (x + y) % 2 == 0;
                colorRect.Color = isEven ? new Color(0.3f, 0.25f, 0.2f) : new Color(0.35f, 0.3f, 0.25f);
                cell = colorRect;
            }
            
            cell.MouseFilter = Control.MouseFilterEnum.Stop;
            cell.Name = $"cell_{x}_{y}";
            cell.ZIndex = IsoUtils.GetZOrder(x, y);
            
            _roomContainer.AddChild(cell);
            _gridCells.Add(cell);
        }
    }
    
    if (floorTexture == null)
    {
        GD.Print("[RoomViewUI] Using fallback color grid (floor.png not found)");
    }
}
    
    private void DrawFurniture(Room room)
    {
        foreach (var placed in room.PlacedFurnitureList)
        {
            // Для изометрии берём центральную клетку мебели
            int centerX = placed.Position.X + placed.Size.X / 2;
            int centerY = placed.Position.Y + placed.Size.Y / 2;
            
            var isoPos = IsoUtils.GridToIso(centerX, centerY);
            
            var rect = new ColorRect();
            rect.Position = new Vector2(
                GridOffsetX + isoPos.X - 20,
                GridOffsetY + isoPos.Y - 40
            );
            rect.Size = new Vector2(40, 40);
            
            if (placed.Furniture is DisplayCase)
            {
                rect.Color = new Color(0.3f, 0.6f, 0.9f, 0.8f);
            }
            else if (placed.Furniture is Pedestal)
            {
                rect.Color = new Color(0.9f, 0.7f, 0.3f, 0.8f);
            }
            
            rect.MouseFilter = Control.MouseFilterEnum.Stop;
            rect.Name = $"furniture_{placed.InstanceId}";
            rect.SetMeta("placed_id", placed.InstanceId);
            rect.ZIndex = IsoUtils.GetZOrder(centerX, centerY) + 1;
            
            _roomContainer.AddChild(rect);
            _furnitureRects.Add(rect);
            
            // Название мебели
            var label = new Label();
            label.Text = placed.Furniture.DisplayName;
            label.Position = rect.Position + new Vector2(0, 45);
            label.AddThemeFontSizeOverride("font_size", 10);
            label.AddThemeColorOverride("font_color", Colors.White);
            label.MouseFilter = Control.MouseFilterEnum.Ignore;
            _roomContainer.AddChild(label);
            _furnitureLabels.Add(label);
            
            DrawFurnitureContents(placed);
        }
    }
    
    private void DrawFurnitureContents(PlacedFurniture placed)
    {
        var items = placed.Furniture.GetAllItems();
        if (items.Count == 0) return;
        
        int centerX = placed.Position.X + placed.Size.X / 2;
        int centerY = placed.Position.Y + placed.Size.Y / 2;
        var isoPos = IsoUtils.GridToIso(centerX, centerY);
        
        Vector2 startPos = new Vector2(
            GridOffsetX + isoPos.X - 15,
            GridOffsetY + isoPos.Y - 30
        );
        
        int iconSize = 10;
        int spacing = 3;
        
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var resource = GameData.GetResource(item.ResourceId);
            if (resource == null) continue;
            
            int row = i / 3;
            int col = i % 3;
            
            var icon = new ColorRect();
            icon.Position = startPos + new Vector2(col * (iconSize + spacing), row * (iconSize + spacing));
            icon.Size = new Vector2(iconSize, iconSize);
            icon.Color = GetRarityColor(resource.Rarity);
            
            if (item.Quality == Quality.Damaged)
            {
                icon.Color = icon.Color.Darkened(0.5f);
            }
            
            icon.MouseFilter = Control.MouseFilterEnum.Ignore;
            _roomContainer.AddChild(icon);
            _furnitureRects.Add(icon);
        }
    }
    
    private void DrawDoors(Room room)
{
    // Загружаем обе текстуры дверей
    Texture2D doorLockedTexture = null;
    Texture2D doorOpenTexture = null;
    
    if (ResourceLoader.Exists("res://assets/museum/doors/door_locked.png"))
    {
        doorLockedTexture = GD.Load<Texture2D>("res://assets/museum/doors/door_locked.png");
    }
    
    if (ResourceLoader.Exists("res://assets/museum/doors/door_open.png"))
    {
        doorOpenTexture = GD.Load<Texture2D>("res://assets/museum/doors/door_open.png");
    }
    
    foreach (var kvp in room.Doors)
    {
        var door = kvp.Value;
        
        var isoPos = IsoUtils.GridToIso(door.Position.X, door.Position.Y);
        
        // Выбираем текстуру в зависимости от состояния двери
        Texture2D doorTexture = null;
        
        if (door.IsExitToStreet)
        {
            // Дверь на улицу — можно использовать открытую или специальную текстуру
            doorTexture = doorOpenTexture;
        }
        else if (door.HasConnection)
        {
            // Зал куплен — открытая дверь со светом
            doorTexture = doorOpenTexture;
        }
        else
        {
            // Зал не куплен — закрытая дверь с замком
            doorTexture = doorLockedTexture;
        }
        
        Control doorControl;
        
        if (doorTexture != null)
        {
            var textureRect = new TextureRect();
            textureRect.Position = new Vector2(
                GridOffsetX + isoPos.X - 32, // Центрируем по X (64/2 = 32)
                GridOffsetY + isoPos.Y - 96  // Дверь стоит на полу, поэтому сдвигаем вверх на высоту
            );
            textureRect.Size = new Vector2(64, 96); // Размер двери
            textureRect.Texture = doorTexture;
            textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            textureRect.ZIndex = IsoUtils.GetZOrder(door.Position.X, door.Position.Y) + 10;
            
            doorControl = textureRect;
        }
        else
        {
            // Запасной вариант: цветной прямоугольник
            var colorRect = new ColorRect();
            colorRect.Position = new Vector2(
                GridOffsetX + isoPos.X - 16,
                GridOffsetY + isoPos.Y - 48
            );
            colorRect.Size = new Vector2(32, 48);
            colorRect.Color = door.HasConnection ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            colorRect.ZIndex = IsoUtils.GetZOrder(door.Position.X, door.Position.Y) + 10;
            
            doorControl = colorRect;
        }
        
        doorControl.MouseFilter = Control.MouseFilterEnum.Stop;
        doorControl.Name = $"door_{door.Direction}";
        doorControl.SetMeta("direction", (int)door.Direction);
        
        _roomContainer.AddChild(doorControl);
        _doorRects.Add(doorControl);
    }
}
    
    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            var mousePos = GetViewport().GetMousePosition();
            
            foreach (var rect in _furnitureRects)
            {
                if (rect.GetRect().HasPoint(mousePos))
                {
                    string placedId = rect.GetMeta("placed_id").AsString();
                    OnFurnitureClicked(placedId);
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
            
            foreach (var rect in _doorRects)
            {
                if (rect.GetRect().HasPoint(mousePos))
                {
                    int dirInt = rect.GetMeta("direction").AsInt32();
                    Direction direction = (Direction)dirInt;
                    OnDoorClicked(direction);
                    GetViewport().SetInputAsHandled();
                    return;
                }
            }
        }
    }
    
    private void OnFurnitureClicked(string placedId)
    {
        GD.Print($"[RoomView] Furniture clicked: {placedId}");
        
        var room = MuseumSystem.Instance.GetCurrentRoom();
        var placed = room.PlacedFurnitureList.Find(p => p.InstanceId == placedId);
        
        if (placed != null)
        {
            ShowFurnitureMenu(placed);
        }
    }
    
    private void ShowFurnitureMenu(PlacedFurniture placed)
    {
        var menu = new VBoxContainer();
        menu.Position = new Vector2(700, 100);
        menu.AddThemeConstantOverride("separation", 10);
        
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.2f, 0.2f, 0.3f, 0.95f);
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 15;
        style.ContentMarginTop = 15;
        style.ContentMarginRight = 15;
        style.ContentMarginBottom = 15;
        menu.AddThemeStyleboxOverride("panel", style);
        
        var title = new Label();
        title.Text = placed.Furniture.DisplayName;
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        menu.AddChild(title);
        
        var addBtn = new Button();
        addBtn.Text = "+ Добавить из инвентаря";
        addBtn.Pressed += () => OnAddFromInventory(placed);
        menu.AddChild(addBtn);
        
        var sellBtn = new Button();
        sellBtn.Text = $"Продать за {placed.Furniture.SellPrice} монет";
        sellBtn.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f));
        sellBtn.Pressed += () => OnSellFurniture(placed);
        menu.AddChild(sellBtn);
        
        var closeBtn = new Button();
        closeBtn.Text = "Закрыть";
        closeBtn.Pressed += () => menu.QueueFree();
        menu.AddChild(closeBtn);
        
        AddChild(menu);
    }
    
    private void OnAddFromInventory(PlacedFurniture placed)
    {
        var room = MuseumSystem.Instance.GetCurrentRoom();
        
        foreach (var invItem in InventorySystem.Instance.GetAllItems())
        {
            if (MuseumSystem.Instance.TryAddItemToFurniture(room, placed, invItem.ResourceId, invItem.Quality))
            {
                GD.Print($"[RoomView] Added {invItem.ResourceId} to furniture");
                UpdateDisplay();
                return;
            }
        }
        
        GD.Print("[RoomView] Нет подходящих предметов в инвентаре");
    }
    
    private void OnSellFurniture(PlacedFurniture placed)
    {
        var room = MuseumSystem.Instance.GetCurrentRoom();
        
        if (MuseumSystem.Instance.SellFurniture(room, placed))
        {
            GD.Print($"[RoomView] Sold furniture for {placed.Furniture.SellPrice} coins");
            UpdateDisplay();
        }
    }
    
    private void OnDoorClicked(Direction direction)
    {
        GD.Print($"[RoomView] Door clicked: {direction}");
        
        var room = MuseumSystem.Instance.GetCurrentRoom();
        var door = room.GetDoor(direction);
        
        if (door == null) return;
        
        if (door.HasConnection)
        {
            MuseumSystem.Instance.EnterDoor(room, direction);
            UpdateDisplay();
        }
        else
        {
            ShowBuyRoomDialog(direction);
        }
    }
    
    private void ShowBuyRoomDialog(Direction direction)
    {
        var dialog = new VBoxContainer();
        dialog.Position = new Vector2(700, 100);
        dialog.AddThemeConstantOverride("separation", 10);
        
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.2f, 0.2f, 0.3f, 0.95f);
        style.CornerRadiusTopLeft = 8;
        style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8;
        style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 15;
        style.ContentMarginTop = 15;
        style.ContentMarginRight = 15;
        style.ContentMarginBottom = 15;
        dialog.AddThemeStyleboxOverride("panel", style);
        
        var title = new Label();
        title.Text = "Купить новый зал?";
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        dialog.AddChild(title);
        
        var costLabel = new Label();
        costLabel.Text = $"Стоимость: {MuseumSystem.RoomBuyPrice} монет";
        dialog.AddChild(costLabel);
        
        var buyBtn = new Button();
        buyBtn.Text = "Купить";
        buyBtn.Pressed += () => 
        {
            var room = MuseumSystem.Instance.GetCurrentRoom();
            Vector2I newPos = direction switch
            {
                Direction.Top => new Vector2I(room.GlobalPosition.X, room.GlobalPosition.Y - 1),
                Direction.Bottom => new Vector2I(room.GlobalPosition.X, room.GlobalPosition.Y + 1),
                Direction.Left => new Vector2I(room.GlobalPosition.X - 1, room.GlobalPosition.Y),
                Direction.Right => new Vector2I(room.GlobalPosition.X + 1, room.GlobalPosition.Y),
                _ => room.GlobalPosition
            };
            
            if (MuseumSystem.Instance.TryBuyRoom(newPos))
            {
                GD.Print($"[RoomView] Bought new room at {newPos}");
                MuseumSystem.Instance.EnterDoor(room, direction);
                UpdateDisplay();
            }
            else
            {
                GD.Print("[RoomView] Cannot buy room");
            }
            
            dialog.QueueFree();
        };
        dialog.AddChild(buyBtn);
        
        var cancelBtn = new Button();
        cancelBtn.Text = "Отмена";
        cancelBtn.Pressed += () => dialog.QueueFree();
        dialog.AddChild(cancelBtn);
        
        AddChild(dialog);
    }
    
    private void UpdateDisplay()
    {
        var room = MuseumSystem.Instance.GetCurrentRoom();
        DisplayRoom(room);
    }
    
    private Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => new Color(0.7f, 0.7f, 0.7f),
            Rarity.Uncommon => new Color(0.3f, 0.8f, 0.3f),
            Rarity.Rare => new Color(0.3f, 0.5f, 0.9f),
            Rarity.Epic => new Color(0.7f, 0.3f, 0.9f),
            Rarity.Legendary => new Color(1.0f, 0.8f, 0.2f),
            _ => Colors.White
        };
    }
    
    private void ClearView()
    {
        foreach (var cell in _gridCells) cell.QueueFree();
        foreach (var rect in _furnitureRects) rect.QueueFree();
        foreach (var label in _furnitureLabels) label.QueueFree();
        foreach (var rect in _doorRects) rect.QueueFree();
        
        _gridCells.Clear();
        _furnitureRects.Clear();
        _furnitureLabels.Clear();
        _doorRects.Clear();
    }
    
    public void AddVisitorToRoom(Control visitor)
    {
        if (_roomContainer != null)
        {
            _roomContainer.AddChild(visitor);
        }
    }
}