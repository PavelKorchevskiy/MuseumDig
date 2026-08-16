using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class RoomViewUI : CanvasLayer
{
    private Control _roomContainer;
    private ColorRect _background;
    private List<Control> _gridCells = new();
    private List<Control> _furnitureRects = new();
    private List<Label> _furnitureLabels = new();
    private List<Control> _doorRects = new();
    private Label _roomNameLabel;

    private Tween _zoomTween;
private Control _activeFurnitureMenu;
private Control _inventorySelector;
    
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
        // Определяем путь к текстуре
        string texturePath = GetFurnitureTexturePath(placed.Furniture);
        
        Texture2D furnitureTexture = null;
        if (ResourceLoader.Exists(texturePath))
        {
            furnitureTexture = GD.Load<Texture2D>(texturePath);
        }
        
        // Вычисляем изометрическую позицию (центр нижней клетки мебели)
        int centerX = placed.Position.X + placed.Size.X / 2;
        int centerY = placed.Position.Y + placed.Size.Y / 2;
        var isoPos = IsoUtils.GridToIso(centerX, centerY);
        
        Control furnitureControl;
        
        if (furnitureTexture != null)
        {
            var textureRect = new TextureRect();
            
            // Размер спрайта (берём из текстуры)
            float texWidth = furnitureTexture.GetWidth();
            float texHeight = furnitureTexture.GetHeight();
            
            // Позиционирование: сдвигаем так, чтобы низ мебели был на уровне пола
            // В изометрии anchor находится в нижнем центре спрайта
            float yOffset = (placed.Size.Y - 1) * (IsoUtils.TileHeight / 2f) + (IsoUtils.TileHeight / 2f);

textureRect.Position = new Vector2(
    GridOffsetX + isoPos.X - texWidth / 2f + 30, // +30 для горизонтального центрирования
    GridOffsetY + isoPos.Y - texHeight + yOffset
);
            
            textureRect.Size = new Vector2(texWidth, texHeight);
            textureRect.Texture = furnitureTexture;
            textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            textureRect.MouseFilter = Control.MouseFilterEnum.Stop;
            textureRect.Name = $"furniture_{placed.InstanceId}";
            textureRect.SetMeta("placed_id", placed.InstanceId);
            
            // Z-порядок: мебель рисуется поверх пола, но экспонаты поверх неё
            textureRect.ZIndex = IsoUtils.GetZOrder(centerX, centerY) + 5;
            
            furnitureControl = textureRect;
        }
        else
        {
            // Запасной вариант: цветной прямоугольник
            var colorRect = new ColorRect();
            colorRect.Position = new Vector2(
                GridOffsetX + isoPos.X - 20,
                GridOffsetY + isoPos.Y - 40
            );
            colorRect.Size = new Vector2(40, 40);
            
            if (placed.Furniture is DisplayCase)
                colorRect.Color = new Color(0.3f, 0.6f, 0.9f, 0.8f);
            else if (placed.Furniture is Pedestal)
                colorRect.Color = new Color(0.9f, 0.7f, 0.3f, 0.8f);
            
            colorRect.MouseFilter = Control.MouseFilterEnum.Stop;
            colorRect.Name = $"furniture_{placed.InstanceId}";
            colorRect.SetMeta("placed_id", placed.InstanceId);
            colorRect.ZIndex = IsoUtils.GetZOrder(centerX, centerY) + 5;
            
            furnitureControl = colorRect;
        }
        
        _roomContainer.AddChild(furnitureControl);
        _furnitureRects.Add(furnitureControl);
        
        // Название мебели (под мебелью)
        var label = new Label();
        label.Text = placed.Furniture.DisplayName;
        label.Position = new Vector2(
            GridOffsetX + isoPos.X - 30,
            GridOffsetY + isoPos.Y + 20
        );
        label.AddThemeFontSizeOverride("font_size", 10);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.ZIndex = IsoUtils.GetZOrder(centerX, centerY) + 6;
        _roomContainer.AddChild(label);
        _furnitureLabels.Add(label);
        
        // Рисуем экспонаты поверх мебели
        DrawFurnitureContents(placed, centerX, centerY);
    }
}

