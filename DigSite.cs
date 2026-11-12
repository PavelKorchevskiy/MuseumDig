using Godot;
using System.Collections.Generic;

public partial class DigSite : Node2D
{
    [Export] public int TileSize = 64;
    private LootSummaryBar _lootSummaryBar;
    private DigSiteUI _topUI;
    private DiggingUI _diggingUI;

    private DigSiteBackground _background;

    // ===================== Камера: жест под тач-экран =====================
    // Та же логика, что в музейной CameraController: один палец — панорама,
    // два пальца — pinch-zoom, короткий тап — для DigInputController (через
    // TapAllowed), эмуляция мыши (project.godot: emulate_touch_from_mouse)
    // даёт то же самое на ПК левой кнопкой. Код не общий с CameraController
    // музея, т.к. у раскопок своя (не изометрическая по комнатам) камера.

    private static DigSite _current;

    /// <summary>true, если последний жест был тапом, а не свайпом/пинчем.</summary>
    public static bool TapAllowed => _current == null || _current._gestureWasTap;

    private Camera2D _camera;

    [Export] public Vector2 MinZoom = new Vector2(0.6f, 0.6f);
    [Export] public Vector2 MaxZoom = new Vector2(1.8f, 1.8f);
    [Export] public float WheelZoomStep = 0.12f;
    [Export] public float TapThresholdPx = 18f;
    [Export] public float InertiaDamping = 4.0f;
    [Export] public float InertiaMinSpeed = 30f;

    // Границы прокрутки в мировых координатах (без учёта зума — зум учитывается при клампе)
    private Rect2 _bounds;
    private bool _boundsReady = false;

    // Пальцы на экране: индекс -> текущая позиция
    private readonly Dictionary<int, Vector2> _touches = new Dictionary<int, Vector2>();
    private bool _touchInputSeen = false;

    private bool _pointerDown = false;
    private bool _panActive = false;
    private bool _gestureWasTap = true;
    private float _travelPx = 0f;
    private Vector2 _pressPos;

    private bool _pinchActive = false;
    private float _pinchStartDistance = 1f;
    private float _pinchStartZoom = 1f;

    private Vector2 _velocity = Vector2.Zero;
    private ulong _lastDragMs = 0;

    // Константы камеры
    private const float CameraSpeed = 400f;           // Скорость движения клавишами (px/sec)
    private const int BackgroundPadding = 6;           // Должно совпадать с PaddingTiles в DigSiteBackground

    public override void _Ready()
    {
        var location = LocationSystem.Instance.GetActiveLocation();
        if (location == null) return;

        _current = this;

        QuestSystem.Instance.OnLocationVisited(location.Id);

        _background = new DigSiteBackground();
        AddChild(_background);
        _background.Setup(this, location);

        GenerateOrLoadGrid();

        // === НОВОЕ: Создаём камеру ===
        SetupCamera(location);

        // === Верхняя плашка: монеты + кнопка + найденные предметы ===
        var topUI = new DigSiteUI();
        _topUI = topUI;
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
        bottomContainer.OffsetTop = -114f; // кнопки инструментов стали крупнее под палец
        bottomContainer.OffsetBottom = -10f;

        _diggingUI = new DiggingUI();
        _diggingUI.AnchorLeft = 0.5f;
        _diggingUI.AnchorRight = 0.5f;
        _diggingUI.OffsetLeft = -250f;
        _diggingUI.OffsetRight = 250f;
        _diggingUI.OffsetTop = 0f;
        _diggingUI.OffsetBottom = 104f;

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

        // === Движение камеры клавиатурой (удобство на ПК, на мобиле не используется) ===
        Vector2 moveDirection = Vector2.Zero;

        // WASD + стрелки
        if (Input.IsActionPressed("ui_left") || Input.IsKeyPressed(Key.A)) moveDirection.X -= 1;
        if (Input.IsActionPressed("ui_right") || Input.IsKeyPressed(Key.D)) moveDirection.X += 1;
        if (Input.IsActionPressed("ui_up") || Input.IsKeyPressed(Key.W)) moveDirection.Y -= 1;
        if (Input.IsActionPressed("ui_down") || Input.IsKeyPressed(Key.S)) moveDirection.Y += 1;

        if (moveDirection.Length() > 0.01f)
        {
            moveDirection = moveDirection.Normalized();
            _velocity = Vector2.Zero;
            _camera.Position = ClampPosition(_camera.Position + moveDirection * CameraSpeed * dt);
        }
        else if (!_pointerDown && !_pinchActive && _velocity.LengthSquared() >= 1f)
        {
            // === Инерция после свайпа пальцем ===
            Vector2 before = _camera.Position;
            _camera.Position = ClampPosition(_camera.Position + _velocity * dt);
            _velocity *= Mathf.Exp(-InertiaDamping * dt);

            if (_velocity.Length() < InertiaMinSpeed || _camera.Position.IsEqualApprox(before))
            {
                _velocity = Vector2.Zero;
            }
        }

        // Обработка активных тайлов
        DiggingManager.Instance.ProcessDelta(dt);
    }

