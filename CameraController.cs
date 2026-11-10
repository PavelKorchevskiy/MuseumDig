using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Камера сцены музея с управлением под тач-экран:
///   • свайп одним пальцем — панорама
///   • два пальца — зум (pinch), с привязкой к точке между пальцами
///   • короткий тап — выбор комнаты / открытие витрины
/// На ПК работает так же: в project.godot включён emulate_touch_from_mouse,
/// поэтому левая кнопка мыши = палец. Колесо мыши дополнительно даёт зум.
/// </summary>
public partial class CameraController : Camera2D
{
    // ===================== Доступ из других скриптов =====================

    public static CameraController Current { get; private set; }

    /// <summary>
    /// true, если последний жест был тапом, а не свайпом.
    /// Обработчики кликов (витрины, скелеты) проверяют это, чтобы не
    /// срабатывать, когда игрок просто тащил камеру по экрану.
    /// </summary>
    public static bool TapAllowed => Current == null || Current._gestureWasTap;

    // ===================== Настройки =====================

    [Export] public float MoveDuration = 0.8f;

    [Export] public Vector2 MinZoom = new Vector2(0.5f, 0.5f);
    [Export] public Vector2 MaxZoom = new Vector2(2.0f, 2.0f);

    /// <summary>Шаг зума колесом мыши (только для отладки на ПК).</summary>
    [Export] public float WheelZoomStep = 0.12f;

    /// <summary>
    /// Сколько клеток вокруг музея разрешено показывать камерой.
    /// Если по краям экрана появляется пустота — уменьшите значение
    /// (или увеличьте BorderThickness в MuseumBackground).
    /// </summary>
    [Export] public int BoundsMarginCells = 8;

    /// <summary>Смещение пальца, после которого жест считается свайпом, а не тапом.</summary>
    [Export] public float TapThresholdPx = 18f;

    /// <summary>Инерция после свайпа: больше значение — быстрее остановка.</summary>
    [Export] public float InertiaDamping = 4.0f;

    /// <summary>Ниже этой скорости инерция гасится полностью.</summary>
    [Export] public float InertiaMinSpeed = 30f;

    private float GridOffsetX => MuseumConstants.GridOffsetX;
    private float GridOffsetY => MuseumConstants.GridOffsetY;

    // ===================== Состояние =====================

    private Tween _currentTween;

    // Пальцы на экране: индекс -> текущая позиция
    private readonly Dictionary<int, Vector2> _touches = new Dictionary<int, Vector2>();
    private bool _touchInputSeen = false;   // устройство присылает touch-события

    private bool _pointerDown = false;
    private bool _panActive = false;        // жест начался на мире, а не на интерфейсе
    private bool _gestureWasTap = true;
    private float _travelPx = 0f;
    private Vector2 _pressPos;

    // Пинч
    private bool _pinchActive = false;
    private float _pinchStartDistance = 1f;
    private float _pinchStartZoom = 1f;

    // Инерция
    private Vector2 _velocity = Vector2.Zero;
    private ulong _lastDragMs = 0;

    // Границы прокрутки в мировых координатах
    private Rect2 _bounds;
    private bool _boundsReady = false;

    private GlobalRoomViewUI _roomView;

    // ===================== Жизненный цикл =====================

    public override void _Ready()
    {
        Current = this;
        Zoom = new Vector2(1, 1);

        // Границы считаем сами (с учётом зума), встроенные лимиты отключаем,
        // иначе они начинают спорить с нашим ограничением.
        LimitLeft = -100000;
        LimitRight = 100000;
        LimitTop = -100000;
        LimitBottom = 100000;

        RecalculateBounds();

        if (DevMode.Instance.FreeCamera)
            GD.Print("[Camera] 🎮 DevMode: границы камеры ОТКЛЮЧЕНЫ");
    }

    public override void _ExitTree()
    {
        if (Current == this) Current = null;
    }

