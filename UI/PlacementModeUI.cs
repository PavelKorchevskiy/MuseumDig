using Godot;
using System.Collections.Generic;

/// <summary>
/// Режим размещения мебели/скелетов под палец:
///   1. Игрок тапает клетку внутри комнаты — туда переносится подсветка и призрак.
///      Выбор клетки приходит от CameraController.OnWorldTap (не от hover мыши,
///      которого на тач-экране не существует).
///   2. Крупная кнопка "Поставить" подтверждает установку в выбранной клетке.
///   3. Крупная кнопка "Отмена" закрывает режим. Если мы ПЕРЕМЕЩАЛИ существующую
///      мебель — она возвращается на прежнее место (раньше при отмене пропадала).
/// </summary>
public partial class PlacementModeUI : Node2D
{
    private ColorRect[,] _gridCells;
    private Room _currentRoom;
    private Furniture _furnitureToPlace;
    private PlacedFurniture _placedToMove = null;
    private Vector2I _hoveredLocalCell = new(-1, -1);

    private TextureRect _ghostSprite;

    private CanvasLayer _uiLayer;
    private Label _titleLabel;
    private Label _hintLabel;
    private Button _cancelBtn;
    private Button _confirmBtn;

    public override void _Ready()
    {
        Visible = false;

        _uiLayer = new CanvasLayer { Layer = 50 };
        AddChild(_uiLayer);

        CreateOverlay();
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        // Esc удобен при тестировании на ПК, на телефоне используется кнопка "Отмена"
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            CancelPlacement();
        }
    }

    // ===== ВХОДНАЯ ТОЧКА ОТ КАМЕРЫ =====

    /// <summary>
    /// Вызывается CameraController при тапе по миру, пока этот режим открыт.
    /// Просто переносит выбранную клетку — установка подтверждается кнопкой.
    /// </summary>
    public void OnWorldTap(Vector2 worldPos)
    {
        if (!Visible || _currentRoom == null) return;

        UpdateHoveredCellFromWorld(worldPos);
        UpdatePreview();
    }

    // ===== ПОСТРОЕНИЕ ПОСТОЯННОГО UI (кнопки, подсказка) =====

    private void CreateOverlay()
    {
        // === Верхняя плашка: название объекта + подсказка ===
        var topBar = new PanelContainer();
        topBar.AnchorLeft = 0.08f;
        topBar.AnchorRight = 0.92f;
        topBar.AnchorTop = 0.04f;
        topBar.AnchorBottom = 0.04f;
        topBar.GrowVertical = Control.GrowDirection.End;
        topBar.MouseFilter = Control.MouseFilterEnum.Ignore; // не мешает тапам по миру

        var topStyle = MobileUI.MakeStyle(new Color(0f, 0f, 0f, 0.6f), 16);
        topStyle.ContentMarginLeft = 18;
        topStyle.ContentMarginRight = 18;
        topStyle.ContentMarginTop = 12;
        topStyle.ContentMarginBottom = 12;
        topBar.AddThemeStyleboxOverride("panel", topStyle);

        var topContent = new VBoxContainer();
        topContent.AddThemeConstantOverride("separation", 4);
        topBar.AddChild(topContent);

        _titleLabel = MobileUI.CreateLabel("", MobileUI.BodyFontSize + 2, Colors.White, center: true, wrap: true);
        topContent.AddChild(_titleLabel);

        _hintLabel = MobileUI.CreateLabel("", MobileUI.SmallFontSize, MobileUI.MutedColor, center: true, wrap: true);
        topContent.AddChild(_hintLabel);

        _uiLayer.AddChild(topBar);

        // === Нижняя панель с крупными кнопками ===
        var bottomBar = new PanelContainer();
        bottomBar.AnchorLeft = 0;
        bottomBar.AnchorRight = 1;
        bottomBar.AnchorTop = 1;
        bottomBar.AnchorBottom = 1;
        bottomBar.OffsetTop = -170;
        bottomBar.OffsetBottom = -20; // отступ от самого низа (жестовая полоса на телефоне)
        bottomBar.MouseFilter = Control.MouseFilterEnum.Stop;

        var bottomStyle = MobileUI.MakeStyle(MobileUI.PanelColor, 20);
        bottomStyle.ContentMarginLeft = 20;
        bottomStyle.ContentMarginRight = 20;
        bottomStyle.ContentMarginTop = 16;
        bottomStyle.ContentMarginBottom = 16;
        bottomBar.AddThemeStyleboxOverride("panel", bottomStyle);

        var buttonsRow = new HBoxContainer();
        buttonsRow.Alignment = BoxContainer.AlignmentMode.Center;
        buttonsRow.AddThemeConstantOverride("separation", 16);
        bottomBar.AddChild(buttonsRow);

        _cancelBtn = MobileUI.CreateButton(MobileUI.Tr("ui.placement.cancel", "Отмена"),
            MobileUI.NeutralColor, MobileUI.ButtonHeight, 220f);
        _cancelBtn.Pressed += CancelPlacement;
        buttonsRow.AddChild(_cancelBtn);

        _confirmBtn = MobileUI.CreateButton(MobileUI.Tr("ui.placement.confirm", "Поставить"),
            MobileUI.PrimaryColor, MobileUI.ButtonHeight, 220f);
        _confirmBtn.Disabled = true;
        _confirmBtn.Pressed += ConfirmPlacement;
        buttonsRow.AddChild(_confirmBtn);

        _uiLayer.AddChild(bottomBar);
    }

    private void SetInfo(string title, string hint)
    {
        _titleLabel.Text = title;
        _hintLabel.Text = hint;
    }

    // ===== ЗАПУСК РЕЖИМА =====

    public void StartPlacement(Room room, Furniture furniture)
    {
        _currentRoom = room;
        _furnitureToPlace = furniture;

        ClearGrid();
        CreateGrid();

        SetInfo(
            furniture.DisplayName,
            MobileUI.Tr("ui.placement.hint_new", "Коснитесь клетки, затем нажмите «Поставить»"));

        ResetSelection();
        Visible = true;
        GD.Print($"[PlacementMode] Started for {furniture.DisplayName} ({furniture.Size.X}x{furniture.Size.Y})");
    }

    /// <summary>
    /// Запускает режим размещения специально для перемещения существующей мебели
    /// </summary>
    public void StartPlacementForMoving(Room room, Furniture furniture, PlacedFurniture placedToMove)
    {
        _currentRoom = room;
        _furnitureToPlace = furniture;

        // 1. Сначала сохраняем ссылку во временную переменную
        var tempSave = placedToMove;

        // 2. Очищаем старое (это обнулит _placedToMove)
        ClearGrid();

        // 3. Восстанавливаем ссылку на оригинальный объект с его TypeId и Items!
        _placedToMove = tempSave;

        CreateGrid();

        SetInfo(
            furniture.DisplayName,
            MobileUI.Tr("ui.placement.hint_move", "Коснитесь новой клетки, затем нажмите «Поставить»"));

        ResetSelection();
        Visible = true;
        GD.Print($"[PlacementMode] 📦 Режим перемещения активирован. TypeId: {_placedToMove.FurnitureTypeId}, Экспонатов: {_placedToMove.Items.Count}");
    }

    /// <summary>
    /// Запускает режим размещения специально для собранных коллекций (скелетов) из инвентаря
    /// </summary>
    public void StartPlacementForCollection(Room room, Furniture furniture)
    {
        _currentRoom = room;
        _furnitureToPlace = furniture;
        _placedToMove = null; // Это новая установка, а не перемещение существующей мебели

        ClearGrid();
        CreateGrid();

        SetInfo(
            furniture.DisplayName,
            MobileUI.Tr("ui.placement.hint_collection", "Коснитесь клетки. «Отмена» вернёт экспонат в инвентарь"));

        ResetSelection();
        Visible = true;
        GD.Print($"[PlacementMode] 🦖 Начато размещение коллекции: {furniture.DisplayName}");
    }

    private void ResetSelection()
    {
        _hoveredLocalCell = new Vector2I(-1, -1);
        _confirmBtn.Disabled = true;
        UpdateGridColors(false);
        if (_ghostSprite != null) _ghostSprite.Visible = false;
    }

    // ===== СОЗДАНИЕ СЕТКИ =====

    private void CreateGrid()
    {
        int startX = _currentRoom.GlobalOffset.X;
        int startY = _currentRoom.GlobalOffset.Y;

        GD.Print($"[PlacementMode] 🛠️ Создание сетки. GlobalOffset комнаты: ({startX}, {startY})");

        _gridCells = new ColorRect[_currentRoom.Width, _currentRoom.Height];

        for (int localX = 0; localX < _currentRoom.Width; localX++)
        {
            for (int localY = 0; localY < _currentRoom.Height; localY++)
            {
                int globalX = startX + localX;
                int globalY = startY + localY;

                var cell = new ColorRect();
                var isoPos = IsoUtils.GridToIso(globalX, globalY);

                cell.Position = new Vector2(
                    MuseumConstants.GridOffsetX + isoPos.X,
                    MuseumConstants.GridOffsetY + isoPos.Y);

                cell.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
                cell.Color = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Серый по умолчанию
                cell.MouseFilter = Control.MouseFilterEnum.Ignore;
                cell.ZIndex = IsoUtils.GetZOrder(globalX, globalY) + 15;

                AddChild(cell);
                _gridCells[localX, localY] = cell;
            }
        }
        GD.Print($"[PlacementMode] ✅ Сетка создана. Всего клеток: {_currentRoom.Width * _currentRoom.Height}");
    }

    // ===== ВЫБОР КЛЕТКИ ПО ТАПУ =====

    private void UpdateHoveredCellFromWorld(Vector2 worldPos)
    {
        float relativeX = worldPos.X - MuseumConstants.GridOffsetX;
        float relativeY = worldPos.Y - MuseumConstants.GridOffsetY;

        Vector2I globalHoveredCell = IsoUtils.IsoToGrid(relativeX, relativeY);

        int localX = globalHoveredCell.X - _currentRoom.GlobalOffset.X;
        int localY = globalHoveredCell.Y - _currentRoom.GlobalOffset.Y;

        if (localX >= 0 && localX < _currentRoom.Width && localY >= 0 && localY < _currentRoom.Height)
        {
            _hoveredLocalCell = new Vector2I(localX, localY);
        }
        else
        {
            _hoveredLocalCell = new Vector2I(-1, -1);
        }
    }

    private void UpdatePreview()
    {
        if (_hoveredLocalCell.X < 0 || _hoveredLocalCell.Y < 0)
        {
            UpdateGridColors(false);
            if (_ghostSprite != null) _ghostSprite.Visible = false;
            _confirmBtn.Disabled = true;
            return;
        }

        bool canPlace = _currentRoom.CanPlaceFurniture(_hoveredLocalCell, _furnitureToPlace.Size);
        UpdateGridColors(canPlace);
        UpdateGhostSprite(canPlace);

        // Подтвердить можно только валидную клетку — тап рядом не ставит мебель вслепую
        _confirmBtn.Disabled = !canPlace;
    }

    private void UpdateGhostSprite(bool canPlace)
    {
        if (_ghostSprite == null)
        {
            _ghostSprite = new TextureRect();
            _ghostSprite.MouseFilter = Control.MouseFilterEnum.Ignore;
            _ghostSprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _ghostSprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            AddChild(_ghostSprite);
        }

        string texturePath = GetFurnitureTexturePath(_furnitureToPlace);
        if (ResourceLoader.Exists(texturePath))
        {
            _ghostSprite.Texture = GD.Load<Texture2D>(texturePath);
        }
        else
        {
            GD.PrintErr($"[PlacementMode] Текстура не найдена: {texturePath}");
            _ghostSprite.Visible = false;
            return;
        }

        int globalX = _currentRoom.GlobalOffset.X + _hoveredLocalCell.X;
        int globalY = _currentRoom.GlobalOffset.Y + _hoveredLocalCell.Y;

        // Привязка к нижнему ряду footprint'а
        int anchorGridX = globalX + _furnitureToPlace.Size.X - 1;
        int anchorGridY = globalY + _furnitureToPlace.Size.Y - 1;

        var anchorIsoPos = IsoUtils.GridToIso(anchorGridX, anchorGridY);

        float centerX = MuseumConstants.GridOffsetX + anchorIsoPos.X + IsoUtils.TileWidth / 2f;
        float centerY = MuseumConstants.GridOffsetY + anchorIsoPos.Y + IsoUtils.TileHeight;

        var texture = _ghostSprite.Texture;
        float realWidth = texture.GetWidth();
        float realHeight = texture.GetHeight();
        _ghostSprite.Size = new Vector2(realWidth, realHeight);

        _ghostSprite.Position = new Vector2(
            centerX - realWidth / 2f,
            centerY - realHeight);

        _ghostSprite.Modulate = canPlace ? new Color(1, 1, 1, 0.6f) : new Color(1, 0.3f, 0.3f, 0.6f);

        int zAnchorX = globalX + _furnitureToPlace.Size.X / 2;
        int zAnchorY = globalY + _furnitureToPlace.Size.Y / 2;
        _ghostSprite.ZIndex = IsoUtils.GetZOrder(zAnchorX, zAnchorY) + (_furnitureToPlace.Size.X + _furnitureToPlace.Size.Y) / 2;

        _ghostSprite.Visible = true;
    }

    private string GetFurnitureTexturePath(Furniture furniture)
    {
        if (furniture is CollectionExhibit)
        {
            return $"res://assets/museum/items/{furniture.TypeId}/full.png";
        }

        if (furniture.TypeId == "display_case_1x1")
            return "res://assets/museum/furniture/display_case_small.png";

        if (furniture.TypeId == "display_case_2x2")
            return "res://assets/museum/furniture/display_case_large.png";

        return $"res://assets/museum/furniture/{furniture.TypeId}.png";
    }

    private void UpdateGridColors(bool isValid)
    {
        Color highlightColor = isValid ? new Color(0.2f, 1.0f, 0.2f, 0.5f) : new Color(1.0f, 0.2f, 0.2f, 0.5f);
        Color defaultColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);

        for (int localX = 0; localX < _currentRoom.Width; localX++)
        {
            for (int localY = 0; localY < _currentRoom.Height; localY++)
            {
                bool isUnderFurniture = _hoveredLocalCell.X >= 0 &&
                    localX >= _hoveredLocalCell.X && localX < _hoveredLocalCell.X + _furnitureToPlace.Size.X &&
                    localY >= _hoveredLocalCell.Y && localY < _hoveredLocalCell.Y + _furnitureToPlace.Size.Y;

                if (_gridCells[localX, localY] != null)
                {
                    _gridCells[localX, localY].Color = isUnderFurniture ? highlightColor : defaultColor;
                }
            }
        }
    }

    // ===== ПОДТВЕРЖДЕНИЕ =====

    private void ConfirmPlacement()
    {
        if (_hoveredLocalCell.X < 0 || _hoveredLocalCell.Y < 0) return;
        TryPlaceFurniture();
    }

    private void TryPlaceFurniture()
    {
        bool success = false;

        // === СЦЕНАРИЙ А: Перемещение существующей мебели ===
        if (_placedToMove != null)
        {
            GD.Print($"[PlacementMode] 🔍 Проверка перемещения. В объекте сейчас {_placedToMove.Items.Count} экспонатов.");

            // Временно убираем, чтобы проверка коллизий не считала её препятствием самой для себя
            _currentRoom.RemoveFurniture(_placedToMove);

            if (_currentRoom.CanPlaceFurniture(_hoveredLocalCell, _placedToMove.Size))
            {
                _placedToMove.Position = _hoveredLocalCell;
                _currentRoom.PlaceFurniture(_placedToMove);
                GD.Print($"[PlacementMode] ✅ Витрина перемещена на ({_hoveredLocalCell.X}, {_hoveredLocalCell.Y})");
                success = true;
            }
            else
            {
                // Место занято — возвращаем на старое место
                _currentRoom.PlaceFurniture(_placedToMove);
                GD.Print($"[PlacementMode] ❌ Нельзя переместить сюда: место занято или вне границ!");
            }
        }
        // === СЦЕНАРИЙ Б: Размещение новой мебели из магазина или инвентаря ===
        else
        {
            if (MuseumSystem.Instance.PlaceFurniture(_currentRoom.Id, _furnitureToPlace, _hoveredLocalCell))
            {
                GD.Print($"[PlacementMode] ✅ Размещено: {_furnitureToPlace.DisplayName} на ({_hoveredLocalCell.X}, {_hoveredLocalCell.Y})");
                success = true;
            }
            else
            {
                GD.Print("[PlacementMode] ❌ Нельзя разместить здесь: место занято или вне границ!");
            }
        }

        if (success)
        {
            var museum = GetTree().CurrentScene as Museum;
            museum?.RefreshRoomView();

            // Объект уже поставлен — закрываем режим БЕЗ возврата на прежнее место
            FinishPlacement();
        }
    }

    // ===== ОТМЕНА =====

    private void CancelPlacement()
    {
        Visible = false;

        if (_placedToMove != null)
        {
            // Раньше при отмене перемещения объект пропадал: он был снят с места
            // ДО открытия этого режима (см. DisplayCaseUI/CollectionExhibitUI.OnMovePressed),
            // а назад никто его не ставил. Возвращаем на прежние координаты.
            _currentRoom.PlaceFurniture(_placedToMove);

            var museum = GetTree().CurrentScene as Museum;
            museum?.RefreshRoomView();

            GD.Print("[PlacementMode] Перемещение отменено, объект возвращён на прежнее место");
        }

        MuseumSystem.Instance.CancelPlacementMode();
        ClearGrid();
        GD.Print("[PlacementMode] Cancelled");
    }

    /// <summary>Закрытие после УСПЕШНОЙ установки — без возврата на прежнее место.</summary>
    private void FinishPlacement()
    {
        Visible = false;
        MuseumSystem.Instance.CancelPlacementMode();
        ClearGrid();
    }

    private void ClearGrid()
    {
        if (_gridCells != null)
        {
            int w = _gridCells.GetLength(0);
            int h = _gridCells.GetLength(1);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    _gridCells[x, y]?.QueueFree();
                }
            }
            _gridCells = null;
        }

        _ghostSprite?.QueueFree();
        _ghostSprite = null;

        _placedToMove = null;
    }
}
