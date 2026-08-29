using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class RoomViewUI : CanvasLayer
{
    private Control _roomContainer;
    private ColorRect _background;
    private List<Control> _gridCells = new();
    private List<Control> _furnitureRects = new();
    private List<Control> _doorRects = new();
    private Label _roomNameLabel;

    private Tween _zoomTween;
    private Control _activeFurnitureMenu;
    private Control _inventorySelector;

    private string _placementCollectionId = null;
    private Vector2I? _placementPreviewPos = null;
    private bool _isPlacementValid = false;
    private List<ColorRect> _placementPreviewRects = new();

    private PlacedFurniture _movingCollection = null; // Для режима перемещения
    private PlacedFurniture _movingDisplayCase = null; // Для перемещения витрины

    // Используем изометрические размеры
    private const int CellWidth = IsoUtils.TileWidth;
    private const int CellHeight = IsoUtils.TileHeight;
    private const int GridOffsetX = 500; // Центрирование сетки
    private const int GridOffsetY = 100;

    public override void _Ready()
    {
        Layer = 1;

        _roomContainer = new Control();
        _roomContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _roomContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_roomContainer);

        if (VisitorManager.Instance != null)
        {
            VisitorManager.Instance.SetRoomContainer(_roomContainer, GridOffsetX, GridOffsetY);
        }

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

        InventoryUI.OnStartPlacement += StartPlacementMode;

        GD.Print("[RoomViewUI] Ready (Isometric Mode)");
    }

    public override void _ExitTree()
    {
        InventoryUI.OnStartPlacement -= StartPlacementMode;
    }

    public override void _Process(double delta)
    {
        // Если активен режим размещения — обновляем превью каждый кадр
        if (_placementCollectionId != null)
        {
            UpdatePlacementPreview();
        }
    }


    public void DisplayRoom(Room room)
    {
        GD.Print($"[RoomViewUI] DisplayRoom: {room.DisplayName}, furniture: {room.PlacedFurnitureList.Count}");

        ClearView();

        _roomNameLabel.Text = room.DisplayName;

        DrawGrid(room);
        DrawWalls(room);
        DrawFurniture(room);
        DrawDoors(room);
        DrawOccupancyGrid(room);
    }

       private void DrawGrid(Room room)
    {
        for (int x = 0; x < room.Width; x++)
        {
            for (int y = 0; y < room.Height; y++)
            {
                var isoPos = IsoUtils.GridToIso(x, y);
                
                // === ШАХМАТНЫЙ ПОРЯДОК ===
                // Если (x + y) чётное — тайл 1, если нечётное — тайл 2
                string texturePath = ((x + y) % 2 == 0) 
                    ? "res://assets/museum/floor/1.png" 
                    : "res://assets/museum/floor/2.png";
                // ==========================
                
                Texture2D floorTexture = null;
                if (ResourceLoader.Exists(texturePath))
                {
                    floorTexture = GD.Load<Texture2D>(texturePath);
                }
                else
                {
                    GD.PrintErr($"[RoomViewUI] Текстура пола не найдена: {texturePath}");
                    continue;
                }

                var tileRect = new TextureRect();
                tileRect.Position = new Vector2(
                    GridOffsetX + isoPos.X,
                    GridOffsetY + isoPos.Y
                );
                tileRect.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
                tileRect.Texture = floorTexture;
                tileRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
                tileRect.MouseFilter = Control.MouseFilterEnum.Ignore;
                tileRect.Name = $"floor_{x}_{y}";
                tileRect.ZIndex = IsoUtils.GetZOrder(x, y);
                
                _roomContainer.AddChild(tileRect);
            }
        }
    }

    private bool _showOccupancyDebug = false;

    // === ПУБЛИЧНЫЙ МЕТОД ===
    public void ToggleOccupancyDebug()
    {
        _showOccupancyDebug = !_showOccupancyDebug;
        GD.Print($"[RoomViewUI] Отладка занятости: {_showOccupancyDebug}");

        // Перерисовываем комнату
        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
    }

    private void DrawOccupancyGrid(Room room)
    {
        if (!_showOccupancyDebug)
        {
            GD.Print("[DrawOccupancyGrid] Отладка отключена");
            return;
        }

        GD.Print($"[DrawOccupancyGrid] Рисую занятые клетки для комнаты {room.Id}");

        int occupiedCount = 0;

        for (int x = 0; x < room.Width; x++)
        {
            for (int y = 0; y < room.Height; y++)
            {
                if (room._occupancyGrid[x, y])
                {
                    occupiedCount++;

                    var isoPos = IsoUtils.GridToIso(x, y);
                    var rect = new ColorRect();
                    rect.Position = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y);
                    rect.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
                    rect.Color = new Color(1f, 0f, 0f, 0.5f); // Ярко-красный
                    rect.MouseFilter = Control.MouseFilterEnum.Ignore;
                    rect.ZIndex = IsoUtils.GetZOrder(x, y) + 1;
                    rect.Name = $"occupancy_{x}_{y}";

                    _roomContainer.AddChild(rect);

                    GD.Print($"[DrawOccupancyGrid] Клетка ({x}, {y}) занята");
                }
            }
        }

        GD.Print($"[DrawOccupancyGrid] Всего нарисовано {occupiedCount} занятых клеток");
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

                float texWidth = furnitureTexture.GetWidth();
                float texHeight = furnitureTexture.GetHeight();

                // 1. Горизонтальное центрирование: центр тайла минус половина ширины спрайта
                float posX = GridOffsetX + isoPos.X - (texWidth / 2f);

                // 2. Вертикальное позиционирование: 
                // isoPos.Y + (TileHeight / 2) — это самая нижняя точка изометрического ромба (пол)
                // Вычитаем texHeight, чтобы низ спрайта стоял ровно на этой линии
                float floorLevelY = isoPos.Y + (IsoUtils.TileHeight / 2f);
                float posY = GridOffsetY + floorLevelY - texHeight;

                // === НАСТРОЙКА СМЕЩЕНИЯ ===
                // Если спрайты имеют прозрачные поля снизу или их нужно чуть опустить/поднять глобально, 
                // измените это число. Положительное число опустит мебель ниже, отрицательное поднимет.
                float manualXAdjustment = 0;
                float manualYAdjustment = 0;
                if (placed.Furniture is DisplayCase)
                {
                    manualXAdjustment = 30f;
                    manualYAdjustment = 18f;
                }
                else if (placed.Furniture is CollectionExhibit)
                {
                    manualXAdjustment = 30f;
                    manualYAdjustment = 30f;
                }

                textureRect.Position = new Vector2(posX + manualXAdjustment, posY + manualYAdjustment);

                textureRect.Size = new Vector2(texWidth, texHeight);
                textureRect.Texture = furnitureTexture;
                textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
                textureRect.MouseFilter = Control.MouseFilterEnum.Stop;
                textureRect.Name = $"furniture_{placed.InstanceId}";
                textureRect.SetMeta("placed_id", placed.InstanceId);

                if (placed.IsFlipped)
                {
                    textureRect.FlipH = true;
                }

                int sortX = placed.Position.X + (placed.Size.X / 2);
                int sortY = placed.Position.Y + placed.Size.Y - 1;
                int baseZ = IsoUtils.GetZOrder(sortX, sortY) + 2;

                textureRect.ZIndex = baseZ;

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
                else if (placed.Furniture is CollectionExhibit)
                    colorRect.Color = new Color(0.9f, 0.7f, 0.3f, 0.8f);

                colorRect.MouseFilter = Control.MouseFilterEnum.Stop;
                colorRect.Name = $"furniture_{placed.InstanceId}";
                colorRect.SetMeta("placed_id", placed.InstanceId);
                colorRect.ZIndex = IsoUtils.GetZOrder(centerX, centerY) + 5;

                furnitureControl = colorRect;
            }

            _roomContainer.AddChild(furnitureControl);
            _furnitureRects.Add(furnitureControl);

            // Рисуем экспонаты поверх мебели
            DrawFurnitureContents(placed, centerX, centerY, 99);
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

        else if (furniture is CollectionExhibit exhibit)
        {
            // Получаем определение коллекции по её ID (например, "triceratops")
            var collection = GameData.GetCollection(exhibit.TypeId);

            if (collection != null && !string.IsNullOrEmpty(collection.TexturePath))
            {
                return collection.TexturePath;
            }

            GD.PrintErr($"[RoomView] Текстура не найдена для коллекции: {exhibit.TypeId}");
        }

        return "";
    }

    // === ИЗМЕНЕНА СИГНАТУРА: добавлен параметр baseZIndex ===
    private void DrawFurnitureContents(PlacedFurniture placed, int sortX, int sortY, int baseZIndex)
    {
        var items = placed.GetAllItems();
        GD.Print($"[DEBUG DRAW] Мебель ID:{placed.InstanceId} | Предметов: {items?.Count ?? 0}");
        if (items == null || items.Count == 0) return;

        // Используем sortX и sortY для позиционирования, если нужно, но главное - для Z
        var isoPos = IsoUtils.GridToIso(sortX, sortY);

        int maxItems = 1;
        Vector2 startPos;
        int iconSize = 24;

        // === 1. ЛОГИКА ДЛЯ ПЬЕДЕСТАЛОВ ===
        if (placed.Furniture is CollectionExhibit)
        {
            maxItems = 1;
            iconSize = 96;

            if (placed.Size.X == 2 && placed.Size.Y == 2)
            {
                startPos = new Vector2(GridOffsetX + isoPos.X - 40, GridOffsetY + isoPos.Y - 130);
            }
            else if (placed.Size.X == 3 && placed.Size.Y == 3)
            {
                startPos = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y - 50);
            }
            else
            {
                startPos = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y - 40);
            }
        }
        // === 2. ЛОГИКА ДЛЯ ВИТРИН ===
        else if (placed.Furniture is DisplayCase)
        {
            if (placed.Size.X == 1 && placed.Size.Y == 1)
            {
                maxItems = 1;
                startPos = new Vector2(GridOffsetX + isoPos.X + 20, GridOffsetY + isoPos.Y - 35);
            }
            else if (placed.Size.X == 2 && placed.Size.Y == 1)
            {
                maxItems = 2;
                startPos = new Vector2(GridOffsetX + isoPos.X + 5, GridOffsetY + isoPos.Y - 57);
            }
            else
            {
                maxItems = 3;
                startPos = new Vector2(GridOffsetX + isoPos.X - 30, GridOffsetY + isoPos.Y - 50);
            }
        }
        else
        {
            startPos = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y - 40);
        }

        int displayCount = Mathf.Min(items.Count, maxItems);
        float isoStepX = IsoUtils.TileWidth / 2f;
        float isoStepY = IsoUtils.TileHeight / 2f;

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

            var textureRect = new TextureRect();
            textureRect.Position = itemPosition;
            textureRect.CustomMinimumSize = new Vector2(iconSize, iconSize);
            textureRect.Size = new Vector2(iconSize, iconSize);
            textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            textureRect.MouseFilter = Control.MouseFilterEnum.Ignore;

            // === ИСПРАВЛЕНИЕ: Используем baseZIndex + 1 вместо + 10 ===
            // +1 гарантирует, что предмет лежит "на" витрине, но не перекрывает посетителя, 
            // который стоит на клетке с большим Y (ближе к камере).
            textureRect.ZIndex = baseZIndex + 1;

            string texturePath = GetExhibitTexturePath(resource.Id);
            if (ResourceLoader.Exists(texturePath))
            {
                textureRect.Texture = GD.Load<Texture2D>(texturePath);
            }
            else
            {
                var fallbackRect = new ColorRect();
                fallbackRect.Position = itemPosition;
                fallbackRect.Size = new Vector2(iconSize, iconSize);
                fallbackRect.Color = GetRarityColor(resource.Rarity); // Убедитесь, что этот метод у вас есть
                fallbackRect.MouseFilter = Control.MouseFilterEnum.Ignore;
                fallbackRect.ZIndex = baseZIndex + 1; // То же самое для запасного варианта
                _roomContainer.AddChild(fallbackRect);
                _furnitureRects.Add(fallbackRect);
                continue;
            }

            _roomContainer.AddChild(textureRect);
            _furnitureRects.Add(textureRect);
        }
    }

    private string GetExhibitTexturePath(string resourceId)
    {
        // 1. Сначала проверяем, не является ли это собранной коллекцией
        var collection = GameData.GetCollection(resourceId);
        if (collection != null)
        {
            return collection.TexturePath;
        }

        // 2. Проверяем папку common (золото, яйца, самоцветы и т.д.)
        string commonPath = $"res://assets/museum/items/common/{resourceId}.png";
        if (ResourceLoader.Exists(commonPath))
        {
            return commonPath;
        }

        // 3. Запасной вариант
        return "res://icon.svg";
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
                textureRect.ZIndex = IsoUtils.GetZOrder(doorGridPos.X, doorGridPos.Y) + 3;

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

        // === ОБРАБОТКА РЕЖИМА РАЗМЕЩЕНИЯ ===
        if (_placementCollectionId != null)
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.Escape)
            {
                CancelPlacement();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (@event is InputEventMouseButton mouseEvent2 && mouseEvent2.Pressed)
            {
                if (mouseEvent2.ButtonIndex == MouseButton.Left && _isPlacementValid && _placementPreviewPos.HasValue)
                {
                    PlaceCollection(_placementCollectionId, _placementPreviewPos.Value);
                }
                else if (mouseEvent2.ButtonIndex == MouseButton.Right)
                {
                    CancelPlacement();
                }
                GetViewport().SetInputAsHandled();
                return;
            }
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
        if (placed == null) return;

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
        Vector2 screenSize = viewport.GetVisibleRect().Size;

        // Увеличиваем высоту панели для двух строк ===
        float panelWidth = 500;
        float panelHeight = 110; // Было 80, стало 110
        _activeFurnitureMenu.Position = new Vector2(
            (screenSize.X - panelWidth) / 2f,
            screenSize.Y - panelHeight - 20
        );
        _activeFurnitureMenu.Size = new Vector2(panelWidth, panelHeight);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        style.BorderWidthLeft = 2; style.BorderWidthTop = 2; style.BorderWidthRight = 2; style.BorderWidthBottom = 2;
        style.BorderColor = new Color(0.8f, 0.7f, 0.4f);
        style.CornerRadiusTopLeft = 8; style.CornerRadiusTopRight = 8;
        style.CornerRadiusBottomLeft = 8; style.CornerRadiusBottomRight = 8;
        style.ContentMarginLeft = 15; style.ContentMarginTop = 10;
        style.ContentMarginRight = 15; style.ContentMarginBottom = 10;
        _activeFurnitureMenu.AddThemeStyleboxOverride("panel", style);

        // Вертикальная раскладка (название сверху, кнопки снизу) ===
        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 8); // Отступ между строками
        _activeFurnitureMenu.AddChild(vbox);

        // === СТРОКА 1: Название мебели ===
        var title = new Label();
        title.Text = placed.Furniture?.DisplayName ?? "Мебель";
        title.AddThemeFontSizeOverride("font_size", 18);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        vbox.AddChild(title);

        // === СТРОКА 2: Кнопки ===
        var hbox = new HBoxContainer();
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        hbox.AddThemeConstantOverride("separation", 10);
        hbox.Alignment = BoxContainer.AlignmentMode.Center; // Центрируем кнопки
        vbox.AddChild(hbox);

        // === КНОПКИ В ЗАВИСИМОСТИ ОТ ТИПА МЕБЕЛИ ===
        if (placed.Furniture is CollectionExhibit)
        {
            var btnReturn = new Button();
            btnReturn.Text = " На склад";
            btnReturn.CustomMinimumSize = new Vector2(110, 0);
            btnReturn.Pressed += () => OnReturnCollectionToInventory(placed);
            hbox.AddChild(btnReturn);

            var btnMove = new Button();
            btnMove.Text = "🔄 Переместить";
            btnMove.CustomMinimumSize = new Vector2(130, 0);
            btnMove.Pressed += () => OnStartMovingCollection(placed);
            hbox.AddChild(btnMove);

            var btnRotate = new Button();
            btnRotate.Text = "🔃 Повернуть";
            btnRotate.CustomMinimumSize = new Vector2(120, 0);
            btnRotate.Pressed += () => OnRotateFurniture(placed);
            hbox.AddChild(btnRotate);

            var btnSell = new Button();
            btnSell.Text = "💰 Продать";
            btnSell.CustomMinimumSize = new Vector2(110, 0);
            btnSell.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.4f));
            btnSell.Pressed += () => OnSellCollection(placed);
            hbox.AddChild(btnSell);
        }
        else if (placed.Furniture is DisplayCase)
        {
            var items = placed.GetAllItems();

            var btnRemove = new Button();
            btnRemove.Text = "Убрать экспонат";
            btnRemove.CustomMinimumSize = new Vector2(130, 0);
            btnRemove.Disabled = (items == null || items.Count == 0);
            btnRemove.Pressed += () => OnRemoveFossil(placed);
            hbox.AddChild(btnRemove);

            var btnAdd = new Button();
            btnAdd.Text = "Добавить";
            btnAdd.CustomMinimumSize = new Vector2(110, 0);
            btnAdd.Pressed += () => ShowInventorySelector(placed);
            int currentItems = placed.GetAllItems().Count;
            int maxCapacity = placed.GetMaxCapacity();
            btnAdd.Disabled = (currentItems >= maxCapacity);
            hbox.AddChild(btnAdd);

            var btnMove = new Button();
            btnMove.Text = "🔄 Переместить";
            btnMove.CustomMinimumSize = new Vector2(130, 0);
            btnMove.Pressed += () => OnStartMovingDisplayCase(placed);
            hbox.AddChild(btnMove);

            var btnRotateCase = new Button();
            btnRotateCase.Text = "🔃 Повернуть";
            btnRotateCase.CustomMinimumSize = new Vector2(120, 0);
            btnRotateCase.Pressed += () => OnRotateFurniture(placed);
            bool isSquareCase = placed.Size.X == placed.Size.Y;
            btnRotateCase.Disabled = !isSquareCase;
            hbox.AddChild(btnRotateCase);

            var btnSell = new Button();
            btnSell.Text = "Продать витрину";
            btnSell.CustomMinimumSize = new Vector2(130, 0);
            btnSell.AddThemeColorOverride("font_color", new Color(1f, 0.5f, 0.5f));
            btnSell.Pressed += () => OnSellFurniture(placed);
            hbox.AddChild(btnSell);
        }

        // Кнопка закрытия (теперь точно влезет)
        var btnClose = new Button();
        btnClose.Text = "✖";
        btnClose.CustomMinimumSize = new Vector2(50, 0);
        btnClose.Pressed += () =>
        {
            _activeFurnitureMenu?.QueueFree();
            _activeFurnitureMenu = null;
            ResetZoom();
        };
        hbox.AddChild(btnClose);
    }

    // === КНОПКИ В ЗАВИСИМОСТИ ОТ ТИПА МЕ
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

            if (placed.Furniture is CollectionExhibit)
            {
                // Для пьедестала показываем ТОЛЬКО собранные коллекции
                bool isAssembledCollection = GameData.GetCollection(invItem.ResourceId) != null;
                if (!isAssembledCollection)
                {
                    continue; // Пропускаем обычные фрагменты
                }
            }
            else
            {
                if (GameData.GetCollection(invItem.ResourceId) != null)
                {
                    continue;
                }
                // Для витрин показываем только окаменелости, которые можно выставить по отдельности
                // Приводим тип к FossilDefinition, чтобы получить доступ к CanExhibitAlone
                if (resource is FossilDefinition fossil && fossil.CanExhibitAlone)
                {
                    // Всё ок, показываем
                }
                else
                {
                    continue; // Пропускаем всё остальное (части коллекций, минералы и т.д.)
                }
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
            int currentItems = placed.GetAllItems().Count;
            int maxCapacity = placed.GetMaxCapacity();

            addBtn.Disabled = (currentItems >= maxCapacity);

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
        foreach (var rect in _doorRects) rect.QueueFree();

        _gridCells.Clear();
        _furnitureRects.Clear();
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
        if (_activeFurnitureMenu != null)
        {
            _activeFurnitureMenu.QueueFree();
            _activeFurnitureMenu = null;
        }
        if (_inventorySelector != null)
        {
            _inventorySelector.QueueFree();
            _inventorySelector = null;
        }
    }

    private void StartPlacementMode(string objectId)
    {
        _placementCollectionId = objectId;
        GD.Print($"[RoomViewUI] Режим размещения активирован для: {objectId}");
        // Здесь можно добавить временную надпись на экране "ЛКМ - разместить, ПКМ - отмена"
    }

    private void CancelPlacement()
    {
        _placementCollectionId = null;
        _placementPreviewPos = null;
        ClearPlacementPreview();
    }

    private void ClearPlacementPreview()
    {
        foreach (var rect in _placementPreviewRects) rect.QueueFree();
        _placementPreviewRects.Clear();
    }

    private void UpdatePlacementPreview()
    {
        ClearPlacementPreview();
        var room = MuseumSystem.Instance.GetCurrentRoom();
        if (room == null) return;

        // === ОПРЕДЕЛЯЕМ РАЗМЕР ОБЪЕКТА (Коллекция или Витрина) ===
        Vector2I objectSize;
        var collection = GameData.GetCollection(_placementCollectionId);

        if (collection != null)
        {
            // Это собранная коллекция
            objectSize = collection.Size;
        }
        else
        {
            // Это витрина! Определяем размер по ID
            if (_placementCollectionId.Contains("2x1") || _placementCollectionId.Contains("2"))
                objectSize = new Vector2I(2, 1);
            else
                objectSize = new Vector2I(1, 1);
        }

        // === РАСЧЕТ ПОЗИЦИИ МЫШИ В СЕТКЕ ===
        var mousePos = GetViewport().GetMousePosition();
        float relX = mousePos.X - GridOffsetX;
        float relY = mousePos.Y - GridOffsetY;

        int gridX = (int)Mathf.Round((relX / (IsoUtils.TileWidth / 2f) + relY / (IsoUtils.TileHeight / 2f)) / 2f);
        int gridY = (int)Mathf.Round((relY / (IsoUtils.TileHeight / 2f) - relX / (IsoUtils.TileWidth / 2f)) / 2f);

        _placementPreviewPos = new Vector2I(gridX, gridY);

        // === ПРОВЕРКА ВАЛИДНОСТИ ПОЗИЦИИ ===
        _isPlacementValid = CanPlaceObject(objectSize, new Vector2I(gridX, gridY), room);

        // === ОТРИСОВКА ПРЕВЬЮ (Зеленые/Красные квадраты) ===
        for (int x = 0; x < objectSize.X; x++)
        {
            for (int y = 0; y < objectSize.Y; y++)
            {
                int checkX = gridX + x;
                int checkY = gridY + y;

                if (checkX >= -1 && checkX < room.Width + 1 && checkY >= -1 && checkY < room.Height + 1)
                {
                    var iso = IsoUtils.GridToIso(checkX, checkY);
                    var rect = new ColorRect();
                    rect.Position = new Vector2(GridOffsetX + iso.X, GridOffsetY + iso.Y);
                    rect.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);

                    // Красный цвет, если клетка за пределами допустимой зоны (включая отступ от стен)
                    bool isValidCell = checkX >= 1 && checkX < room.Width - 1 &&
                                       checkY >= 1 && checkY < room.Height - 1 &&
                                       !room._occupancyGrid[checkX, checkY];

                    rect.Color = (_isPlacementValid && isValidCell)
                        ? new Color(0.2f, 0.8f, 0.2f, 0.5f)
                        : new Color(0.8f, 0.2f, 0.2f, 0.5f);

                    rect.MouseFilter = Control.MouseFilterEnum.Ignore;
                    rect.ZIndex = IsoUtils.GetZOrder(checkX, checkY) + 2;

                    _roomContainer.AddChild(rect);
                    _placementPreviewRects.Add(rect);
                }
            }
        }
    }

    private bool CanPlaceObject(Vector2I size, Vector2I pos, Room room)
    {
        // Отступ от стен комнаты (минимум 1 клетка)
        const int wallMargin = 1;

        for (int x = 0; x < size.X; x++)
        {
            for (int y = 0; y < size.Y; y++)
            {
                int checkX = pos.X + x;
                int checkY = pos.Y + y;

                // Проверка: клетка должна быть ВНУТРИ комнаты с отступом от стен
                if (checkX < wallMargin || checkX >= room.Width - wallMargin)
                    return false;
                if (checkY < wallMargin || checkY >= room.Height - wallMargin)
                    return false;

                // Проверка: клетка не должна быть занята
                if (room._occupancyGrid[checkX, checkY])
                    return false;
            }
        }
        return true;
    }

    private void PlaceCollection(string collectionId, Vector2I pos)
    {
        var room = MuseumSystem.Instance.GetCurrentRoom();

        // 1. Логика перемещения ВИТРИНЫ
        if (_movingDisplayCase != null)
        {
            _movingDisplayCase.Position = pos;
            room.PlacedFurnitureList.Add(_movingDisplayCase);

            for (int x = 0; x < _movingDisplayCase.Size.X; x++)
            {
                for (int y = 0; y < _movingDisplayCase.Size.Y; y++)
                {
                    room._occupancyGrid[pos.X + x, pos.Y + y] = true;
                }
            }
            _movingDisplayCase = null;
            GD.Print($"[RoomView] Витрина перемещена на {pos}");
        }
        else if (_movingCollection != null)
        {
            _movingCollection.Position = pos;
            room.PlacedFurnitureList.Add(_movingCollection);

            for (int x = 0; x < _movingCollection.Size.X; x++)
            {
                for (int y = 0; y < _movingCollection.Size.Y; y++)
                {
                    room._occupancyGrid[pos.X + x, pos.Y + y] = true;
                }
            }
            _movingCollection = null;
            GD.Print($"[RoomView] Коллекция перемещена на {pos}");
        }
        else
        {
            // РЕЖИМ РАЗМЕЩЕНИЯ: создаем новую коллекцию из инвентаря
            var collection = GameData.GetCollection(collectionId);
            if (collection != null)
            {
                PlaceCollectionInRoom(collection, pos);
                InventorySystem.Instance.RemoveItem(collectionId, Quality.Good, 1);
            }
        }

        CancelPlacement();
        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
    }

    /// <summary>
    /// Размещает собранную коллекцию в текущей комнате как самостоятельный экспонат
    /// </summary>
    public void PlaceCollectionInRoom(CollectionDefinition collection, Vector2I position)
    {
        // 1. Получаем текущую комнату
        // Если GetCurrentRoom() не работает, попробуйте заменить на CurrentRoom (если это свойство)
        // или MuseumSystem.Instance.GetCurrentRoom() (если класс статический).
        var room = MuseumSystem.Instance.GetCurrentRoom();
        if (room == null)
        {
            GD.PrintErr("[MuseumSystem] Нет текущей комнаты!");
            return;
        }

        var exhibit = new CollectionExhibit(collection);

        // 2. Генерируем уникальный ID, чтобы не зависеть от _instanceCounter
        // Это создаст ID вроде "furn_8a7b9c2d", что гарантирует уникальность
        string newId = $"furn_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

        var placed = new PlacedFurniture
        {
            InstanceId = newId,
            FurnitureTypeId = collection.Id,
            Position = position,
            Size = collection.Size,
            Furniture = exhibit
        };

        room.PlacedFurnitureList.Add(placed);

        // 3. Обновляем сетку занятости
        for (int x = 0; x < collection.Size.X; x++)
        {
            for (int y = 0; y < collection.Size.Y; y++)
            {
                int checkX = position.X + x;
                int checkY = position.Y + y;

                if (checkX >= 0 && checkX < room.Width && checkY >= 0 && checkY < room.Height)
                {
                    room._occupancyGrid[checkX, checkY] = true;
                }
            }
        }

        GD.Print($"[MuseumSystem] Коллекция {collection.DisplayName} размещена!");
    }

    /// <summary>
    /// Вернуть коллекцию обратно в инвентарь
    /// </summary>
    private void OnReturnCollectionToInventory(PlacedFurniture placed)
    {
        if (placed.Furniture is not CollectionExhibit exhibit) return;

        var collectionId = exhibit.TypeId;
        var room = MuseumSystem.Instance.GetCurrentRoom();

        // Удаляем из комнаты
        room.PlacedFurnitureList.Remove(placed);

        // Освобождаем клетки в сетке
        for (int x = 0; x < placed.Size.X; x++)
        {
            for (int y = 0; y < placed.Size.Y; y++)
            {
                int checkX = placed.Position.X + x;
                int checkY = placed.Position.Y + y;
                if (checkX >= 0 && checkX < room.Width && checkY >= 0 && checkY < room.Height)
                {
                    room._occupancyGrid[checkX, checkY] = false;
                }
            }
        }

        // Добавляем обратно в инвентарь
        InventorySystem.Instance.AddItem(collectionId, Quality.Good, 1);

        GD.Print($"[RoomView] Коллекция {collectionId} возвращена в инвентарь");

        _activeFurnitureMenu?.QueueFree();
        _activeFurnitureMenu = null;
        ResetZoom();

        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
    }

    /// <summary>
    /// Начать режим перемещения коллекции
    /// </summary>
    private void OnStartMovingCollection(PlacedFurniture placed)
    {
        if (placed.Furniture is not CollectionExhibit exhibit) return;

        var collectionId = exhibit.TypeId;
        var room = MuseumSystem.Instance.GetCurrentRoom();

        // Временно удаляем из комнаты (но не в инвентарь)
        room.PlacedFurnitureList.Remove(placed);

        // Освобождаем старые клетки
        for (int x = 0; x < placed.Size.X; x++)
        {
            for (int y = 0; y < placed.Size.Y; y++)
            {
                int checkX = placed.Position.X + x;
                int checkY = placed.Position.Y + y;
                if (checkX >= 0 && checkX < room.Width && checkY >= 0 && checkY < room.Height)
                {
                    room._occupancyGrid[checkX, checkY] = false;
                }
            }
        }

        // Активируем режим размещения для этой же коллекции
        _activeFurnitureMenu?.QueueFree();
        _activeFurnitureMenu = null;
        ResetZoom();

        // Запускаем режим размещения
        StartPlacementMode(collectionId);

        // Сохраняем ссылку на перемещаемый объект, чтобы не создавать новый
        _movingCollection = placed;

        GD.Print($"[RoomView] Начато перемещение коллекции {collectionId}");
    }

    private void OnStartMovingDisplayCase(PlacedFurniture placed)
    {
        var room = MuseumSystem.Instance.GetCurrentRoom();

        // 1. Временно убираем витрину из списка комнаты
        room.PlacedFurnitureList.Remove(placed);

        // 2. Освобождаем клетки в сетке
        for (int x = 0; x < placed.Size.X; x++)
        {
            for (int y = 0; y < placed.Size.Y; y++)
            {
                int checkX = placed.Position.X + x;
                int checkY = placed.Position.Y + y;
                if (checkX >= 0 && checkX < room.Width && checkY >= 0 && checkY < room.Height)
                {
                    room._occupancyGrid[checkX, checkY] = false;
                }
            }
        }

        // 3. Сохраняем ссылку на витрину и запускаем режим размещения
        _movingDisplayCase = placed;

        _activeFurnitureMenu?.QueueFree();
        _activeFurnitureMenu = null;
        ResetZoom();

        // Передаем ID типа мебели (например, "display_case_1x1") в режим размещения
        StartPlacementMode(placed.FurnitureTypeId);
    }

    /// <summary>
    /// Поворачивает мебель: для квадратных - отзеркаливает, для неквадратных - меняет размер и запускает перемещение
    /// </summary>
    private void OnRotateFurniture(PlacedFurniture placed)
    {
        if (placed == null) return;

        var room = MuseumSystem.Instance.GetCurrentRoom();
        if (room == null) return;

        // Проверяем, квадратный ли объект
        bool isSquare = placed.Size.X == placed.Size.Y;

        if (isSquare)
        {
            // === КВАДРАТНЫЙ: просто отзеркаливаем текстуру ===
            placed.IsFlipped = !placed.IsFlipped;
            GD.Print($"[RoomView] Объект {placed.FurnitureTypeId} отзеркален: {placed.IsFlipped}");

            // Закрываем меню и обновляем вид
            _activeFurnitureMenu?.QueueFree();
            _activeFurnitureMenu = null;
            ResetZoom();

            var museum = GetTree().CurrentScene as Museum;
            museum?.RefreshRoomView();
        }
        else
        {
            // === НЕКВАДРАТНЫЙ: меняем размер местами и запускаем перемещение ===
            GD.Print($"[RoomView] Объект {placed.FurnitureTypeId} неквадратный, меняем размер с {placed.Size} на ({placed.Size.Y}x{placed.Size.X})");

            // Временно убираем из комнаты
            room.PlacedFurnitureList.Remove(placed);

            // Освобождаем старые клетки
            for (int x = 0; x < placed.Size.X; x++)
            {
                for (int y = 0; y < placed.Size.Y; y++)
                {
                    int checkX = placed.Position.X + x;
                    int checkY = placed.Position.Y + y;
                    if (checkX >= 0 && checkX < room.Width && checkY >= 0 && checkY < room.Height)
                    {
                        room._occupancyGrid[checkX, checkY] = false;
                    }
                }
            }

            // Меняем размер местами (2x1 → 1x2 или наоборот)
            placed.Size = new Vector2I(placed.Size.Y, placed.Size.X);

            // Закрываем меню
            _activeFurnitureMenu?.QueueFree();
            _activeFurnitureMenu = null;
            ResetZoom();

            // Запускаем режим перемещения
            if (placed.Furniture is DisplayCase)
            {
                _movingDisplayCase = placed;
                StartPlacementMode(placed.FurnitureTypeId);
            }
            else if (placed.Furniture is CollectionExhibit)
            {
                _movingCollection = placed;
                StartPlacementMode(placed.FurnitureTypeId);
            }
        }
    }

    /// <summary>
    /// Продать выставленную коллекцию
    /// </summary>
    private void OnSellCollection(PlacedFurniture placed)
    {
        if (placed.Furniture is not CollectionExhibit exhibit) return;

        var collection = GameData.GetCollection(exhibit.TypeId);
        if (collection == null) return;

        // Рассчитываем цену продажи (например, 50% от базовой стоимости)
        int sellPrice = collection.BaseMuseumIncome * 10; // Или используйте collection.BaseSellPrice, если есть

        // Удаляем из комнаты
        var room = MuseumSystem.Instance.GetCurrentRoom();
        room.PlacedFurnitureList.Remove(placed);

        // Освобождаем клетки
        for (int x = 0; x < placed.Size.X; x++)
        {
            for (int y = 0; y < placed.Size.Y; y++)
            {
                int checkX = placed.Position.X + x;
                int checkY = placed.Position.Y + y;
                if (checkX >= 0 && checkX < room.Width && checkY >= 0 && checkY < room.Height)
                {
                    room._occupancyGrid[checkX, checkY] = false;
                }
            }
        }

        // Начисляем монеты игроку
        // (предполагается, что у вас есть метод добавления монет в EconomySystem или аналогичный)
        // EconomySystem.Instance.AddCoins(sellPrice);

        GD.Print($"[RoomView] Коллекция {collection.DisplayName} продана за {sellPrice} монет");

        _activeFurnitureMenu?.QueueFree();
        _activeFurnitureMenu = null;
        ResetZoom();

        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
    }

       private void DrawWalls(Room room)
    {
        string wallTexturePath = "res://assets/museum/wall/wall.png"; 
        Texture2D wallTexture = null;
        
        if (ResourceLoader.Exists(wallTexturePath))
        {
            wallTexture = GD.Load<Texture2D>(wallTexturePath);
        }
        else
        {
            GD.PrintErr($"[RoomViewUI] Текстура стены не найдена: {wallTexturePath}");
            return;
        }

        float wallWidth = wallTexture.GetWidth();
        float wallHeight = wallTexture.GetHeight();

        // === НАСТРОЙКА СМЕЩЕНИЯ СТЕН ===
        float wallAdjustY = 10f; 
        // =================================

        // === СЕВЕРО-ЗАПАДНАЯ СТЕНА (левая верхняя грань, x=0) ===
        // Убран лишний тайл на y=-1. Теперь от y=0 до y=Height
        for (int y = 0; y <= room.Height; y++)
        {
            var isoPos = IsoUtils.GridToIso(0, y);
            var wallRect = new TextureRect();
            
            wallRect.Position = new Vector2(
                GridOffsetX + isoPos.X,
                GridOffsetY + isoPos.Y - wallHeight + wallAdjustY
            );
            
            wallRect.Size = new Vector2(wallWidth, wallHeight);
            wallRect.Texture = wallTexture;
            wallRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            wallRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            wallRect.Name = $"wall_nw_0_{y}";
            wallRect.FlipH = false;
            wallRect.ZIndex = IsoUtils.GetZOrder(0, y) + 1;
            
            _roomContainer.AddChild(wallRect);
        }

        // === СЕВЕРО-ВОСТОЧНАЯ СТЕНА (правая верхняя грань, y=0) ===
        // Убран лишний тайл на x=-1. Теперь от x=0 до x=Width
        for (int x = 0; x <= room.Width; x++)
        {
            var isoPos = IsoUtils.GridToIso(x, 0);
            var wallRect = new TextureRect();
            
            wallRect.Position = new Vector2(
                GridOffsetX + isoPos.X,
                GridOffsetY + isoPos.Y - wallHeight + wallAdjustY
            );
            
            wallRect.Size = new Vector2(wallWidth, wallHeight);
            wallRect.Texture = wallTexture;
            wallRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            wallRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            wallRect.Name = $"wall_ne_{x}_0";
            wallRect.FlipH = true;
            wallRect.ZIndex = IsoUtils.GetZOrder(x, 0) + 1;
            
            _roomContainer.AddChild(wallRect);
        }
    }
}