    /// <summary>
    /// Пересчитывает границы прокрутки по текущей планировке музея.
    /// Вызовите, если открыли новую комнату.
    /// </summary>
    public void RecalculateBounds()
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            minX = Math.Min(minX, room.GlobalOffset.X);
            maxX = Math.Max(maxX, room.GlobalOffset.X + room.Width - 1);
            minY = Math.Min(minY, room.GlobalOffset.Y);
            maxY = Math.Max(maxY, room.GlobalOffset.Y + room.Height - 1);
        }

        if (minX == int.MaxValue)
        {
            _boundsReady = false;
            GD.PrintErr("[Camera] Нет комнат, границы камеры не заданы");
            return;
        }

        minX -= BoundsMarginCells;
        minY -= BoundsMarginCells;
        maxX += BoundsMarginCells;
        maxY += BoundsMarginCells;

        // Крайние точки изометрического ромба сетки
        float left = GridOffsetX + IsoUtils.GridToIso(minX, maxY).X;
        float right = GridOffsetX + IsoUtils.GridToIso(maxX, minY).X + IsoUtils.TileWidth;
        float top = GridOffsetY + IsoUtils.GridToIso(minX, minY).Y;
        float bottom = GridOffsetY + IsoUtils.GridToIso(maxX, maxY).Y + IsoUtils.TileHeight;

        _bounds = new Rect2(left, top, right - left, bottom - top);
        _boundsReady = true;
    }

    // ===================== Ввод =====================

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
            // Колесо мыши — удобство для отладки на ПК
            if (wheel.ButtonIndex == MouseButton.WheelUp && !IsPointOverBlockingUi(wheel.Position))
            {
                ApplyZoom(Zoom.X + WheelZoomStep, wheel.Position);
                return;
            }
            if (wheel.ButtonIndex == MouseButton.WheelDown && !IsPointOverBlockingUi(wheel.Position))
            {
                ApplyZoom(Zoom.X - WheelZoomStep, wheel.Position);
                return;
            }
        }

        // Если устройство даёт touch-события, мышь дублирует их — игнорируем.
        if (_touchInputSeen) return;

        switch (@event)
        {
            case InputEventMouseButton mb
                when mb.ButtonIndex == MouseButton.Left || mb.ButtonIndex == MouseButton.Right:
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

            // Второй палец — это уже не тап
            _gestureWasTap = false;
            if (_touches.Count == 2 && _panActive) BeginPinch();
            return;
        }

        _touches.Remove(touch.Index);

        if (_pinchActive && _touches.Count < 2)
        {
            _pinchActive = false;
            _velocity = Vector2.Zero;
            // Один палец остался — продолжаем панораму с него
            _lastDragMs = Time.GetTicksMsec();
        }

        if (_touches.Count == 0) EndPointer();
    }

    private void BeginPointer(Vector2 position)
    {
        _pointerDown = true;
        _pressPos = position;
        _travelPx = 0f;
        _gestureWasTap = true;
        _velocity = Vector2.Zero;
        _lastDragMs = Time.GetTicksMsec();

        // Жест принимаем только если палец опустился на мир.
        // Панели интерфейса (инвентарь, витрина, панели сверху и снизу)
        // должны прокручиваться и нажиматься сами, без движения камеры.
        _panActive = !IsPointOverBlockingUi(position);
        if (_panActive) StopTween();
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
        if (index != LowestTouchIndex()) return;   // тащим только ведущим пальцем

        StopTween();

        float zoom = Mathf.Max(Zoom.X, 0.01f);
        Vector2 worldDelta = -relative / zoom;
        GlobalPosition = ClampPosition(GlobalPosition + worldDelta);

        ulong now = Time.GetTicksMsec();
        float dt = Mathf.Max((now - _lastDragMs) / 1000f, 0.004f);
        _lastDragMs = now;
        _velocity = _velocity.Lerp(worldDelta / dt, 0.5f);
    }

    private void EndPointer()
    {
        bool wasPan = _panActive;
        bool wasTap = _gestureWasTap && _travelPx <= TapThresholdPx;

        _pointerDown = false;
        _pinchActive = false;
        _panActive = false;
        _touches.Clear();

        if (!wasPan)
        {
            _velocity = Vector2.Zero;
            return;
        }

        if (wasTap)
        {
            _velocity = Vector2.Zero;
            HandleWorldTap(_pressPos);
        }
        else if (_velocity.Length() < InertiaMinSpeed)
        {
            _velocity = Vector2.Zero;
            UpdateActiveRoom();
        }
        // иначе камера доедет по инерции и сама обновит активную комнату
    }

    private int LowestTouchIndex()
    {
        if (_touches.Count == 0) return 0;

        int min = int.MaxValue;
        foreach (int key in _touches.Keys)
            if (key < min) min = key;
        return min;
    }

    // ===================== Пинч =====================

    private void BeginPinch()
    {
        if (!TryGetTwoTouches(out Vector2 a, out Vector2 b)) return;

        _pinchActive = true;
        _gestureWasTap = false;
        _velocity = Vector2.Zero;
        _pinchStartDistance = Mathf.Max(a.DistanceTo(b), 1f);
        _pinchStartZoom = Zoom.X;
        StopTween();
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

    // ===================== Зум и границы =====================

    private void ApplyZoom(float targetZoom, Vector2? screenFocus)
    {
        float clamped = Mathf.Clamp(targetZoom, MinZoom.X, MaxZoom.X);
        float current = Mathf.Max(Zoom.X, 0.01f);
        if (Mathf.IsEqualApprox(clamped, current)) return;

        StopTween();

        if (screenFocus.HasValue)
        {
            // Держим мировую точку под пальцами на месте
            Vector2 viewCenter = GetViewport().GetVisibleRect().Size / 2f;
            Vector2 offset = screenFocus.Value - viewCenter;
            Vector2 worldUnderFocus = GlobalPosition + offset / current;

            Zoom = new Vector2(clamped, clamped);
            GlobalPosition = ClampPosition(worldUnderFocus - offset / clamped);
        }
        else
        {
            Zoom = new Vector2(clamped, clamped);
            GlobalPosition = ClampPosition(GlobalPosition);
        }
    }

    private Vector2 ClampPosition(Vector2 position)
    {
        return ClampPositionAt(position, Zoom.X);
    }

    private Vector2 ClampPositionAt(Vector2 position, float zoom)
    {
        if (!_boundsReady || DevMode.Instance.FreeCamera) return position;

        Vector2 half = GetViewport().GetVisibleRect().Size / (2f * Mathf.Max(zoom, 0.01f));
        Vector2 min = _bounds.Position + half;
        Vector2 max = _bounds.End - half;
        Vector2 center = _bounds.GetCenter();

        // Если мир по этой оси уже экрана — просто центрируем
        float x = (min.X <= max.X) ? Mathf.Clamp(position.X, min.X, max.X) : center.X;
        float y = (min.Y <= max.Y) ? Mathf.Clamp(position.Y, min.Y, max.Y) : center.Y;

        return new Vector2(x, y);
    }

    // ===================== Инерция =====================

    public override void _Process(double delta)
    {
        if (_pointerDown || _pinchActive) return;
        if (_velocity.LengthSquared() < 1f) return;

        float d = (float)delta;
        Vector2 before = GlobalPosition;

        GlobalPosition = ClampPosition(GlobalPosition + _velocity * d);
        _velocity *= Mathf.Exp(-InertiaDamping * d);

        // Остановились сами или упёрлись в границу
        if (_velocity.Length() < InertiaMinSpeed || GlobalPosition.IsEqualApprox(before))
        {
            _velocity = Vector2.Zero;
            UpdateActiveRoom();
        }
    }

    // ===================== Тап по миру =====================

    private void HandleWorldTap(Vector2 screenPos)
    {
        Vector2 world = GetCanvasTransform().AffineInverse() * screenPos;

        // Если активен режим размещения мебели/скелета - тап обрабатывает он,
        // а не обычный клик по полу/витрине.
        var placement = GetPlacementUi();
        if (placement != null)
        {
            placement.OnWorldTap(world);
            return;
        }

        GetRoomView()?.OnWorldTap(world);
    }

    private PlacementModeUI GetPlacementUi()
    {
        var scene = GetTree()?.CurrentScene;
        if (scene == null) return null;

        var ui = scene.GetNodeOrNull<PlacementModeUI>("PlacementModeUI");
        return (ui != null && ui.Visible) ? ui : null;
    }

    private void UpdateActiveRoom()
    {
        GetRoomView()?.UpdateActiveRoomByCameraCenter(GlobalPosition);
    }

    private GlobalRoomViewUI GetRoomView()
    {
        if (_roomView != null && IsInstanceValid(_roomView)) return _roomView;

        _roomView = null;
        var scene = GetTree()?.CurrentScene;
        if (scene == null) return null;

        _roomView = scene.GetNodeOrNull<GlobalRoomViewUI>("GlobalRoomView");
        if (_roomView == null)
        {
            foreach (var child in scene.GetChildren())
            {
                if (child is GlobalRoomViewUI view)
                {
                    _roomView = view;
                    break;
                }
            }
        }
        return _roomView;
    }

    // ===================== Проверка: палец на интерфейсе? =====================

    /// <summary>
    /// true, если точка экрана попадает в видимый элемент интерфейса.
    /// Ищем только внутри CanvasLayer — в мировые узлы не спускаемся,
    /// там тысячи тайлов и обход был бы дорогим.
    /// </summary>
    private bool IsPointOverBlockingUi(Vector2 screenPos)
    {
        var tree = GetTree();
        if (tree == null) return false;

        // Автозагрузки: InventoryUI и подобные лежат прямо в /root
        if (ScanLayersIn(tree.Root, screenPos)) return true;

        // Слои внутри сцены музея
        var scene = tree.CurrentScene;
        if (scene != null && ScanLayersIn(scene, screenPos)) return true;

        return false;
    }

    private bool ScanLayersIn(Node parent, Vector2 point)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is CanvasLayer layer)
            {
                if (layer.Visible && ScanControls(layer, point)) return true;
            }
            else if (ScanLayersIn(child, point))
            {
                // Рекурсия: CanvasLayer может быть не прямым ребёнком сцены/root,
                // а лежать глубже (например, _uiLayer внутри PlacementModeUI).
                return true;
            }
        }
        return false;
    }

    private bool ScanControls(Node parent, Vector2 point)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is CanvasLayer nested)
            {
                // Вложенный слой: DisplayCaseUI, CollectionExhibitUI, магазин, прогресс
                if (nested.Visible && ScanControls(nested, point)) return true;
                continue;
            }

            if (child is Control control)
            {
                if (!control.IsVisibleInTree()) continue;

                if (control.MouseFilter != Control.MouseFilterEnum.Ignore &&
                    control.GetGlobalRect().HasPoint(point))
                    return true;

                if (ScanControls(control, point)) return true;
            }
        }
        return false;
    }

    // ===================== Публичное API (используется другими UI) =====================

    /// <summary>Плавное перемещение камеры к центру комнаты.</summary>
    public void MoveToRoomCenter(Vector2I globalTileCenter)
    {
        StopTween();
        _velocity = Vector2.Zero;

        var isoPos = IsoUtils.GridToIso(globalTileCenter.X, globalTileCenter.Y);
        Vector2 target = ClampPosition(new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y));

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "global_position", target, MoveDuration)
                     .SetTrans(Tween.TransitionType.Cubic)
                     .SetEase(Tween.EaseType.InOut);
        _currentTween.TweenCallback(Callable.From(UpdateActiveRoom));
    }

    /// <summary>Мгновенный прыжок камеры (используется при старте сцены).</summary>
    public void SnapToRoomCenter(Vector2I globalTileCenter)
    {
        StopTween();
        _velocity = Vector2.Zero;

        var isoPos = IsoUtils.GridToIso(globalTileCenter.X, globalTileCenter.Y);
        GlobalPosition = ClampPosition(new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y));
    }

    /// <summary>Плавно наводит камеру на объект и приближает.</summary>
    public void ZoomToObject(Vector2 worldPosition, float targetZoom = 1.5f)
    {
        StopTween();
        _velocity = Vector2.Zero;

        float zoom = Mathf.Clamp(targetZoom, MinZoom.X, MaxZoom.X);
        Vector2 target = ClampPositionAt(worldPosition, zoom);

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "global_position", target, 0.6f)
                     .SetTrans(Tween.TransitionType.Cubic)
                     .SetEase(Tween.EaseType.InOut);
        _currentTween.Parallel().TweenProperty(this, "zoom", new Vector2(zoom, zoom), 0.6f)
                     .SetTrans(Tween.TransitionType.Cubic)
                     .SetEase(Tween.EaseType.InOut);
    }

    /// <summary>Возвращает зум к обычному (вызывается при закрытии панелей).</summary>
    public void ResetZoom()
    {
        StopTween();
        _velocity = Vector2.Zero;

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "zoom", Vector2.One, 0.5f)
                     .SetTrans(Tween.TransitionType.Cubic)
                     .SetEase(Tween.EaseType.Out);
        _currentTween.TweenCallback(Callable.From(() =>
        {
            GlobalPosition = ClampPosition(GlobalPosition);
            UpdateActiveRoom();
        }));
    }

    private void StopTween()
    {
        if (_currentTween == null) return;

        if (IsInstanceValid(_currentTween) && _currentTween.IsRunning())
            _currentTween.Kill();

        _currentTween = null;
    }
}
