using Godot;
using System.Collections.Generic;

public partial class DigSite : Node2D
{
    [Export] public int TileSize = 64;
    
    public override void _Ready()
    {
// Принудительно удаляем всех посетителей, если они остались с прошлой сцены
        foreach (var child in GetTree().Root.GetChildren())
        {
            if (child.Name.ToString().Contains("Visitor"))
            {
                child.QueueFree();
            }
        }
        
        GenerateOrLoadGrid();        
        // Добавляем панель инструментов
        var diggingUI = new DiggingUI();
        AddChild(diggingUI);
        var topUI = new DigSiteUI();
        AddChild(topUI);
        var inputController = new DigInputController();
        AddChild(inputController);
    }

    public override void _Process(double delta)
    {
        DiggingManager.Instance.ProcessDelta((float)delta);
    }

    private void GenerateOrLoadGrid()
    {
        var location = LocationSystem.Instance.GetActiveLocation();
        if (location == null)
        {
            GD.PrintErr("Current location not found!");
            return;
        }

        int width = location.GridWidth;
        int height = location.GridHeight;
        float baseHp = location.BaseTileHp;
        float hpGrowth = location.TileHpGrowthPerRow;

        GD.Print($"[DigSite] Generating grid {width}x{height} for {location.DisplayName}");

        var savedData = DiggingManager.Instance.GetSaveDataForLocation(location.Id);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var tile = new Tile();
                AddChild(tile);
                tile.Initialize(x, y, baseHp, hpGrowth, location.Id);
                
                string key = $"{x},{y}";
                if (savedData != null && savedData.ContainsKey(key))
                {
                    var data = savedData[key];
                    tile.Stage = data.Stage;
                    tile.Action = data.Action;
                    tile.CurrentHP = data.CurrentHP;
                    tile.MaxHP = data.MaxHP;
                    tile.ExtractionProgress = data.ExtractionProgress;
                    tile.HiddenResourceId = data.HiddenResourceId;
                    tile.HiddenAmount = data.HiddenAmount;
                    tile.IsFindingExtractable = data.IsFindingExtractable;
                    tile.UpdateVisuals();
                }

                DiggingManager.Instance.RegisterTile(tile);
            }
        }

        // === ЦЕНТРИРОВАНИЕ ИЗОМЕТРИЧЕСКОЙ СЕТКИ ===
        float isoGridWidth = (width + height) * 64f;  // 128 / 2
        float isoGridHeight = (width + height) * 32f; // 64 / 2 (или 37f для 30°)
        
        var viewportSize = GetViewport().GetVisibleRect().Size;
        Position = new Vector2(
            (viewportSize.X - isoGridWidth) / 2f + 64f,
            (viewportSize.Y - isoGridHeight) / 2f + 64f
        );
    }

        /// <summary>
    /// Возвращает тайл под указанной мировой позицией (с учётом формы ромба).
    /// </summary>
    public Tile GetTileAtPosition(Vector2 worldPos)
    {
        // Конвертируем мировую позицию в локальную позицию сетки
        Vector2 localPos = worldPos - Position;

        // Перебираем все тайлы и проверяем попадание
        foreach (var child in GetChildren())
        {
            if (child is Tile tile)
            {
                if (tile.IsPointInsideShape(localPos))
                {
                    return tile;
                }
            }
        }
        return null;
    }

    public override void _ExitTree()
    {
        var location = LocationSystem.Instance.GetActiveLocation();
        if (location != null)
        {

            var existingData = DiggingManager.Instance.GetSaveDataForLocation(location.Id);
            if (existingData == null || existingData.Count == 0)
            {
                GD.Print($"[DigSite] ⏭️ Пропускаем сохранение (локация была сброшена): {location.Id}");
                return;
            }
            var data = new Dictionary<string, DiggingTileData>();
            
            // Собираем данные со всех активных плиток
            foreach (var child in GetChildren())
            {
                if (child is Tile tile)
                {
                    string key = $"{tile.GridPosition.X},{tile.GridPosition.Y}";
                    data[key] = new DiggingTileData
                    {
                        X = tile.GridPosition.X,
                        Y = tile.GridPosition.Y,
                        Stage = tile.Stage,
                        Action = tile.Action,
                        CurrentHP = tile.CurrentHP,
                        MaxHP = tile.MaxHP,
                        ExtractionProgress = tile.ExtractionProgress,
                        HiddenResourceId = tile.HiddenResourceId,
                        HiddenAmount = tile.HiddenAmount,
                        IsFindingExtractable = tile.IsFindingExtractable
                    };
                }
            }
            
            DiggingManager.Instance.SetSaveDataForLocation(location.Id, data);
            SaveSystem.Instance?.MarkDirty();
            GD.Print($"[DigSite] 💾 Сохранен прогресс локации: {location.Id} ({data.Count} плиток)");
        }
    }
}