private string GetFurnitureTexturePath(Furniture furniture)
{
    if (furniture is DisplayCase)
    {
        if (furniture.Size.X == 1 && furniture.Size.Y == 1)
            return "res://assets/museum/furniture/display_case_small.png";
        else
            return "res://assets/museum/furniture/display_case_large.png";
    }
    else if (furniture is Pedestal)
    {
        if (furniture.Size.X == 2 && furniture.Size.Y == 2)
            return "res://assets/museum/furniture/pedestal_small.png";
        else
            return "res://assets/museum/furniture/pedestal_large.png";
    }
    
    return "";
}
    
    private void DrawFurnitureContents(PlacedFurniture placed, int centerX, int centerY)
{
    var items = placed.GetAllItems();
    GD.Print($"[DEBUG DRAW] Витрина ID:{placed.InstanceId} | Отрисовка предметов: {items?.Count ?? 0}");
    if (items.Count == 0) return;
    
    var isoPos = IsoUtils.GridToIso(centerX, centerY);
    
    int maxItems;
    Vector2 startPos;
    
    if (placed.Size.X == 1 && placed.Size.Y == 1)
    {
        maxItems = 1;
        startPos = new Vector2(GridOffsetX + isoPos.X + 20, GridOffsetY + isoPos.Y - 51);
    }
    else if (placed.Size.X == 2 && placed.Size.Y == 1)
    {
        maxItems = 2;
        // Ваши отрегулированные параметры
        startPos = new Vector2(GridOffsetX + isoPos.X + 5, GridOffsetY + isoPos.Y - 70);
    }
    else
    {
        maxItems = 3;
        startPos = new Vector2(GridOffsetX + isoPos.X - 30, GridOffsetY + isoPos.Y - 50);
    }
    
    int displayCount = Mathf.Min(items.Count, maxItems);
    int iconSize = 24; // Чуть увеличили размер для лучшей видимости деталей (было 16)
    
    // Изометрический шаг для большой витрины
    float isoStepX = IsoUtils.TileWidth / 2f;  // 32
    float isoStepY = IsoUtils.TileHeight / 2f; // 16
    
    for (int i = 0; i < displayCount; i++)
    {
        var item = items[i];
        var resource = GameData.GetResource(item.ResourceId);
        if (resource == null) continue;
        
        Vector2 itemPosition;
        if (placed.Size.X == 2 && placed.Size.Y == 1)
        {
            itemPosition = startPos + new Vector2(i * isoStepX, i * isoStepY);
        }
        else
        {
            int iconsPerRow = (placed.Size.X == 1 && placed.Size.Y == 1) ? 1 : 3;
            int row = i / iconsPerRow;
            int col = i % iconsPerRow;
            int spacing = 8;
            itemPosition = startPos + new Vector2(col * (iconSize + spacing), row * (iconSize + spacing));
        }
        
        // === НОВОЕ: Используем TextureRect вместо ColorRect ===
        var textureRect = new TextureRect();
        textureRect.Position = itemPosition;
        textureRect.Size = new Vector2(iconSize, iconSize);
        textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        textureRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        textureRect.ZIndex = IsoUtils.GetZOrder(centerX, centerY) + 10;
        
        // 1. Загружаем текстуру экспоната
        string texturePath = GetExhibitTexturePath(resource.Id); // Или resource.Name, зависит от вашей структуры
        if (ResourceLoader.Exists(texturePath))
        {
            textureRect.Texture = GD.Load<Texture2D>(texturePath);
        }
        else
        {
            // ЗАПАСНОЙ ВАРИАНТ: Если спрайт не найден, рисуем цветной квадрат по редкости
            var fallbackRect = new ColorRect();
            fallbackRect.Position = itemPosition;
            fallbackRect.Size = new Vector2(iconSize, iconSize);
            fallbackRect.Color = GetRarityColor(resource.Rarity);
            fallbackRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            fallbackRect.ZIndex = IsoUtils.GetZOrder(centerX, centerY) + 10;
            _roomContainer.AddChild(fallbackRect);
            _furnitureRects.Add(fallbackRect);
            continue; // Пропускаем добавление textureRect
        }
        
        // 2. Обработка качества (Damaged)
        if (item.Quality == Quality.Damaged)
        {
            // Затемняем текстуру на 50%
            textureRect.Modulate = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
        else
        {
            // Можно добавить легкий цветовой оттенок в зависимости от редкости
            Color rarityTint = GetRarityColor(resource.Rarity);
            // Смешиваем белый с цветом редкости, чтобы не перекрывать детали спрайта полностью
            textureRect.Modulate = new Color(1f, 1f, 1f, 1f).Lerp(rarityTint, 0.3f); 
        }
        
        _roomContainer.AddChild(textureRect);
        _furnitureRects.Add(textureRect);
    }
}

// === НОВЫЙ МЕТОД: Маппинг ID ресурса на путь к спрайту ===
private string GetExhibitTexturePath(string resourceId)
{
    // Приведите эти строки в соответствие с тем, как у вас называются ресурсы в GameData
    // Например, если resourceId == "dino_skull", вернется путь к черепу.
    
    string lowerId = resourceId.ToLower();
    
    if (lowerId.Contains("skull") || lowerId.Contains("bone"))
        return "res://assets/museum/items/item_skull.png";
    
    if (lowerId.Contains("ammonite") || lowerId.Contains("fossil"))
        return "res://assets/museum/items/item_ammonite.png";
    
    if (lowerId.Contains("egg"))
        return "res://assets/museum/items/item_egg.png";
    
    if (lowerId.Contains("tooth") || lowerId.Contains("artifact"))
        return "res://assets/museum/items/item_tooth.png";
    
    // Если ничего не подошло, возвращаем несуществующий путь, 
    // чтобы сработал запасной вариант с ColorRect
    return "res://assets/museum/items/missing.png"; 
}
    
private void DrawDoors(Room room)
{
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
        
        // Определяем позицию двери на краю комнаты
        Vector2I doorGridPos = door.Position;
        
        switch (door.Direction)
        {
            case Direction.Top:
                doorGridPos = new Vector2I(door.Position.X, 0);
                break;
            case Direction.Bottom:
                doorGridPos = new Vector2I(door.Position.X, room.Height - 1);
                break;
            case Direction.Left:
                doorGridPos = new Vector2I(0, door.Position.Y);
                break;
            case Direction.Right:
                doorGridPos = new Vector2I(room.Width - 1, door.Position.Y);
                break;
        }
        
        var isoPos = IsoUtils.GridToIso(doorGridPos.X, doorGridPos.Y);
        
        Texture2D doorTexture = null;
        if (door.IsExitToStreet || door.HasConnection)
        {
            doorTexture = doorOpenTexture;
        }
        else
        {
            doorTexture = doorLockedTexture;
        }
        
        Control doorControl;
        
        if (doorTexture != null)
        {
            var textureRect = new TextureRect();
            
            float texWidth = doorTexture.GetWidth();
            float texHeight = doorTexture.GetHeight();
            
            // Позиционирование и ориентация двери
            Vector2 doorPosition;
            bool flipHorizontal = false;
            
            switch (door.Direction)
            {
                case Direction.Top:
                    // Верхняя дверь — стандартная ориентация
                    doorPosition = new Vector2(
                        GridOffsetX + isoPos.X - texWidth / 2f,
                        GridOffsetY + isoPos.Y - texHeight + 16
                    );
                    break;
                    
                case Direction.Bottom:
                    // Нижняя дверь — стандартная ориентация
                    doorPosition = new Vector2(
                        GridOffsetX + isoPos.X - texWidth / 2f,
                        GridOffsetY + isoPos.Y - texHeight + 16
                    );
                    break;
                    
                case Direction.Left:
                    // Левая дверь — ОТРАЖАЕМ по горизонтали
                    doorPosition = new Vector2(
                        GridOffsetX + isoPos.X - texWidth + 8,
                        GridOffsetY + isoPos.Y - texHeight / 2f
                    );
                    flipHorizontal = true;
                    break;
                    
                case Direction.Right:
                    // Правая дверь — ОТРАЖАЕМ по горизонтали
                    doorPosition = new Vector2(
                        GridOffsetX + isoPos.X + 8,
                        GridOffsetY + isoPos.Y - texHeight / 2f
                    );
                    flipHorizontal = true;
                    break;
                    
                default:
                    doorPosition = new Vector2(
                        GridOffsetX + isoPos.X - texWidth / 2f,
                        GridOffsetY + isoPos.Y - texHeight + 16
                    );
                    break;
            }
            
            textureRect.Position = doorPosition;
            textureRect.Size = new Vector2(texWidth, texHeight);
            textureRect.Texture = doorTexture;
            textureRect.FlipH = flipHorizontal; // ← КЛЮЧЕВОЕ ИЗМЕНЕНИЕ
            textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            textureRect.ZIndex = IsoUtils.GetZOrder(doorGridPos.X, doorGridPos.Y) + 10;
            
            doorControl = textureRect;
        }
        else
        {
            var colorRect = new ColorRect();
            colorRect.Position = new Vector2(
                GridOffsetX + isoPos.X - 16,
                GridOffsetY + isoPos.Y - 48
            );
            colorRect.Size = new Vector2(32, 48);
            colorRect.Color = door.HasConnection ? new Color(0.3f, 0.9f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            colorRect.ZIndex = IsoUtils.GetZOrder(doorGridPos.X, doorGridPos.Y) + 10;
            
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

        if (_activeFurnitureMenu != null || _inventorySelector != null) 
    {
        return; 
    }
        
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
    var room = MuseumSystem.Instance.GetCurrentRoom();
    var placed = room.PlacedFurnitureList.Find(p => p.InstanceId == placedId);
    
    if (placed != null)
    {
        // 1. Приближаем камеру к мебели
        Vector2 gridCenter = new Vector2(
            placed.Position.X + placed.Size.X / 2f,
            placed.Position.Y + placed.Size.Y / 2f
        );
        ZoomToFurniture(gridCenter);

        // 2. Открываем меню с небольшой задержкой, чтобы зум успел начаться
        CallDeferred(nameof(ShowEnhancedFurnitureMenu), placed);
    }
}
    
    private void ShowEnhancedFurnitureMenu(PlacedFurniture placed)
{
    if (placed == null)
    {
        GD.PrintErr("[RoomView] Placed furniture is null!");
        return;
    }
    
    if (_activeFurnitureMenu != null)
    {
        _activeFurnitureMenu.QueueFree();
        _activeFurnitureMenu = null;
    }

    _activeFurnitureMenu = new PanelContainer();
    AddChild(_activeFurnitureMenu);
    _activeFurnitureMenu.MouseFilter = Control.MouseFilterEnum.Stop;
    _activeFurnitureMenu.ZIndex = 100;

    Viewport viewport = GetViewport();
    if (viewport == null)
    {
        GD.PrintErr("[RoomView] Viewport is null!");
        _activeFurnitureMenu.QueueFree();
        return;
    }
    
    Vector2 screenSize = viewport.GetVisibleRect().Size;
    _activeFurnitureMenu.Position = new Vector2(screenSize.X / 2f - 150, screenSize.Y / 2f - 150);
    _activeFurnitureMenu.Size = new Vector2(300, 300); // Увеличили высоту для новой кнопки

    var style = new StyleBoxFlat();
    style.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
    style.BorderWidthLeft = 2; style.BorderWidthTop = 2; style.BorderWidthRight = 2; style.BorderWidthBottom = 2;
    style.BorderColor = new Color(0.8f, 0.7f, 0.4f);
    style.CornerRadiusTopLeft = 8; style.CornerRadiusTopRight = 8;
    style.CornerRadiusBottomLeft = 8; style.CornerRadiusBottomRight = 8;
    style.ContentMarginLeft = 15; style.ContentMarginTop = 15;
    style.ContentMarginRight = 15; style.ContentMarginBottom = 15;
    _activeFurnitureMenu.AddThemeStyleboxOverride("panel", style);

    var vbox = new VBoxContainer();
    vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    vbox.AddThemeConstantOverride("separation", 10);
    _activeFurnitureMenu.AddChild(vbox);

    // Заголовок
    var title = new Label();
    title.Text = placed.Furniture?.DisplayName ?? "Мебель";
    title.HorizontalAlignment = HorizontalAlignment.Center;
    title.AddThemeFontSizeOverride("font_size", 18);
    title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
    vbox.AddChild(title);

    // Секция текущих экспонатов
    var items = placed.GetAllItems();
    var contentsLabel = new Label();
    contentsLabel.Text = items.Count > 0 ? $"Экспонатов: {items.Count}" : "Пусто";
    contentsLabel.AddThemeFontSizeOverride("font_size", 14);
    contentsLabel.HorizontalAlignment = HorizontalAlignment.Center;
    vbox.AddChild(contentsLabel);

    // Кнопка "Убрать первый экспонат" - задизейблена если витрина пуста
    var btnRemove = new Button();
    btnRemove.Text = "Убрать первый экспонат";
    btnRemove.Disabled = (items == null || items.Count == 0); // ← БЛОКИРОВКА
    btnRemove.Pressed += () => OnRemoveFossil(placed);
    vbox.AddChild(btnRemove);

    // Кнопка "Добавить из инвентаря"
    var btnAdd = new Button();
    btnAdd.Text = "Добавить из инвентаря";
    btnAdd.Pressed += () => ShowInventorySelector(placed);
    vbox.AddChild(btnAdd);

    // НОВАЯ КНОПКА: "Продать витрину"
    var btnSell = new Button();
    btnSell.Text = "Продать витрину";
    btnSell.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f)); // Красный текст
    btnSell.Pressed += () => OnSellFurniture(placed);
    vbox.AddChild(btnSell);

    // Кнопка "Закрыть"
    var btnClose = new Button();
    btnClose.Text = "Закрыть";
    btnClose.Pressed += () => 
    {
        if (_activeFurnitureMenu != null)
        {
            _activeFurnitureMenu.QueueFree();
            _activeFurnitureMenu = null;
        }
        ResetZoom();
    };
    vbox.AddChild(btnClose);
    
    GD.Print("[RoomView] Furniture menu created successfully");
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

    private void OnRemoveFossil(PlacedFurniture placed)
{
    var items = placed.GetAllItems();
        GD.Print($"[DEBUG REMOVE] Витрина ID:{placed.InstanceId} | Попытка убрать. Всего предметов: {items?.Count ?? 0}");

    if (items == null || items.Count == 0) 
    {
        GD.Print("[RoomView] Нечего убирать, витрина пуста.");
        return;
    }

    // Берем первый экспонат
    var itemToRemove = items[0]; 

    // ВАЖНО: Вызываем метод у экземпляра placed (PlacedFurniture), передавая И ID, И Quality
    var removedItem = placed.RemoveItem(itemToRemove.ResourceId, itemToRemove.Quality);
    
    if (removedItem != null)
    {
        // Возвращаем предмет в инвентарь
        InventorySystem.Instance.AddItem(removedItem.ResourceId, removedItem.Quality, 1);
        GD.Print($"[RoomView] Предмет {removedItem.ResourceId} возвращен в инвентарь");
        
        // Обновляем вид комнаты
        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
        
        // Перерисовываем меню
        _activeFurnitureMenu?.QueueFree();
        _activeFurnitureMenu = null;
        CallDeferred(nameof(ShowEnhancedFurnitureMenu), placed);
    }
    else
    {
        GD.PrintErr("[RoomView] Ошибка: RemoveItem вернул null!");
    }
}

private void OnSellFurniture(PlacedFurniture placed)
{
    if (placed == null) return;
    
    // Подтверждение продажи (опционально, можно убрать)
    var items = placed.GetAllItems();
    if (items != null && items.Count > 0)
    {
        GD.PrintErr($"[RoomView] ВНИМАНИЕ: Витрина содержит {items.Count} экспонат(ов)! Они будут потеряны при продаже.");
        // Здесь можно добавить диалог подтверждения, если нужно
    }
    
    // Вызываем метод продажи через MuseumSystem
    bool sold = MuseumSystem.Instance.SellFurniture(MuseumSystem.Instance.GetCurrentRoom(), placed);
    
    if (sold)
    {
        GD.Print($"[RoomView] Витрина {placed.Furniture?.DisplayName} продана");
        
        // Закрываем меню
        _activeFurnitureMenu?.QueueFree();
        _activeFurnitureMenu = null;
        
        // Обновляем комнату
        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
        
        // Отдаляем камеру
        ResetZoom();
    }
    else
    {
        GD.PrintErr("[RoomView] Не удалось продать витрину!");
    }
}

private void ShowInventorySelector(PlacedFurniture placed)
{
    if (placed == null)
    {
        GD.PrintErr("[RoomView] Placed furniture is null in inventory selector!");
        return;
    }
    
    if (_inventorySelector != null)
    {
        _inventorySelector.QueueFree();
        _inventorySelector = null;
    }

    _inventorySelector = new PanelContainer();
    AddChild(_inventorySelector);
    _inventorySelector.ZIndex = 200;
    _inventorySelector.MouseFilter = Control.MouseFilterEnum.Stop;
    
    Viewport viewport = GetViewport();
    if (viewport == null)
    {
        GD.PrintErr("[RoomView] Viewport is null in inventory selector!");
        return;
    }
    
    Vector2 screenSize = GetViewport().GetVisibleRect().Size;
    _inventorySelector.Position = new Vector2(screenSize.X / 2f - 200, screenSize.Y / 2f - 250);
    _inventorySelector.Size = new Vector2(400, 500);

    var style = new StyleBoxFlat();
    style.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.98f);
    style.BorderWidthLeft = 2; style.BorderWidthTop = 2; style.BorderWidthRight = 2; style.BorderWidthBottom = 2;
    style.BorderColor = new Color(0.4f, 0.6f, 0.9f);
    style.CornerRadiusTopLeft = 8; style.CornerRadiusTopRight = 8;
    style.CornerRadiusBottomLeft = 8; style.CornerRadiusBottomRight = 8;
    style.ContentMarginLeft = 10; style.ContentMarginTop = 10;
    style.ContentMarginRight = 10; style.ContentMarginBottom = 10;
    _inventorySelector.AddThemeStyleboxOverride("panel", style);

    var vbox = new VBoxContainer();
    vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    _inventorySelector.AddChild(vbox);

    var titleLabel = new Label();
    titleLabel.Text = "Выберите находку для выставки:";
    titleLabel.AddThemeFontSizeOverride("font_size", 16);
    titleLabel.AddThemeColorOverride("font_color", Colors.White);
    vbox.AddChild(titleLabel);

    // ScrollContainer для списка
var scroll = new ScrollContainer();
scroll.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
    vbox.AddChild(scroll);

    var itemList = new VBoxContainer();
    itemList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    itemList.AddThemeConstantOverride("separation", 8);
    scroll.AddChild(itemList);

    // Получаем предметы из инвентаря (отфильтруйте по типу, если нужно, например, только окаменелости)
    var inventoryItems = InventorySystem.Instance.GetAllItems(); 
    
    bool hasItems = false;

    foreach (var invItem in inventoryItems)
    {
        var resource = GameData.GetResource(invItem.ResourceId);
    if (resource == null) continue;

if (resource is FossilDefinition fossil && !fossil.CanExhibitAlone)
{
    continue; 
}

    hasItems = true;
    var itemRow = new HBoxContainer();
        itemRow.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        itemRow.AddThemeConstantOverride("separation", 10);

        // Спрайт находки 24x24
        var iconRect = new TextureRect();
        iconRect.Size = new Vector2(24, 24);
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        string texPath = GetExhibitTexturePath(resource.Id); // Используем ваш метод маппинга
        if (ResourceLoader.Exists(texPath))
        {
            iconRect.Texture = GD.Load<Texture2D>(texPath);
        }
        else
        {
            // Фоллбэк: цветной квадрат
            var fallback = new ColorRect();
            fallback.Size = new Vector2(24, 24);
            fallback.Color = GetRarityColor(resource.Rarity);
            itemRow.AddChild(fallback);
        }
        itemRow.AddChild(iconRect);

        // Информация о предмете
        var infoVbox = new VBoxContainer();
        var nameLabel = new Label();
        nameLabel.Text = resource.DisplayName;
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.AddThemeColorOverride("font_color", GetRarityColor(resource.Rarity));
        
        var qualityLabel = new Label();
        qualityLabel.Text = invItem.Quality.ToString();
        qualityLabel.AddThemeFontSizeOverride("font_size", 12);
        qualityLabel.AddThemeColorOverride("font_color", invItem.Quality == Quality.Damaged ? new Color(0.8f, 0.4f, 0.4f) : Colors.LightGray);

        infoVbox.AddChild(nameLabel);
        infoVbox.AddChild(qualityLabel);
        itemRow.AddChild(infoVbox);

        // Кнопка "Выставить"
        // Кнопка "Выставить"
        var addBtn = new Button();
        addBtn.Text = "Выставить";
        addBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        
        // Сохраняем значения в локальные переменные для замыкания
        string itemResourceId = invItem.ResourceId;
        Quality itemQuality = invItem.Quality;
        PlacedFurniture targetPlaced = placed; // Ссылка на текущую витрину

        addBtn.Pressed += () => 
        {
            GD.Print($"[DEBUG UI] Нажата кнопка 'Выставить' для {itemResourceId}");
            
            var currentRoom = MuseumSystem.Instance.GetCurrentRoom();
            
            // Находим витрину по ID
            var realPlaced = currentRoom.PlacedFurnitureList.FirstOrDefault(p => p.InstanceId == targetPlaced.InstanceId);
            
            if (realPlaced == null)
            {
                GD.PrintErr("[RoomView] Витрина не найдена!");
                return;
            }

            bool success = MuseumSystem.Instance.TryAddItemToFurniture(currentRoom, realPlaced, itemResourceId, itemQuality);
            
            if (success)
            {
                InventorySystem.Instance.RemoveItem(itemResourceId, itemQuality, 1);
                GD.Print($"[RoomView] Успешно добавлено: {itemResourceId}");
                
                _inventorySelector?.QueueFree();
                _inventorySelector = null;
                
                var museum = GetTree().CurrentScene as Museum;
                museum?.RefreshRoomView();
                
                _activeFurnitureMenu?.QueueFree();
                _activeFurnitureMenu = null;
                ResetZoom();
            }
            else
            {
                GD.PrintErr("[RoomView] Не удалось добавить предмет");
            }
        };
        itemRow.AddChild(addBtn);

        itemList.AddChild(itemRow);
    }

    if (!hasItems)
    {
        var emptyLabel = new Label();
        emptyLabel.Text = "В инвентаре нет доступных находок.";
        emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
        itemList.AddChild(emptyLabel);
    }

    // Кнопка закрытия
    var closeBtn = new Button();
    closeBtn.Text = "Назад";
    closeBtn.Pressed += () => 
    {
        _inventorySelector?.QueueFree();
        _inventorySelector = null;
    };
    vbox.AddChild(closeBtn);
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

    public void ZoomToFurniture(Vector2 furnitureGridPos)
{
    if (_zoomTween != null) _zoomTween.Kill();
    _zoomTween = CreateTween();

    Vector2 targetScale = new Vector2(1.5f, 1.5f); // Масштаб приближения
    
    // Вычисляем изометрическую позицию центра мебели
    var isoPos = IsoUtils.GridToIso((int)furnitureGridPos.X, (int)furnitureGridPos.Y);
    
    // Мы хотим, чтобы эта точка оказалась примерно в центре экрана при зуме
    Vector2 screenSize = GetViewport().GetVisibleRect().Size;
    Vector2 targetContainerPos = (screenSize / 2f) - (new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y) * targetScale);

    _zoomTween.TweenProperty(_roomContainer, "scale", targetScale, 0.3f).SetTrans(Tween.TransitionType.Cubic);
    _zoomTween.Parallel().TweenProperty(_roomContainer, "position", targetContainerPos, 0.3f).SetTrans(Tween.TransitionType.Cubic);
}

public void ResetZoom()
{
    if (_zoomTween != null) _zoomTween.Kill();
    _zoomTween = CreateTween();
    
    _zoomTween.TweenProperty(_roomContainer, "scale", Vector2.One, 0.3f).SetTrans(Tween.TransitionType.Cubic);
    _zoomTween.Parallel().TweenProperty(_roomContainer, "position", Vector2.Zero, 0.3f).SetTrans(Tween.TransitionType.Cubic);
}
}