using Godot;
using System.Collections.Generic;

public partial class PlacementModeUI : CanvasLayer
{
    private ColorRect[,] _gridCells;
    private Label _instructionLabel;
    
    private Room _currentRoom;
    private Furniture _furnitureToPlace;
    private Vector2I _hoveredCell = new(-1, -1);
    
    // ИЗОМЕТРИЧЕСКИЕ КОНСТАНТЫ (должны совпадать с RoomViewUI)
    private const int GridOffsetX = 500;
    private const int GridOffsetY = 100;
    
    public override void _Ready()
    {
        Layer = 50;
        Visible = false;
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
        _gridCells = new ColorRect[_currentRoom.Width, _currentRoom.Height];
        
        for (int x = 0; x < _currentRoom.Width; x++)
        {
            for (int y = 0; y < _currentRoom.Height; y++)
            {
                var cell = new ColorRect();
                var isoPos = IsoUtils.GridToIso(x, y);
                
                cell.Position = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y);
                cell.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
                cell.Color = new Color(1f, 1f, 1f, 0.1f);
                cell.MouseFilter = Control.MouseFilterEnum.Ignore;
                cell.ZIndex = IsoUtils.GetZOrder(x, y);
                
                AddChild(cell);
                _gridCells[x, y] = cell;
            }
        }
    }
    
    private void CreateInstructionLabel()
    {
        _instructionLabel = new Label();
        _instructionLabel.Text = $"Размещение: {_furnitureToPlace.DisplayName} ({_furnitureToPlace.Size.X}x{_furnitureToPlace.Size.Y})\nКлик = поставить | Esc = отмена";
        _instructionLabel.Position = new Vector2(20, 20);
        _instructionLabel.AddThemeFontSizeOverride("font_size", 18);
        _instructionLabel.AddThemeColorOverride("font_color", Colors.White);
        AddChild(_instructionLabel);
    }
    
    // ===== ОБНОВЛЕНИЕ ПРЕДПРОСМОТРА =====
    
    private void UpdateHoveredCell()
    {
        var mousePos = GetViewport().GetMousePosition();
        
        float relativeX = mousePos.X - GridOffsetX;
        float relativeY = mousePos.Y - GridOffsetY;
        
        _hoveredCell = IsoUtils.IsoToGrid(relativeX, relativeY);
        
        if (_hoveredCell.X < 0 || _hoveredCell.X >= _currentRoom.Width ||
            _hoveredCell.Y < 0 || _hoveredCell.Y >= _currentRoom.Height)
        {
            _hoveredCell = new Vector2I(-1, -1);
        }
    }
    
    private void UpdatePreview()
    {
        if (_hoveredCell.X < 0 || _hoveredCell.Y < 0 )
        {
            UpdateGridColors(false);
            return;
        }       
        bool canPlace = MuseumSystem.Instance.CanPlaceFurnitureAt(_currentRoom, _hoveredCell, _furnitureToPlace.Size);
        UpdateGridColors(canPlace);
    }
    
    private void UpdateGridColors(bool isValid)
    {
        Color highlightColor = isValid ? new Color(0.3f, 1f, 0.3f, 0.4f) : new Color(1f, 0.3f, 0.3f, 0.4f);
        Color defaultColor = new Color(1f, 1f, 1f, 0.1f);

        for (int x = 0; x < _currentRoom.Width; x++)
        {
            for (int y = 0; y < _currentRoom.Height; y++)
            {
                bool isUnderFurniture = x >= _hoveredCell.X && x < _hoveredCell.X + _furnitureToPlace.Size.X &&
                                        y >= _hoveredCell.Y && y < _hoveredCell.Y + _furnitureToPlace.Size.Y;
                
                if (_gridCells[x, y] != null)
                {
                    _gridCells[x, y].Color = isUnderFurniture ? highlightColor : defaultColor;
                }
            }
        }
    }
    
    // ===== РАЗМЕЩЕНИЕ И ОТМЕНА =====
    
    private void TryPlaceFurniture()
    {
        if (_hoveredCell.X < 0 || _hoveredCell.Y < 0) return;
        
        if (MuseumSystem.Instance.PlaceFurniture(_currentRoom, _furnitureToPlace, _hoveredCell))
        {
            GD.Print($"[PlacementMode] Placed {_furnitureToPlace.DisplayName} at ({_hoveredCell.X}, {_hoveredCell.Y})");
            
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
    
    // ===== УТИЛИТЫ =====
    
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
    
    private void ClearGrid()
    {
        if (_gridCells != null)
        {
            for (int x = 0; x < _currentRoom?.Width; x++)
            {
                for (int y = 0; y < _currentRoom?.Height; y++)
                {
                    _gridCells[x, y]?.QueueFree();
                }
            }
            _gridCells = null;
        }
        
        _instructionLabel?.QueueFree();
        _instructionLabel = null;
    }
}