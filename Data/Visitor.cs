using Godot;
using System.Collections.Generic;
using System.Linq;

public enum VisitorState
{
    Wandering,
    Looking,
    Exiting
}

public partial class Visitor : Control
{
    // === ГЛОБАЛЬНАЯ СИСТЕМА ===
    private Vector2I _globalCell;
    private MuseumLayout _layout;
    private string _currentRoomId;

    private int _variantId = 1;
    
    private float GridOffsetX => MuseumConstants.GridOffsetX;
    private float GridOffsetY => MuseumConstants.GridOffsetY;
    
    // === ДВИЖЕНИЕ И ПУТЬ ===
    private List<Vector2I> _path = new();
    private int _pathIndex = 0;
    private float _moveTimer = 0f;
    private const float MoveInterval = 0.5f;
    
    // === СОСТОЯНИЯ ===
    private VisitorState _state = VisitorState.Wandering;
    private float _lookTimer = 0f;
    private const float LookDuration = 4.0f;
    private float _decisionTimer = 0f;
    private const float DecisionInterval = 5.0f;
    private PlacedFurniture _lookingAt = null;
    
    // === АНИМАЦИЯ И СПРАЙТ ===
    private TextureRect _sprite;
    private float _animTimer = 0f;
    private float _frameDuration = 0.3f;
    private int _currentFrame = 0;
    private string _currentState = "idle";
    private string _currentBaseDir = "se";
    private bool _isFlipped = false;
    private string _visitorFolder = "1";

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(64, 64);
        MouseFilter = MouseFilterEnum.Ignore;
        
