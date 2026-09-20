using Godot;

public partial class CameraController : Camera2D
{
    [Export] public float MoveDuration = 0.8f;
    [Export] public float DragSpeed = 1.5f; 
    [Export] public float ZoomSpeed = 0.1f; 
    [Export] public Vector2 MinZoom = new Vector2(0.5f, 0.5f); 
    [Export] public Vector2 MaxZoom = new Vector2(2.0f, 2.0f); 
    
    // === ПЕРЕИМЕНОВАНО, чтобы не конфликтовать с встроенными LimitLeft и т.д. ===
    [Export] public float BoundaryLeft = -200f;
    [Export] public float BoundaryRight = 2000f;
    [Export] public float BoundaryTop = -200f;
    [Export] public float BoundaryBottom = 1500f;

    private float GridOffsetX => MuseumConstants.GridOffsetX;
    private float GridOffsetY => MuseumConstants.GridOffsetY;

    private Tween _currentTween;
    private bool _isDragging = false;
    private Vector2 _dragStartPos;
    private Vector2 _cameraStartPos;

    public override void _Ready()
    {
        // Применяем наши границы к встроенным свойствам Godot
        LimitLeft = (int)BoundaryLeft;
        LimitRight = (int)BoundaryRight;
        LimitTop = (int)BoundaryTop;
        LimitBottom = (int)BoundaryBottom;
        
        Zoom = new Vector2(1, 1);
    }

    public override void _Input(InputEvent @event)
    {
        // === ПЕРЕТАСКИВАНИЕ ПРАВОЙ КНОПКОЙ МЫШИ ===
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                if (mouseButton.Pressed)
                {
                    // Начало перетаскивания
                    _isDragging = true;
                    _dragStartPos = mouseButton.Position;
                    _cameraStartPos = GlobalPosition;
                    
                    // Останавливаем анимацию, если она идёт
                    if (_currentTween != null && _currentTween.IsRunning())
                    {
                        _currentTween.Kill();
                    }
                }
                else
                {
                    // Конец перетаскивания
                    _isDragging = false;
                    
                    // Обновляем активную комнату после перетаскивания
                    UpdateActiveRoom();
                }
            }
            
            // === ЗУМ КОЛЁСИКОМ МЫШИ ===
            if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
            {
                ZoomCamera(ZoomSpeed);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
            {
                ZoomCamera(-ZoomSpeed);
            }
        }
        
        // === ДВИЖЕНИЕ МЫШИ ПРИ ПЕРЕТАСКИВАНИИ ===
        if (@event is InputEventMouseMotion mouseMotion && _isDragging)
        {
            Vector2 dragDelta = mouseMotion.Position - _dragStartPos;
            GlobalPosition = _cameraStartPos - dragDelta * DragSpeed;
            
            // Ограничиваем позицию камеры
            GlobalPosition = new Vector2(
                Mathf.Clamp(GlobalPosition.X, LimitLeft, LimitRight),
                Mathf.Clamp(GlobalPosition.Y, LimitTop, LimitBottom)
            );
        }
    }

    /// <summary>
    /// Увеличивает или уменьшает зум камеры
    /// </summary>
    private void ZoomCamera(float delta)
    {
        Vector2 newZoom = Zoom + new Vector2(delta, delta);
        newZoom = new Vector2(
            Mathf.Clamp(newZoom.X, MinZoom.X, MaxZoom.X),
            Mathf.Clamp(newZoom.Y, MinZoom.Y, MaxZoom.Y)
        );
        
        // Плавный зум
        var tween = CreateTween();
        tween.TweenProperty(this, "zoom", newZoom, 0.2f)
             .SetTrans(Tween.TransitionType.Sine)
             .SetEase(Tween.EaseType.Out);
    }

    /// <summary>
    /// Определяет активную комнату на основе текущей позиции камеры
    /// </summary>
    private void UpdateActiveRoom()
    {
        var globalView = GetTree().CurrentScene.GetNodeOrNull<GlobalRoomViewUI>("GlobalRoomView");
        if (globalView != null)
        {
            globalView.UpdateActiveRoomByCameraCenter(GlobalPosition);
        }
    }

    /// <summary>
    /// Плавное перемещение камеры к центру комнаты (вызывается при клике)
    /// </summary>
    public void MoveToRoomCenter(Vector2I globalTileCenter)
    {
        if (_currentTween != null && _currentTween.IsRunning())
        {
            _currentTween.Kill();
        }

        var isoPos = IsoUtils.GridToIso(globalTileCenter.X, globalTileCenter.Y);
        
        Vector2 targetPosition = new Vector2(
            GridOffsetX + isoPos.X,
            GridOffsetY + isoPos.Y
        );

        // Ограничиваем позицию
        targetPosition = new Vector2(
            Mathf.Clamp(targetPosition.X, BoundaryLeft, BoundaryRight),
            Mathf.Clamp(targetPosition.Y, BoundaryTop, BoundaryBottom)
        );

        GD.Print($"[Camera] Двигаемся к центру комнаты. Цель: {targetPosition}");

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "global_position", targetPosition, MoveDuration)
                       .SetTrans(Tween.TransitionType.Cubic)
                       .SetEase(Tween.EaseType.InOut);
        
        // После завершения анимации обновляем активную комнату
        _currentTween.TweenCallback(Callable.From(() =>
        {
            UpdateActiveRoom();
        }));
    }
    
    /// <summary>
    /// Мгновенный прыжок камеры (используется при старте)
    /// </summary>
    public void SnapToRoomCenter(Vector2I globalTileCenter)
    {
        var isoPos = IsoUtils.GridToIso(globalTileCenter.X, globalTileCenter.Y);
        GlobalPosition = new Vector2(
            GridOffsetX + isoPos.X,
            GridOffsetY + isoPos.Y
        );
        GD.Print($"[Camera] Мгновенный прыжок в: {GlobalPosition}");
    }

        /// <summary>
    /// Плавно перемещает камеру к объекту и приближает (зум)
    /// </summary>
    public void ZoomToObject(Vector2 worldPosition, float targetZoom = 1.5f)
    {
        if (_currentTween != null && _currentTween.IsRunning())
        {
            _currentTween.Kill();
        }

        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(worldPosition.X, BoundaryLeft, BoundaryRight),
            Mathf.Clamp(worldPosition.Y, BoundaryTop, BoundaryBottom)
        );

        _currentTween = CreateTween();
        
        // Двигаем камеру
        _currentTween.TweenProperty(this, "global_position", clampedPos, 0.6f)
                       .SetTrans(Tween.TransitionType.Cubic)
                       .SetEase(Tween.EaseType.InOut);
        
        // Одновременно меняем зум
        Vector2 newZoom = new Vector2(targetZoom, targetZoom);
        _currentTween.Parallel().TweenProperty(this, "zoom", newZoom, 0.6f)
                       .SetTrans(Tween.TransitionType.Cubic)
                       .SetEase(Tween.EaseType.InOut);
    }

    /// <summary>
    /// Сбрасывает зум к нормальному состоянию (вызывается при закрытии UI)
    /// </summary>
    public void ResetZoom()
    {
        if (_currentTween != null && _currentTween.IsRunning())
        {
            _currentTween.Kill();
        }

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "zoom", new Vector2(1f, 1f), 0.5f)
                       .SetTrans(Tween.TransitionType.Cubic)
                       .SetEase(Tween.EaseType.Out);
    }
}