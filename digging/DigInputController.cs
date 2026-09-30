using Godot;

public partial class DigInputController : Node2D
{
    private Tile _hoveredTile = null; // Текущий подсвеченный тайл
    private DigSite _digSite;

    public override void _Ready()
    {
        _digSite = GetParent() as DigSite;
    }

    public override void _Process(double delta)
    {
        // Если инструмент не выбран — сбрасываем подсветку
        if (DiggingUI.Instance?.SelectedTool == null)
        {
            ClearHighlight();
            return;
        }

        // Определяем тайл под курсором
        var mousePos = GetGlobalMousePosition();
        Tile tileUnderMouse = _digSite?.GetTileAtPosition(mousePos);

        // Если тайл изменился — обновляем подсветку
        if (tileUnderMouse != _hoveredTile)
        {
            ClearHighlight();
            _hoveredTile = tileUnderMouse;
            
            if (_hoveredTile != null)
            {
                _hoveredTile.Highlight();
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent 
            && mouseEvent.Pressed 
            && mouseEvent.ButtonIndex == MouseButton.Left)
        {
            var mousePos = GetGlobalMousePosition();
            Tile clickedTile = _digSite?.GetTileAtPosition(mousePos);

            if (clickedTile != null)
            {
                if (clickedTile.IsFindingExtractable 
                    && (clickedTile.Stage == TileStage.HasFinding_Dug1 || clickedTile.Stage == TileStage.HasFinding_Dug2))
                {
                    clickedTile.CollectFinding();
                    GetViewport().SetInputAsHandled();
                    return;
                }

                // Если выбран инструмент, применяем его
                if (DiggingUI.Instance?.SelectedTool != null)
                {
                    clickedTile.ApplySelectedTool();
                    GetViewport().SetInputAsHandled();
                }
            }
        }
    }

    private void ClearHighlight()
    {
        if (_hoveredTile != null)
        {
            _hoveredTile.Unhighlight();
            _hoveredTile = null;
        }
    }
}