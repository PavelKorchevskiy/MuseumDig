using Godot;
using System.Collections.Generic;

public partial class RoomViewUI : CanvasLayer
{
    public Control _roomContainer; 
    private ColorRect _background;
    private List<ColorRect> _gridCells = new();
    private List<ColorRect> _furnitureRects = new();
    private List<Label> _furnitureLabels = new();
    private List<ColorRect> _doorRects = new();
    private Label _roomNameLabel;
    
    private const int CellSize = 50;
    private const int GridOffsetX = 150;
    private const int GridOffsetY = 100;
    
    public override void _Ready()
    {
        Layer = 5;
        
        // ИСПРАВЛЕНО: Используем Control вместо Node2D
        _roomContainer = new Control();
        _roomContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _roomContainer.MouseFilter = Control.MouseFilterEnum.Ignore; // Клики проходят к дочерним
        AddChild(_roomContainer);
        
        // Фон
        _background = new ColorRect();
        _background.Color = new Color(0.1f, 0.1f, 0.15f);
        _background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _background.MouseFilter = Control.MouseFilterEnum.Ignore;
        _roomContainer.AddChild(_background);
        
        // Название зала
        _roomNameLabel = new Label();
        _roomNameLabel.Position = new Vector2(20, 20);
        _roomNameLabel.AddThemeFontSizeOverride("font_size", 24);
        _roomNameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        _roomNameLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        _roomContainer.AddChild(_roomNameLabel);
        
        GD.Print("[RoomViewUI] Ready");
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

    public void AddVisitorToRoom(Control visitor)
    {
        if (_roomContainer != null)
        {
            _roomContainer.AddChild(visitor);
        }
    }
    
    private void DrawGrid(Room room)
    {
        GD.Print($"[RoomViewUI] Drawing grid {room.Width}x{room.Height}");
        
        for (int x = 0; x < room.Width; x++)
        {
            for (int y = 0; y < room.Height; y++)
            {
                var cell = new ColorRect();
                cell.Position = new Vector2(GridOffsetX + x * CellSize, GridOffsetY + y * CellSize);
                cell.Size = new Vector2(CellSize - 2, CellSize - 2);
                cell.Color = new Color(0.2f, 0.2f, 0.25f);
                cell.MouseFilter = Control.MouseFilterEnum.Stop;
                
                cell.Name = $"cell_{x}_{y}";
                _roomContainer.AddChild(cell);
                _gridCells.Add(cell);
            }
        }
    }
    
    private void DrawFurniture(Room room)
{
    foreach (var placed in room.PlacedFurnitureList)
    {
        // Основной прямоугольник мебели
        var rect = new ColorRect();
        rect.Position = new Vector2(
            GridOffsetX + placed.Position.X * CellSize,
            GridOffsetY + placed.Position.Y * CellSize
        );
        rect.Size = new Vector2(
            placed.Size.X * CellSize - 2,
            placed.Size.Y * CellSize - 2
        );
        
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
        
        _roomContainer.AddChild(rect);
        _furnitureRects.Add(rect);
        
        // Название мебели
        var label = new Label();
        label.Text = placed.Furniture.DisplayName;
        label.Position = rect.Position + new Vector2(5, 5);
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        _roomContainer.AddChild(label);
        _furnitureLabels.Add(label);
        
        // НОВОЕ: Визуализация содержимого
        DrawFurnitureContents(placed);
    }
}

private void DrawFurnitureContents(PlacedFurniture placed)
{
    var items = placed.Furniture.GetAllItems();
    if (items.Count == 0) return;
    
    Vector2 startPos = new Vector2(
        GridOffsetX + placed.Position.X * CellSize + 10,
        GridOffsetY + placed.Position.Y * CellSize + 25
    );
    
    int iconSize = 15;
    int spacing = 5;
    int iconsPerRow = (int)((placed.Size.X * CellSize - 20) / (iconSize + spacing));
    
    for (int i = 0; i < items.Count; i++)
    {
        var item = items[i];
        var resource = GameData.GetResource(item.ResourceId);
        if (resource == null) continue;
        
        int row = i / iconsPerRow;
        int col = i % iconsPerRow;
        
        var icon = new ColorRect();
        icon.Position = startPos + new Vector2(col * (iconSize + spacing), row * (iconSize + spacing));
        icon.Size = new Vector2(iconSize, iconSize);
        
        // Цвет зависит от редкости
        icon.Color = GetRarityColor(resource.Rarity);
        
        // Если предмет повреждён — делаем тусклым
        if (item.Quality == Quality.Damaged)
        {
            icon.Color = icon.Color.Darkened(0.5f);
        }
        
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        _roomContainer.AddChild(icon);
        _furnitureRects.Add(icon); // Добавляем в список для очистки
    }
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
    
    private void DrawDoors(Room room)
    {
        foreach (var kvp in room.Doors)
        {
            var door = kvp.Value;
            var rect = new ColorRect();
            
            Vector2 doorPos;
            Vector2 doorSize;
            
            switch (door.Direction)
            {
                case Direction.Top:
                    doorPos = new Vector2(GridOffsetX + door.Position.X * CellSize, GridOffsetY - 10);
                    doorSize = new Vector2(CellSize - 2, 10);
                    break;
                case Direction.Bottom:
                    doorPos = new Vector2(GridOffsetX + door.Position.X * CellSize, GridOffsetY + room.Height * CellSize);
                    doorSize = new Vector2(CellSize - 2, 10);
                    break;
                case Direction.Left:
                    doorPos = new Vector2(GridOffsetX - 10, GridOffsetY + door.Position.Y * CellSize);
                    doorSize = new Vector2(10, CellSize - 2);
                    break;
                case Direction.Right:
                    doorPos = new Vector2(GridOffsetX + room.Width * CellSize, GridOffsetY + door.Position.Y * CellSize);
                    doorSize = new Vector2(10, CellSize - 2);
                    break;
                default:
                    continue;
            }
            
            rect.Position = doorPos;
            rect.Size = doorSize;
            
            if (door.HasConnection)
            {
                rect.Color = new Color(0.3f, 0.9f, 0.3f, 0.9f);
            }
            else
            {
                rect.Color = new Color(0.5f, 0.5f, 0.5f, 0.9f);
            }
            
            rect.MouseFilter = Control.MouseFilterEnum.Stop;
            rect.Name = $"door_{door.Direction}";
            rect.SetMeta("direction", (int)door.Direction);
            
            _roomContainer.AddChild(rect);
            _doorRects.Add(rect);
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
    
    // Находим мебель по ID
    var room = MuseumSystem.Instance.GetCurrentRoom();
    var placed = room.PlacedFurnitureList.Find(p => p.InstanceId == placedId);
    
    if (placed != null)
    {
        ShowFurnitureMenu(placed);
    }
}

private void ShowFurnitureMenu(PlacedFurniture placed)
{
    // Создаём простое меню взаимодействия
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
    
    // Заголовок
    var title = new Label();
    title.Text = placed.Furniture.DisplayName;
    title.AddThemeFontSizeOverride("font_size", 18);
    title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
    menu.AddChild(title);
    
    // Кнопка добавления
    var addBtn = new Button();
    addBtn.Text = "+ Добавить из инвентаря";
    addBtn.Pressed += () => OnAddFromInventory(placed);
    menu.AddChild(addBtn);
    
    // Кнопка продажи
    var sellBtn = new Button();
    sellBtn.Text = $"Продать за {placed.Furniture.SellPrice} монет";
    sellBtn.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f));
    sellBtn.Pressed += () => OnSellFurniture(placed);
    menu.AddChild(sellBtn);
    
    // Кнопка закрытия
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
            UpdateDisplay(); // Обновляем отображение
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
        UpdateDisplay(); // Обновляем отображение
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
        // Переход в соседний зал
        MuseumSystem.Instance.EnterDoor(room, direction);
        UpdateDisplay();
    }
    else
    {
        // Предложение купить новый зал
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
}