    // ===================== Жест камеры: пан одним пальцем + pinch-zoom =====================
    // Та же схема, что в музейной CameraController._Input — см. комментарий у полей выше.
    public override void _Input(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventScreenTouch touch:
                _touchInputSeen = true;
                HandleTouch(touch);
                return;

            case InputEventScreenDrag drag:
                _touchInputSeen = true;
                HandleDrag(drag.Index, drag.Position, drag.Relative);
                return;
        }

        if (@event is InputEventMouseButton wheel && wheel.Pressed)
        {
            if (wheel.ButtonIndex == MouseButton.WheelUp)
            {
                ApplyZoom(_camera.Zoom.X + WheelZoomStep, wheel.Position);
                return;
            }
            if (wheel.ButtonIndex == MouseButton.WheelDown)
            {
                ApplyZoom(_camera.Zoom.X - WheelZoomStep, wheel.Position);
                return;
            }
        }

        // Если устройство даёт touch-события, мышь дублирует их — игнорируем.
        if (_touchInputSeen) return;

        switch (@event)
        {
            case InputEventMouseButton mb when mb.ButtonIndex == MouseButton.Left:
                if (mb.Pressed) BeginPointer(mb.Position);
                else EndPointer();
                return;

            case InputEventMouseMotion motion when _pointerDown:
                HandleDrag(0, motion.Position, motion.Relative);
                return;
        }
    }

    private void HandleTouch(InputEventScreenTouch touch)
    {
        if (touch.Pressed)
        {
            _touches[touch.Index] = touch.Position;

            if (_touches.Count == 1)
            {
                BeginPointer(touch.Position);
                return;
            }

            _gestureWasTap = false; // второй палец — уже не тап
            if (_touches.Count == 2 && _panActive) BeginPinch();
            return;
        }

        _touches.Remove(touch.Index);

        if (_pinchActive && _touches.Count < 2)
        {
            _pinchActive = false;
            _velocity = Vector2.Zero;
            _lastDragMs = Time.GetTicksMsec();
        }

        if (_touches.Count == 0) EndPointer();
    }

    private void BeginPointer(Vector2 position)
    {
        _pointerDown = true;
        _panActive = true;
        _pressPos = position;
        _travelPx = 0f;
        _gestureWasTap = true;
        _velocity = Vector2.Zero;
        _lastDragMs = Time.GetTicksMsec();
    }

    private void HandleDrag(int index, Vector2 position, Vector2 relative)
    {
        if (_touches.Count > 0) _touches[index] = position;

        _travelPx += relative.Length();
        if (_travelPx > TapThresholdPx) _gestureWasTap = false;

        if (_pinchActive)
        {
            UpdatePinch();
            return;
        }

        if (!_panActive) return;
        if (index != LowestTouchIndex()) return; // тащим только ведущим пальцем

        float zoom = Mathf.Max(_camera.Zoom.X, 0.01f);
        Vector2 worldDelta = -relative / zoom;
        _camera.Position = ClampPosition(_camera.Position + worldDelta);

        ulong now = Time.GetTicksMsec();
        float dt = Mathf.Max((now - _lastDragMs) / 1000f, 0.004f);
        _lastDragMs = now;
        _velocity = _velocity.Lerp(worldDelta / dt, 0.5f);
    }

    private void EndPointer()
    {
        bool wasTap = _gestureWasTap && _travelPx <= TapThresholdPx;

        _pointerDown = false;
        _pinchActive = false;
        _panActive = false;
        _touches.Clear();

        if (wasTap || _velocity.Length() < InertiaMinSpeed)
        {
            _velocity = Vector2.Zero;
        }
        // иначе камера доедет по инерции (см. _Process)
    }

    private int LowestTouchIndex()
    {
        if (_touches.Count == 0) return 0;
        int min = int.MaxValue;
        foreach (int key in _touches.Keys)
            if (key < min) min = key;
        return min;
    }

    private void BeginPinch()
    {
        if (!TryGetTwoTouches(out Vector2 a, out Vector2 b)) return;

        _pinchActive = true;
        _gestureWasTap = false;
        _velocity = Vector2.Zero;
        _pinchStartDistance = Mathf.Max(a.DistanceTo(b), 1f);
        _pinchStartZoom = _camera.Zoom.X;
    }

    private void UpdatePinch()
    {
        if (!TryGetTwoTouches(out Vector2 a, out Vector2 b)) return;

        float distance = Mathf.Max(a.DistanceTo(b), 1f);
        float target = _pinchStartZoom * (distance / _pinchStartDistance);
        ApplyZoom(target, (a + b) / 2f);
    }

    private bool TryGetTwoTouches(out Vector2 a, out Vector2 b)
    {
        a = Vector2.Zero;
        b = Vector2.Zero;

        int found = 0;
        foreach (var position in _touches.Values)
        {
            if (found == 0) a = position;
            else b = position;

            found++;
            if (found == 2) return true;
        }
        return false;
    }

    private void ApplyZoom(float targetZoom, Vector2? screenFocus)
    {
        float clamped = Mathf.Clamp(targetZoom, MinZoom.X, MaxZoom.X);
        float current = Mathf.Max(_camera.Zoom.X, 0.01f);
        if (Mathf.IsEqualApprox(clamped, current)) return;

        if (screenFocus.HasValue)
        {
            // Держим мировую точку под пальцами на месте
            Vector2 viewCenter = GetViewport().GetVisibleRect().Size / 2f;
            Vector2 offset = screenFocus.Value - viewCenter;
            Vector2 worldUnderFocus = _camera.Position + offset / current;

            _camera.Zoom = new Vector2(clamped, clamped);
            _camera.Position = ClampPosition(worldUnderFocus - offset / clamped);
        }
        else
        {
            _camera.Zoom = new Vector2(clamped, clamped);
            _camera.Position = ClampPosition(_camera.Position);
        }
    }

    private Vector2 ClampPosition(Vector2 position)
    {
        if (!_boundsReady || DevMode.Instance.FreeCamera) return position;

        Vector2 half = GetViewport().GetVisibleRect().Size / (2f * Mathf.Max(_camera.Zoom.X, 0.01f));
        Vector2 min = _bounds.Position + half;
        Vector2 max = _bounds.End - half;
        Vector2 center = _bounds.GetCenter();

        float x = (min.X <= max.X) ? Mathf.Clamp(position.X, min.X, max.X) : center.X;
        float y = (min.Y <= max.Y) ? Mathf.Clamp(position.Y, min.Y, max.Y) : center.Y;

        return new Vector2(x, y);
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

        // === Догонка прогресса за время, пока игрок был в музее ===
        // Все тайлы локации уже созданы и зарегистрированы в DiggingManager
        // (Tile.Initialize -> RegisterTile) — можно безопасно применить урон/
        // прогресс накопленный с момента ухода со сцены раскопок.
        DiggingManager.Instance.ApplyCatchUpForLocation(location.Id);
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

    /// <summary>
    /// Собранная находка летит с плитки в верхнюю плашку; счётчик в плашке
    /// обновляется в момент "приземления".
    /// </summary>
    /// <param name="screenFrom">Центр находки в координатах экрана.</param>
    /// <param name="screenSize">Её размер на экране в пикселях.</param>
    public void PlayLootFlyAnimation(Texture2D texture, Vector2 screenFrom, float screenSize, string resourceId)
    {
        if (_topUI == null || _lootSummaryBar == null || texture == null)
        {
            RefreshLootSummary();
            return;
        }

        Vector2 target = _lootSummaryBar.GetSlotScreenCenter(resourceId);
        _topUI.FlyToLootBar(texture, screenFrom, screenSize, target, () =>
        {
            RefreshLootSummary();
            _lootSummaryBar.Pulse(resourceId);
        });
    }

    public override void _ExitTree()
    {
        if (_current == this) _current = null;

        // Данные будут синхронизированы автоматически через GetAllSaveData()
        // Но на всякий случай сохраняем явно при смене сцены
        var location = LocationSystem.Instance?.GetActiveLocation();
        if (location != null)
        {
            var data = CollectTileData();
            DiggingManager.Instance.SetSaveDataForLocation(location.Id, data);
            DiggingManager.Instance.MarkLocationExited(location.Id);
            GD.Print($"[DigSite] 💾 Сохранено {data.Count} плиток при выходе из сцены: {location.Id}");
        }
    }

    private void SetupCamera(LocationDefinition location)
    {
        _camera = new Camera2D();
        _camera.Enabled = true;
        _camera.Zoom = Vector2.One;
        _camera.PositionSmoothingEnabled = true;
        _camera.PositionSmoothingSpeed = 8f; // Плавное движение

        // Границы считаем сами (с учётом зума в ClampPosition), встроенные
        // лимиты Camera2D отключаем, чтобы не спорили с нашим клампом.
        _camera.LimitLeft = -100000;
        _camera.LimitRight = 100000;
        _camera.LimitTop = -100000;
        _camera.LimitBottom = 100000;

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

        // Сырые границы мира — половину viewport'а (зависящую от текущего
        // зума) вычитаем позже, в ClampPosition, а не здесь, иначе зум
        // перестал бы работать корректно у краёв.
        _bounds = new Rect2(bgMinX, bgMinY, bgMaxX - bgMinX, bgMaxY - bgMinY);
        _boundsReady = true;

        GD.Print($"[DigSite] Границы фона: X=[{bgMinX:F0}, {bgMaxX:F0}], Y=[{bgMinY:F0}, {bgMaxY:F0}]");
    }
}