using Godot;
using System.Collections.Generic;

public partial class PlacementModeUI : CanvasLayer
{
    private ColorRect[,] _gridCells;
    private ColorRect _previewGhost;
    private Label _instructionLabel;
    
    private Room _currentRoom;
    private Furniture _furnitureToPlace;
    private Vector2I _hoveredCell = new(-1, -1);
    
    // Размеры ячейки в пикселях
    private const int CellSize = 60;
    private const int GridOffsetX = 100;
    private const int GridOffsetY = 100;
    
    public override void _Ready()
    {
        Layer = 50; // Поверх основного UI, но ниже модалок
        Visible = false;
    }
    
    public override void _Process(double delta)
    {
        if (!Visible) return;
        
        UpdateHoveredCell();
        UpdatePreview();
        
        // Отмена по Esc
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
        CreatePreviewGhost();
        CreateInstructionLabel();
        
        Visible = true;
        GD.Print($"[PlacementMode] Started for {furniture.DisplayName} ({furniture.Size.X}x{furniture.Size.Y})");
    }
    
    // ===== СОЗДАНИЕ СЕТКИ =====
    
    private void CreateGrid()
    {
        _gridCells = new ColorRect[_currentRoom.Width, _currentRoom.Height];
        
        for (int x = 0; x < _currentRoom.Width; x++)
        {
            for (int y = 0; y < _currentRoom.Height; y++)
            {
                var cell = new ColorRect();
                cell.Position = new Vector2(GridOffsetX + x * CellSize, GridOffsetY + y * CellSize);
                cell.Size = new Vector2(CellSize - 2, CellSize - 2); // -2 для зазора между клетками
                
                // Определяем цвет клетки
                bool canPlace = MuseumSystem.Instance.CanPlaceFurnitureAt(_currentRoom, new Vector2I(x, y), _furnitureToPlace.Size);
                cell.Color = canPlace ? new Color(0.2f, 0.8f, 0.2f, 0.3f) : new Color(0.8f, 0.2f, 0.2f, 0.3f);
                
                cell.MouseFilter = Control.MouseFilterEnum.Ignore; // Клики проходят сквозь клетки
                
                AddChild(cell);
                _gridCells[x, y] = cell;
            }
        }
    }
    
    private void CreatePreviewGhost()
    {
        _previewGhost = new ColorRect();
        _previewGhost.Color = new Color(1f, 1f, 1f, 0.5f); // Полупрозрачный белый
        _previewGhost.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_previewGhost);
    }
    
    private void CreateInstructionLabel()
    {
        _instructionLabel = new Label();
        _instructionLabel.Text = $"Размещение: {_furnitureToPlace.DisplayName} ({_furnitureToPlace.Size.X}x{_furnitureToPlace.Size.Y})\nКлик = поставить | Esc = отмена";
        _instructionLabel.Position = new Vector2(GridOffsetX, GridOffsetY - 50);
        _instructionLabel.AddThemeFontSizeOverride("font_size", 18);
        _instructionLabel.AddThemeColorOverride("font_color", Colors.White);
        AddChild(_instructionLabel);
    }
    
    // ===== ОБНОВЛЕНИЕ ПРЕДПРОСМОТРА =====
    
    private void UpdateHoveredCell()
    {
        var mousePos = GetViewport().GetMousePosition();
        
        // Конвертируем позицию мыши в координаты сетки
        int gridX = (int)((mousePos.X - GridOffsetX) / CellSize);
        int gridY = (int)((mousePos.Y - GridOffsetY) / CellSize);
        
        if (gridX >= 0 && gridX < _currentRoom.Width && gridY >= 0 && gridY < _currentRoom.Height)
        {
            _hoveredCell = new Vector2I(gridX, gridY);
        }
        else
        {
            _hoveredCell = new Vector2I(-1, -1);
        }
    }
    
    private void UpdatePreview()
    {
        if (_hoveredCell.X < 0 || _hoveredCell.Y < 0)
        {
            _previewGhost.Visible = false;
            return;
        }
        
        _previewGhost.Visible = true;
        _previewGhost.Position = new Vector2(
            GridOffsetX + _hoveredCell.X * CellSize,
            GridOffsetY + _hoveredCell.Y * CellSize
        );
        _previewGhost.Size = new Vector2(
            _furnitureToPlace.Size.X * CellSize - 2,
            _furnitureToPlace.Size.Y * CellSize - 2
        );
        
        // Проверяем, можно ли разместить мебель здесь
        bool canPlace = MuseumSystem.Instance.CanPlaceFurnitureAt(_currentRoom, _hoveredCell, _furnitureToPlace.Size);
        _previewGhost.Color = canPlace ? new Color(0.3f, 1f, 0.3f, 0.6f) : new Color(1f, 0.3f, 0.3f, 0.6f);
    }
    
    // ===== РАЗМЕЩЕНИЕ И ОТМЕНА =====
    
    private void TryPlaceFurniture()
{
    if (_hoveredCell.X < 0 || _hoveredCell.Y < 0) return;
    
    if (MuseumSystem.Instance.PlaceFurniture(_currentRoom, _furnitureToPlace, _hoveredCell))
    {
        GD.Print($"[PlacementMode] Placed {_furnitureToPlace.DisplayName} at ({_hoveredCell.X}, {_hoveredCell.Y})");
        
        // НОВОЕ: Находим сцену Музея и просим её обновить визуализацию
        var museum = GetTree().CurrentScene as Museum;
        museum?.RefreshRoomView();
        
        CancelPlacement();
    }
    else
    {
        GD.Print("[PlacementMode] Cannot place here!");
    }
}
    
    private void CancelPlacement()
    {
        Visible = false;
        MuseumSystem.Instance.CancelPlacementMode();
        GD.Print("[PlacementMode] Cancelled");
    }
    
    // ===== ОЧИСТКА =====
    
    private void ClearGrid()
    {
        if (_gridCells != null)
        {
            foreach (var cell in _gridCells)
            {
                cell?.QueueFree();
            }
            _gridCells = null;
        }
        
        _previewGhost?.QueueFree();
        _previewGhost = null;
        
        _instructionLabel?.QueueFree();
        _instructionLabel = null;
    }
}