        _sprite = new TextureRect();
        _sprite.CustomMinimumSize = new Vector2(64, 64);
        _sprite.Size = new Vector2(64, 64);
        _sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _sprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _sprite.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_sprite);
        
        _sprite.Texture = GD.Load<Texture2D>("res://icon.svg");
    }
    
    public void Initialize(MuseumLayout layout, Vector2I startGlobalCell, string startRoomId)
    {
        _layout = layout;
        _globalCell = startGlobalCell;
        _currentRoomId = startRoomId;

        int randomVariant = (int)(GD.Randi() % 2) + 1; 
        _visitorFolder = randomVariant.ToString();
        
        GD.Print($"[Visitor] Спавн посетителя. Внешность: папка {_visitorFolder}");
        
        _path.Clear();
        _pathIndex = 0;
        
        UpdateVisualPosition();
        FindNewWanderTarget();
    }
    
    public override void _Process(double delta)
    {
        float fDelta = (float)delta;
        
        switch (_state)
        {
            case VisitorState.Wandering:
                ProcessWandering(fDelta);
                break;
            case VisitorState.Looking:
                ProcessLooking(fDelta);
                break;
            case VisitorState.Exiting:
                ProcessExiting(fDelta);
                break;
        }
        
        UpdateAnimation(fDelta);
        ApplyRoomVisibility();
    }
    
        private void UpdateVisualPosition()
    {
        var isoPos = IsoUtils.GridToIso(_globalCell.X, _globalCell.Y);

        // 1. Точка на тайле, к которой привязываем персонажа.
        // Было: IsoUtils.TileHeight (самый низ ромба).
        // Стало: IsoUtils.TileHeight * 0.65f 
        // (Это поднимает точку привязки на ~35% вверх, визуально поднимая персонажа на треть тайла)
        float tileFootX = IsoUtils.TileWidth / 2f;
        float tileFootY = IsoUtils.TileHeight * 0.65f; // <-- ГЛАВНОЕ ИЗМЕНЕНИЕ

        // 2. Точка на спрайте (где у картинки находятся "ноги").
        // 32 - центр по ширине. 52 - чуть выше самого низа картинки 
        // (компенсирует прозрачность снизу, если она есть).
        float spriteFootX = 32f;
        float spriteFootY = 52f; 

        // 3. Вычисляем итоговую позицию
        Position = new Vector2(
            GridOffsetX + isoPos.X + tileFootX - spriteFootX,
            GridOffsetY + isoPos.Y + tileFootY - spriteFootY
        );

        // 4. Z-индекс: Пол = 0, Посетитель = 2, Стена = 5.
        // Оставляем +2, чтобы они корректно прятались за стенами неактивных комнат.
        ZIndex = IsoUtils.GetZOrder(_globalCell.X, _globalCell.Y) + 4;
    }

    private void ApplyRoomVisibility()
    {
        if (_layout == null || _sprite == null) return;
        
        float alpha = _layout.GetRoomAlpha(_currentRoomId);
        _sprite.Modulate = new Color(1, 1, 1, alpha);
    }
    private void UpdateCurrentRoom()
    {
        int roomIdx = _layout.RoomIndex[_globalCell.X, _globalCell.Y];
        if (roomIdx >= 0 && roomIdx < _layout.Rooms.Count)
        {
            string newRoomId = _layout.Rooms[roomIdx].Id;
            if (newRoomId != _currentRoomId)
            {
                _currentRoomId = newRoomId;
                ApplyRoomVisibility();
            }
        }
    }
    
    private void ProcessWandering(float delta)
    {
        _moveTimer += delta;
        _decisionTimer += delta;
        
        if (_moveTimer >= MoveInterval)
        {
            _moveTimer = 0f;
            MoveAlongPath();
        }
        
        if (_decisionTimer >= DecisionInterval)
        {
            _decisionTimer = 0f;
            MakeDecision();
        }
    }
    
    private void ProcessLooking(float delta)
    {
        _lookTimer += delta;
        if (_lookTimer >= LookDuration)
        {
            FinishLooking();
        }
    }
    
    private void ProcessExiting(float delta)
    {
        if (_currentState != "walk")
        {
            _currentState = "walk";
            _currentFrame = 0;
            _animTimer = 0f;
            ApplyFrameToSprite();
        }
        
        _moveTimer += delta;
        if (_moveTimer >= MoveInterval)
        {
            _moveTimer = 0f;
            MoveAlongPath();
        }
    }
    
       private void MoveAlongPath()
    {
        if (_pathIndex >= _path.Count)
        {
            if (_state == VisitorState.Exiting)
            {
                QueueFree();
                return;
            }
            FindNewWanderTarget();
            return;
        }
        
        Vector2I nextCell = _path[_pathIndex];
        
        // 1. Определяем направление СРАЗУ при получении новой клетки
        DetermineDirection(nextCell);
        
        _globalCell = nextCell;
        _pathIndex++;
        
        UpdateVisualPosition();
        UpdateCurrentRoom();
        CheckForFurniture();
        
        // 2. Обновляем визуальное состояние
        if (_state != VisitorState.Looking)
        {
            bool wasWalking = (_currentState == "walk");
            _currentState = "walk";
            
            // ВАЖНО: Применяем новую текстуру и FlipH НЕМЕДЛЕННО, 
            // чтобы направление всегда совпадало с движением
            ApplyFrameToSprite();
            
            // Сбрасываем счетчик кадров в ноль ТОЛЬКО если посетитель только что начал идти 
            // (был в состоянии idle). Если он уже шел, не сбрасываем, чтобы анимация не дергалась.
            if (!wasWalking)
            {
                _currentFrame = 0;
                _animTimer = 0f;
            }
        }
    }
    
    public void FindNewWanderTarget()
    {
        if (_layout == null) return;
        List<Vector2I> walkableCells = new List<Vector2I>();
        
        for (int x = 0; x < MuseumLayout.GridWidth; x++)
        {
            for (int y = 0; y < MuseumLayout.GridHeight; y++)
            {
                var tile = _layout.Grid[x, y];
                if (tile == TileType.Floor || tile == TileType.Door)
                {
                    // === ПРОВЕРКА: Пропускаем закрытые комнаты ===
                    int roomIdx = _layout.RoomIndex[x, y];
                    if (roomIdx >= 0 && roomIdx < _layout.Rooms.Count)
                    {
                        if (!_layout.Rooms[roomIdx].IsUnlocked) continue;
                    }

                    if (x > 0 && x < MuseumLayout.GridWidth - 1 && y > 0 && y < MuseumLayout.GridHeight - 1)
                    {
                        walkableCells.Add(new Vector2I(x, y));
                    }
                }
            }
        }

        if (walkableCells.Count == 0) return;

        Vector2I target;
        int attempts = 0;
        do
        {
            target = walkableCells[GD.RandRange(0, walkableCells.Count - 1)];
            attempts++;
        } 
        while (target == _globalCell && attempts < 20);

        _path = FindPath(_globalCell, target);
        _pathIndex = 0;
    }
    
    private List<Vector2I> FindPath(Vector2I start, Vector2I goal)
    {
        var queue = new Queue<Vector2I>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        
        queue.Enqueue(start);
        cameFrom[start] = start;

        Vector2I[] directions = new Vector2I[]
        {
            new Vector2I(1, 0), new Vector2I(-1, 0),
            new Vector2I(0, 1), new Vector2I(0, -1)
        };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current == goal) break;

            foreach (var dir in directions)
            {
                var next = new Vector2I(current.X + dir.X, current.Y + dir.Y);
                
                if (next.X < 0 || next.X >= MuseumLayout.GridWidth ||
                    next.Y < 0 || next.Y >= MuseumLayout.GridHeight)
                    continue;

                var tile = _layout.Grid[next.X, next.Y];
                if (tile != TileType.Floor && tile != TileType.Door)
                    continue;

                if (!cameFrom.ContainsKey(next))
                {
                    queue.Enqueue(next);
                    cameFrom[next] = current;
                }
            }
        }

        var path = new List<Vector2I>();
        if (!cameFrom.ContainsKey(goal)) return path;

        var step = goal;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }
        path.Reverse();
        return path;
    }

    // === ИСПРАВЛЕННЫЙ МЕТОД ПРОВЕРКИ МЕБЕЛИ ===
    private void CheckForFurniture()
    {
        if (_state != VisitorState.Wandering) return;

        var allRooms = MuseumLayout.Instance.Rooms;
        if (allRooms == null) return;
        
        // 1. Берем старый Room (чтобы получить список мебели)
        Room currentRoom = allRooms.Find(r => r.Id == _currentRoomId);
        if (currentRoom == null) return;

        // 2. Берем новый RoomConfig из Layout (чтобы получить GlobalOffset)
        var roomConfig = _layout.GetRoom(_currentRoomId);
        if (roomConfig == null) return;

        // 3. Переводим глобальные координаты посетителя в локальные для этой комнаты
        Vector2I localCell = new Vector2I(
            _globalCell.X - roomConfig.GlobalOffset.X,
            _globalCell.Y - roomConfig.GlobalOffset.Y
        );

        foreach (var placed in currentRoom.PlacedFurnitureList)
        {
            bool isInteresting = false;
            if (placed.Furniture is CollectionExhibit) isInteresting = true; 
            else if (placed.Furniture is DisplayCase) isInteresting = placed.GetAllItems().Count > 0; 

            if (!isInteresting) continue;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;

                    int checkX = localCell.X + dx;
                    int checkY = localCell.Y + dy;

                    if (checkX >= placed.Position.X && checkX < placed.Position.X + placed.Size.X &&
                        checkY >= placed.Position.Y && checkY < placed.Position.Y + placed.Size.Y)
                    {
                        if (GD.Randf() < 0.5f) 
                        {
                            StartLooking(placed, localCell);
                            return;
                        }
                    }
                }
            }
        }
    }
    
    private void StartLooking(PlacedFurniture furniture, Vector2I localVisitorCell)
    {
        _state = VisitorState.Looking;
        _currentState = "look";
        _currentFrame = 0;
        _animTimer = 0f;
        _lookTimer = 0f;
        _lookingAt = furniture;
        _path.Clear();
        _pathIndex = 0;
        
        Vector2I exhibitCenter = new Vector2I(
            furniture.Position.X + furniture.Size.X / 2,
            furniture.Position.Y + furniture.Size.Y / 2
        );
        
        DetermineDirectionLocal(exhibitCenter, localVisitorCell);
        ApplyFrameToSprite();
    }
    
    private void FinishLooking()
    {
        _state = VisitorState.Wandering;
        _currentState = "idle";
        _currentFrame = 0;
        _animTimer = 0f;
        _lookingAt = null;
        _decisionTimer = 0f;
        
        ApplyFrameToSprite();
        FindNewWanderTarget();
    }
    
    private void MakeDecision()
    {
        float roll = GD.Randf();
        // TODO
        if (roll < 0.0015f)
        {
            StartExiting();
        }
    }

       private void StartExiting()
    {
        _state = VisitorState.Exiting;
        _path.Clear();
        _pathIndex = 0;

        // Получаем координаты выхода на улицу из Layout
        Vector2I targetGlobal = MuseumLayout.Instance.GetStreetExitPosition();

        GD.Print($"[Visitor] Начинаю выход из музея. Цель: {targetGlobal}");

        // Строим путь к выходу
        _path = FindPath(_globalCell, targetGlobal);
        _pathIndex = 0;

        // Если путь не найден (например, выход заблокирован мебелью), просто удаляем посетителя
        if (_path.Count == 0)
        {
            GD.PrintErr("[Visitor] Путь к выходу не найден! Удаляю посетителя.");
            QueueFree();
        }
    }
    
    private void UpdateAnimation(float delta)
    {
        _animTimer += delta;
        if (_animTimer >= _frameDuration)
        {
            _animTimer = 0f;
            _currentFrame = (_currentFrame + 1) % 4;
            ApplyFrameToSprite();
        }
    }
    
        private void ApplyFrameToSprite()
    {
        if (_sprite == null) return;

        string stateDir = _currentBaseDir;
        string texturePath = $"res://assets/museum/visitors/{_visitorFolder}/{_currentState}_{stateDir}_{_currentFrame}.png";

        if (ResourceLoader.Exists(texturePath))
        {
            _sprite.Texture = GD.Load<Texture2D>(texturePath);
            _sprite.FlipH = _isFlipped;
        }
        else
        {
            // ВАЖНО: Логируем отсутствующие спрайты. 
            // Если вы видите эту ошибку в консоли, значит в папке нет нужного файла, 
            // и именно из-за этого раньше включалась неправильная подмена направления.
            GD.PrintErr($"[Visitor] ⚠️ Отсутствует спрайт: {texturePath}. Проверьте папку assets!");
            
            // Временная заглушка, чтобы игра не крашилась и не показывала неправильное направление
            string fallbackPath = $"res://assets/museum/visitors/{_visitorFolder}/walk_se_0.png";
            if (ResourceLoader.Exists(fallbackPath))
            {
                _sprite.Texture = GD.Load<Texture2D>(fallbackPath);
                _sprite.FlipH = false;
            }
        }
    }

        private void DetermineDirection(Vector2I target)
    {
        int dx = target.X - _globalCell.X;
        int dy = target.Y - _globalCell.Y;

        // Строгие неравенства для избежания конфликтов при dx=0 или dy=0
        if (dx > 0 && dy > 0)      { _currentBaseDir = "se"; _isFlipped = true; }  // Вниз-вправо
        else if (dx < 0 && dy > 0) { _currentBaseDir = "se"; _isFlipped = false; } // Вниз-влево
        else if (dx > 0 && dy < 0) { _currentBaseDir = "ne"; _isFlipped = false; } // Вверх-вправо
        else if (dx < 0 && dy < 0) { _currentBaseDir = "ne"; _isFlipped = true; }  // Вверх-влево
        
        // Обработка движения строго по осям (выбираем наиболее подходящую базу)
        else if (dx > 0 && dy == 0) { _currentBaseDir = "se"; _isFlipped = true; }  // Строго вправо
        else if (dx < 0 && dy == 0) { _currentBaseDir = "ne"; _isFlipped = true; } // Строго влево
        else if (dx == 0 && dy > 0) { _currentBaseDir = "se"; _isFlipped = false; } // Строго вниз
        else if (dx == 0 && dy < 0) { _currentBaseDir = "ne"; _isFlipped = false; } // Строго вверх
    }

    private void DetermineDirectionLocal(Vector2I target, Vector2I current)
    {
        int dx = target.X - current.X;
        int dy = target.Y - current.Y;

        if (dx > 0 && dy > 0)      { _currentBaseDir = "se"; _isFlipped = true; }
        else if (dx < 0 && dy > 0) { _currentBaseDir = "se"; _isFlipped = false; }
        else if (dx > 0 && dy < 0) { _currentBaseDir = "ne"; _isFlipped = false; }
        else if (dx < 0 && dy < 0) { _currentBaseDir = "ne"; _isFlipped = true; }
        
        else if (dx > 0 && dy == 0) { _currentBaseDir = "se"; _isFlipped = true; }
        else if (dx < 0 && dy == 0) { _currentBaseDir = "ne"; _isFlipped = true; }
        else if (dx == 0 && dy > 0) { _currentBaseDir = "se"; _isFlipped = false; }
        else if (dx == 0 && dy < 0) { _currentBaseDir = "ne"; _isFlipped = false; }
    }
}