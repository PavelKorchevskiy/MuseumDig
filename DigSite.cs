using Godot;
using System.Collections.Generic;

public partial class DigSite : Node2D
{
    [Export] public int TileSize = 64;
    private LootSummaryBar _lootSummaryBar;
    private DiggingUI _diggingUI;

    private DigSiteBackground _background;
        // === Камера ===
    private Camera2D _camera;
    private float _minCameraX, _maxCameraX;
    private float _minCameraY, _maxCameraY;

        // === Drag-перетаскивание камеры ===
    private bool _isDragging = false;
    private Vector2 _lastMousePos;
    
    // Константы камеры
    private const float CameraSpeed = 400f;           // Скорость движения клавишами (px/sec)
    private const int BackgroundPadding = 6;           // Должно совпадать с PaddingTiles в DigSiteBackground

    public override void _Ready()
    {
        var location = LocationSystem.Instance.GetActiveLocation();
        if (location == null) return;

        _background = new DigSiteBackground();
        AddChild(_background);
        _background.Setup(this, location);

        GenerateOrLoadGrid();

        // === НОВОЕ: Создаём камеру ===
        SetupCamera(location);

        // === Верхняя плашка: монеты + кнопка + найденные предметы ===
        var topUI = new DigSiteUI();
        AddChild(topUI);

        // Создаём панель находок и добавляем её внутрь DigSiteUI
        _lootSummaryBar = new LootSummaryBar();
        topUI.AddLootSummaryBar(_lootSummaryBar);

        // === Нижняя панель: инструменты ===
        var bottomLayer = new CanvasLayer();
        bottomLayer.Layer = 10;

        var bottomContainer = new Control();
        bottomContainer.AnchorTop = 1f;
        bottomContainer.AnchorBottom = 1f;
        bottomContainer.AnchorLeft = 0f;
        bottomContainer.AnchorRight = 1f;
        bottomContainer.OffsetTop = -90f;
        bottomContainer.OffsetBottom = -10f;

        _diggingUI = new DiggingUI();
        _diggingUI.AnchorLeft = 0.5f;
        _diggingUI.AnchorRight = 0.5f;
        _diggingUI.OffsetLeft = -250f;
        _diggingUI.OffsetRight = 250f;
        _diggingUI.OffsetTop = 0f;
        _diggingUI.OffsetBottom = 80f;

        bottomContainer.AddChild(_diggingUI);
        bottomLayer.AddChild(bottomContainer);
        AddChild(bottomLayer);

        var inputController = new DigInputController();
        AddChild(inputController);

        _lootSummaryBar.Refresh(this);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        
        // === Движение камеры клавиатурой ===
        Vector2 moveDirection = Vector2.Zero;
        
        // WASD + стрелки
        if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A)) moveDirection.X -= 1;
        if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D)) moveDirection.X += 1;
        if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W)) moveDirection.Y -= 1;
        if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S)) moveDirection.Y += 1;
        
        // Применяем движение клавиатуры
        if (moveDirection.Length() > 0.01f)
        {
            moveDirection = moveDirection.Normalized();
            _camera.Position += moveDirection * CameraSpeed * dt;
            
            // Ограничиваем позицию камеры
            _camera.Position = new Vector2(
                Mathf.Clamp(_camera.Position.X, _minCameraX, _maxCameraX),
                Mathf.Clamp(_camera.Position.Y, _minCameraY, _maxCameraY)
            );
        }
        
        // Обработка активных тайлов
        DiggingManager.Instance.ProcessDelta(dt);
    }

        public override void _Input(InputEvent @event)
    {
        // Правая кнопка мыши — drag камеры
        if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Right)
        {
            if (mouseButton.Pressed)
            {
                // Начало drag
                _isDragging = true;
                _lastMousePos = GetViewport().GetMousePosition();
                Input.SetDefaultCursorShape(Input.CursorShape.Drag); // Курсор "рука"
            }
            else
            {
                // Конец drag
                _isDragging = false;
                Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
            }
        }
        
        // Движение мыши во время drag
        if (@event is InputEventMouseMotion mouseMotion && _isDragging)
        {
            Vector2 currentMousePos = GetViewport().GetMousePosition();
            Vector2 delta = currentMousePos - _lastMousePos;
            
            // Инвертируем направление (drag вправо = камера влево)
            _camera.Position -= delta;
            
            // Ограничиваем позицию камеры
            _camera.Position = new Vector2(
                Mathf.Clamp(_camera.Position.X, _minCameraX, _maxCameraX),
                Mathf.Clamp(_camera.Position.Y, _minCameraY, _maxCameraY)
            );
            
            _lastMousePos = currentMousePos;
        }
    }

    private void GenerateOrLoadGrid()
    {
        var location = LocationSystem.Instance.GetActiveLocation();
        if (location == null)
        {
            GD.PrintErr("Current location not found!");
            return;
        }
        var savedData = DiggingManager.Instance.GetSaveDataForLocation(location.Id);
        bool hasSave = savedData != null && savedData.Count > 0;
        GD.Print($"[DigSite] Локация '{location.Id}': сохранение {(hasSave ? $"НАЙДЕНО ({savedData.Count} плиток)" : "НЕ НАЙДЕНО")} → {(hasSave ? "загрузка" : "генерация")}");

        int width = location.GridWidth;
        int height = location.GridHeight;
        float baseHp = location.BaseTileHp;
        float hpGrowth = location.TileHpGrowthPerRow;

        GD.Print($"[DigSite] Generating grid {width}x{height} for {location.DisplayName}");


            for (int y = 0; y < location.GridHeight; y++)
    {
        for (int x = 0; x < location.GridWidth; x++)
        {
            var tile = new Tile();
            AddChild(tile);
            
            string key = $"{x},{y}";
            bool hasSavedData = savedData != null && savedData.ContainsKey(key);
            
            if (hasSavedData)
            {
                // === ЗАГРУЗКА: лут НЕ роллим, берём из сохранения ===
                tile.Initialize(x, y, location.BaseTileHp, location.TileHpGrowthPerRow, location.Id, rollLoot: false);
                
                var data = savedData[key];
                tile.Stage = data.Stage;
                tile.Action = data.Action;
                tile.CurrentHP = data.CurrentHP;
                tile.MaxHP = data.MaxHP;
                tile.ExtractionProgress = data.ExtractionProgress;
                
                if (!string.IsNullOrEmpty(data.LootResourceId))
                {
                    tile._loot = new TileLoot
                    {
                        ResourceId = data.LootResourceId,
                        Amount = data.LootAmount,
                        RevealedAtStage = data.LootRevealedAtStage,
                        IsBone = data.LootIsBone,
                        Collected = data.LootCollected
                    };
                }
                // Если в сохранении лута нет — он и так null (rollLoot:false)
                
                tile.UpdateVisuals();
            }
            else
            {
                // === СВЕЖАЯ ГЕНЕРАЦИЯ: лут роллим ===
                tile.Initialize(x, y, location.BaseTileHp, location.TileHpGrowthPerRow, location.Id, rollLoot: true);
            }
        }
    }
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

    /// <summary>
    /// Собирает актуальные данные всех тайлов. Без побочных эффектов.
    /// </summary>
    public Dictionary<string, DiggingTileData> CollectTileData()
    {
        var data = new Dictionary<string, DiggingTileData>();

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

                    LootResourceId = tile._loot?.ResourceId ?? "",
                    LootAmount = tile._loot?.Amount ?? 0,
                    LootRevealedAtStage = tile._loot?.RevealedAtStage ?? TileStage.Intact,
                    LootIsBone = tile._loot?.IsBone ?? false,
                    LootCollected = tile._loot?.Collected ?? false
                };
            }
        }

        return data;
    }

    public void RefreshLootSummary()
    {
        _lootSummaryBar?.Refresh(this);
    }

    public override void _ExitTree()
    {
        // Данные будут синхронизированы автоматически через GetAllSaveData()
        // Но на всякий случай сохраняем явно при смене сцены
        var location = LocationSystem.Instance?.GetActiveLocation();
        if (location != null)
        {
            var data = CollectTileData();
            DiggingManager.Instance.SetSaveDataForLocation(location.Id, data);
            GD.Print($"[DigSite] 💾 Сохранено {data.Count} плиток при выходе из сцены: {location.Id}");
        }
    }

    private void SetupCamera(LocationDefinition location)
    {
        _camera = new Camera2D();
        _camera.Enabled = true;
        _camera.PositionSmoothingEnabled = true;
        _camera.PositionSmoothingSpeed = 8f; // Плавное движение
        AddChild(_camera);
        
        // Вычисляем границы движения камеры
        CalculateCameraBounds(location);
    }

        private void CalculateCameraBounds(LocationDefinition location)
    {
        int gridWidth = location.GridWidth;
        int gridHeight = location.GridHeight;
        
        const float TileWidth = 128f;
        const float TileHeight = 74f;
        const float VerticalOffset = (128f - TileHeight) / 2f;
        
        // Фон рисуется от -BackgroundPadding до gridWidth + BackgroundPadding - 1
        int minGX = -BackgroundPadding;
        int maxGX = gridWidth + BackgroundPadding - 1;
        int minGY = -BackgroundPadding;
        int maxGY = gridHeight + BackgroundPadding - 1;
        
        // Крайние точки фона в мировых координатах (формула из UpdatePosition)
        float bgMinX = (minGX - maxGY) * (TileWidth / 2f);    // Самая левая точка
        float bgMaxX = (maxGX - minGY) * (TileWidth / 2f);    // Самая правая точка
        float bgMinY = (minGX + minGY) * (TileHeight / 2f) - VerticalOffset;  // Самая верхняя
        float bgMaxY = (maxGX + maxGY) * (TileHeight / 2f) - VerticalOffset;  // Самая нижняя
        
        // Размер видимой области (viewport)
        var viewportSize = GetViewport().GetVisibleRect().Size;
        
        // Границы движения камеры:
        // Камера не может смещаться так, чтобы край экрана выходил за границы фона
        _minCameraX = bgMinX + viewportSize.X / 2f;
        _maxCameraX = bgMaxX - viewportSize.X / 2f;
        _minCameraY = bgMinY + viewportSize.Y / 2f;
        _maxCameraY = bgMaxY - viewportSize.Y / 2f;
        
        // Если фон меньше экрана — фиксируем камеру в центре
        if (_minCameraX > _maxCameraX)
        {
            _minCameraX = _maxCameraX = (bgMinX + bgMaxX) / 2f;
        }
        if (_minCameraY > _maxCameraY)
        {
            _minCameraY = _maxCameraY = (bgMinY + bgMaxY) / 2f;
        }
        
        GD.Print($"[DigSite] Границы камеры: X=[{_minCameraX:F0}, {_maxCameraX:F0}], Y=[{_minCameraY:F0}, {_maxCameraY:F0}]");
    }
}