using Godot;
using System.Collections.Generic;

public partial class PlacementModeUI : Node2D
{
    private ColorRect[,] _gridCells;
    private Label _instructionLabel;
    
    private Room _currentRoom;
    private Furniture _furnitureToPlace;
    private PlacedFurniture _placedToMove  = null;
    private Vector2I _hoveredLocalCell = new(-1, -1);

    private TextureRect _ghostSprite;
    
    // Отдельный CanvasLayer для UI-элементов (инструкция, кнопки)
    private CanvasLayer _uiLayer;

    public override void _Ready()
    {
        Visible = false;
        
        _uiLayer = new CanvasLayer();
        _uiLayer.Layer = 50;
        AddChild(_uiLayer);
    }
    
    public override void _Process(double delta)
    {
        if (!Visible) return;
        
        UpdateHoveredCell();
        UpdatePreview();
        
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            CancelPlacement();
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;
        
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            TryPlaceFurniture();
            GetViewport().SetInputAsHandled();
        }
    }
    
    // ===== ЗАПУСК РЕЖИМА =====
    
    public void StartPlacement(Room room, Furniture furniture)
    {
        _currentRoom = room;
        _furnitureToPlace = furniture;
        
        ClearGrid();
        CreateGrid();
        CreateInstructionLabel();
        
        Visible = true;
        GD.Print($"[PlacementMode] Started for {furniture.DisplayName} ({furniture.Size.X}x{furniture.Size.Y})");
    }
    
    // ===== СОЗДАНИЕ СЕТКИ =====
    
    private void CreateGrid()
    {
        // === ИСПРАВЛЕНИЕ 1: GlobalPosition -> GlobalOffset ===
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
                    MuseumConstants.GridOffsetY + isoPos.Y
                );
                
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
    
    private void CreateInstructionLabel()
    {
        _instructionLabel = new Label();
        _instructionLabel.Text = $"Размещение: {_furnitureToPlace.DisplayName} ({_furnitureToPlace.Size.X}x{_furnitureToPlace.Size.Y})\nЛКМ = поставить | Esc = отмена";
        _instructionLabel.Position = new Vector2(20, 20);
        _instructionLabel.AddThemeFontSizeOverride("font_size", 18);
        _instructionLabel.AddThemeColorOverride("font_color", Colors.White);
        
        _uiLayer.AddChild(_instructionLabel);
    }
    
    // ===== ОБНОВЛЕНИЕ ПРЕДПРОСМОТРА =====
    
    private void UpdateHoveredCell()
    {
        var mouseScreenPos = GetViewport().GetMousePosition();
        var camera = GetViewport().GetCamera2D();
        
        Vector2 mouseWorldPos;
        if (camera != null)
        {
            mouseWorldPos = GetGlobalMousePosition();
        }
        else
        {
            mouseWorldPos = mouseScreenPos;
        }
        
        float relativeX = mouseWorldPos.X - MuseumConstants.GridOffsetX;
        float relativeY = mouseWorldPos.Y - MuseumConstants.GridOffsetY;
        
        Vector2I globalHoveredCell = IsoUtils.IsoToGrid(relativeX, relativeY);
        
        // === ИСПРАВЛЕНИЕ 1: GlobalPosition -> GlobalOffset ===
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
            return;
        }       
        
        // === ИСПРАВЛЕНИЕ 2: Проверяем занятость напрямую у Room ===
        bool canPlace = _currentRoom.CanPlaceFurniture(_hoveredLocalCell, _furnitureToPlace.Size);
        UpdateGridColors(canPlace);
        
        UpdateGhostSprite(canPlace);
    }
    
        private void UpdateGhostSprite(bool canPlace)
    {
        // Создаём "призрак" при первом вызове
        if (_ghostSprite == null)
        {
            _ghostSprite = new TextureRect();
            _ghostSprite.MouseFilter = Control.MouseFilterEnum.Ignore;
            _ghostSprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
            _ghostSprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            AddChild(_ghostSprite);
        }
        
        // Загружаем текстуру
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
        
        // Глобальные координаты левого верхнего угла выделения
        int globalX = _currentRoom.GlobalOffset.X + _hoveredLocalCell.X;
        int globalY = _currentRoom.GlobalOffset.Y + _hoveredLocalCell.Y;
        
        // === ИСПРАВЛЕНИЕ: Привязка к НИЖНЕМУ РЯДУ footprint'а ===
        int anchorGridX = globalX + _furnitureToPlace.Size.X - 1;
        int anchorGridY = globalY + _furnitureToPlace.Size.Y - 1; // ← Нижний ряд, а не центр!
        
        var anchorIsoPos = IsoUtils.GridToIso(anchorGridX, anchorGridY);
        
        // Базовая точка привязки: нижний центр нижней клетки объекта
        float centerX = MuseumConstants.GridOffsetX + anchorIsoPos.X + IsoUtils.TileWidth / 2f;
        float centerY = MuseumConstants.GridOffsetY + anchorIsoPos.Y + IsoUtils.TileHeight;

        // Реальный размер текстуры
        var texture = _ghostSprite.Texture;
        float realWidth = texture.GetWidth();
        float realHeight = texture.GetHeight();
        _ghostSprite.Size = new Vector2(realWidth, realHeight);

        // Позиционируем спрайт так, чтобы его нижний центр совпал с точкой привязки
        _ghostSprite.Position = new Vector2(
            centerX - realWidth / 2f,
            centerY - realHeight
        );
        
        // Цвет: зелёный если можно поставить, красный если нельзя
        _ghostSprite.Modulate = canPlace ? new Color(1, 1, 1, 0.6f) : new Color(1, 0.3f, 0.3f, 0.6f);
        
                // Z-индекс призрака: та же логика, что и у финального объекта
        int zAnchorX = globalX + _furnitureToPlace.Size.X / 2;
        int zAnchorY = globalY + _furnitureToPlace.Size.Y / 2;
        _ghostSprite.ZIndex = IsoUtils.GetZOrder(zAnchorX, zAnchorY) + (_furnitureToPlace.Size.X + _furnitureToPlace.Size.Y) / 2;

        _ghostSprite.Visible = true;
    }
    
       private string GetFurnitureTexturePath(Furniture furniture)
    {
        // 1. Если это собранная коллекция (скелет), используем путь к full.png
        if (furniture is CollectionExhibit)
        {
            return $"res://assets/museum/items/{furniture.TypeId}/full.png";
        }

        // 2. Иначе используем стандартные пути для обычной мебели
        if (furniture.TypeId == "display_case_1x1")
            return "res://assets/museum/furniture/display_case_small.png";
            
        if (furniture.TypeId == "display_case_2x2")
            return "res://assets/museum/furniture/display_case_large.png";
        
        // Fallback для любой другой обычной мебели (если появится)
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
                bool isUnderFurniture = localX >= _hoveredLocalCell.X && localX < _hoveredLocalCell.X + _furnitureToPlace.Size.X &&
                                        localY >= _hoveredLocalCell.Y && localY < _hoveredLocalCell.Y + _furnitureToPlace.Size.Y;
                
                if (_gridCells[localX, localY] != null)
                {
                    _gridCells[localX, localY].Color = isUnderFurniture ? highlightColor : defaultColor;
                }
            }
        }
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
        
        if (_instructionLabel != null)
        {
            _instructionLabel.QueueFree();
        }
        
        _instructionLabel = new Label();
        _instructionLabel.Text = $"Перемещение: {_furnitureToPlace.DisplayName}\nЛКМ = установить | Esc = отмена";
        _instructionLabel.Position = new Vector2(20, 20);
        _instructionLabel.AddThemeFontSizeOverride("font_size", 18);
        _instructionLabel.AddThemeColorOverride("font_color", Colors.White);
        _uiLayer.AddChild(_instructionLabel);
        
        Visible = true;
        GD.Print($"[PlacementMode] 📦 Режим перемещения активирован. TypeId: {_placedToMove.FurnitureTypeId}, Экспонатов: {_placedToMove.Items.Count}");
    }
    
    // ===== РАЗМЕЩЕНИЕ И ОТМЕНА =====
    
        private void TryPlaceFurniture()
    {
        if (_hoveredLocalCell.X < 0 || _hoveredLocalCell.Y < 0) return;
        
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
                // Если место занято, возвращаем витрину на старое место
                _currentRoom.PlaceFurniture(_placedToMove);
                GD.Print($"[PlacementMode] ❌ Нельзя переместить сюда: место занято или вне границ!");
            }
        }
        // === СЦЕНАРИЙ Б: Размещение новой мебели из магазина ===
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

        // === ЗАВЕРШЕНИЕ: Если действие успешно, закрываем режим размещения ===
        if (success)
        {
            var museum = GetTree().CurrentScene as Museum;
            museum?.RefreshRoomView();
            
            // Вызываем отмену, которая скроет UI и очистит состояние
            CancelPlacement(); 
        }
    }
    
    private void CancelPlacement()
    {
        Visible = false;
        MuseumSystem.Instance.CancelPlacementMode();
        ClearGrid();
        GD.Print("[PlacementMode] Cancelled");
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
        
        _instructionLabel?.QueueFree();
        _instructionLabel = null;
        
        _ghostSprite?.QueueFree();
        _ghostSprite = null;

        _placedToMove = null;
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
        
        // Обновляем или создаем текст инструкции
        if (_instructionLabel != null)
        {
            _instructionLabel.QueueFree();
        }
        
        _instructionLabel = new Label();
        _instructionLabel.Text = $"Размещение: {_furnitureToPlace.DisplayName}\nЛКМ = установить | Esc = отмена (вернет в инвентарь)";
        _instructionLabel.Position = new Vector2(20, 20);
        _instructionLabel.AddThemeFontSizeOverride("font_size", 18);
        _instructionLabel.AddThemeColorOverride("font_color", Colors.White);
        _uiLayer.AddChild(_instructionLabel);
        
        Visible = true;
        GD.Print($"[PlacementMode] 🦖 Начато размещение коллекции: {furniture.DisplayName}");
    }
}