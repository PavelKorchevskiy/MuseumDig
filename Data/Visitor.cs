using Godot;
using System.Collections.Generic;

public enum VisitorState
{
    Wandering,
    Looking,
    Transitioning,
    Exiting
}

public partial class Visitor : Control
{
    private const int CellSize = 50;
    private const int GridOffsetX = 150;
    private const int GridOffsetY = 100;
    
    private Vector2I _currentCell;
    private List<Vector2I> _path = new();
    private int _pathIndex = 0;
    
    private float _moveTimer = 0f;
    private const float MoveInterval = 0.5f;
    
    private float _lookTimer = 0f;
    private const float LookDuration = 4.0f;
    
    private float _decisionTimer = 0f;
    private const float DecisionInterval = 5.0f;
    
    private VisitorState _state = VisitorState.Wandering;
    private PlacedFurniture _lookingAt = null;
    
    private Room _currentRoom;
    private Direction _targetDoorDirection;
    
    private TextureRect _sprite;
    private int _gridOffsetX = 0;
    private int _gridOffsetY = 0;

     private float _animTimer = 0f;
    private float _frameDuration = 0.3f; // Скорость анимации (секунд на кадр)
    private int _currentFrame = 0;
    
    private string _currentState = "idle"; // walk, idle, look
    private string _currentBaseDir = "se"; // se (вниз-вправо) или ne (вверх-вправо)
    private bool _isFlipped = false;       // Для отражения (sw и nw)
    
    private string _visitorFolder = "1";   // ID папки с ассетами (можно рандомизироват
    
    // НОВОЕ: Публичное свойство для проверки видимости
    public Room CurrentRoom => _currentRoom;
    
        public override void _Ready()
    {
        CustomMinimumSize = new Vector2(64, 64); // Размер спрайта
        MouseFilter = MouseFilterEnum.Ignore;
        
        _sprite = new TextureRect();
        _sprite.CustomMinimumSize = new Vector2(64, 64); // Защита от схлопывания в 0
        _sprite.Size = new Vector2(64, 64);
        _sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _sprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _sprite.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_sprite);
        
        // Загружаем спрайт (убедитесь, что файл существует по этому пути!)
        string spritePath = "res://assets/museum/visitors/visitor_1_1.png";
        if (ResourceLoader.Exists(spritePath))
        {
            _sprite.Texture = GD.Load<Texture2D>(spritePath);
        }
        else
        {
            GD.PrintErr($"[Visitor] Спрайт не найден: {spritePath}");
            _sprite.Texture = GD.Load<Texture2D>("res://icon.svg"); // Заглушка
        }
        
    }
    
    public void Initialize(Room room, Vector2I startCell, int gridOffsetX, int gridOffsetY, string visitorType)
    {
        _currentRoom = room;
        _currentCell = startCell;
        _gridOffsetX = gridOffsetX; // Сохраняем смещение
        _gridOffsetY = gridOffsetY; // Сохраняем смещение
        _state = VisitorState.Wandering;
        UpdateVisualPosition();
        FindNewWanderTarget();
        _visitorFolder = visitorType;
        _currentState = "idle";
        ApplyFrameToSprite();
    }
    
        public override void _Process(double delta)
    {
        float fDelta = (float)delta;
        
        // Сначала обновляем логику состояний (ваш существующий switch)
        switch (_state)
        {
            case VisitorState.Wandering:
                ProcessWandering(fDelta);
                break;
            case VisitorState.Looking:
                ProcessLooking(fDelta);
                break;
            case VisitorState.Transitioning:
                ProcessTransitioning(fDelta);
                break;
            case VisitorState.Exiting:
                ProcessExiting(fDelta);
                break;
        }
        
        // Затем обновляем анимацию
        UpdateAnimation(fDelta);
    }
    
    // ===== СОСТОЯНИЯ =====
    
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
        // GD.Print("--------ProcessLooking " + _currentState);
        if (_lookTimer >= LookDuration)
        {
            FinishLooking();
        }
    }
    
    private void ProcessTransitioning(float delta)
    {
        if (_currentState != "walk")
        {
            if (_state != VisitorState.Looking)
{
    _currentState = "walk";
}
            _currentFrame = 0;
            _animTimer = 0f;
            ApplyFrameToSprite();
        }
        _moveTimer += delta;
        if (_moveTimer >= MoveInterval)
        {
            _moveTimer = 0f;
            
            if (_pathIndex < _path.Count)
            {
                _currentCell = _path[_pathIndex];
                _pathIndex++;
                UpdateVisualPosition();
            }
            else
            {
                CompleteTransition();
            }
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
            
            if (_pathIndex < _path.Count)
            {
                _currentCell = _path[_pathIndex];
                _pathIndex++;
                UpdateVisualPosition();
            }
            else
            {
                if (_currentRoom.IsMainHall)
                {
                    // GD.Print("[Visitor] Exited through street door!");
                    QueueFree();
                }
                else
                {
                    FindPathToMainHall();
                }
            }
        }
    }
    
    // ===== ДВИЖЕНИЕ =====
    
    private void MoveAlongPath()
    {
        if (_pathIndex >= _path.Count)
        {
            FindNewWanderTarget();
            return;
        }
        
        var nextCell = _path[_pathIndex];
        
        // === Определяем направление ===
        DetermineDirection(nextCell);
        
        _currentCell = nextCell;
        _pathIndex++;
        UpdateVisualPosition();
        CheckForFurniture();
        
        // === Устанавливаем анимацию ходьбы ===
        if (_currentState != "walk")
        {
            if (_state != VisitorState.Looking)
{
    _currentState = "walk";
}
            _currentFrame = 0;
            _animTimer = 0f;
            ApplyFrameToSprite();
        }
    }
    
            private void UpdateVisualPosition()
    {
        // === ЗАЩИТНЫЙ КЛАПАН (оставляем для надежности) ===
        if (_currentCell.X < 0 || _currentCell.Y < 0 || 
            _currentCell.X >= _currentRoom.Width || _currentCell.Y >= _currentRoom.Height)
        {
            GD.PrintErr($"[Visitor ОШИБКА] Выход за границы! Клетка: {_currentCell}. Сброс.");
            _currentCell = new Vector2I(1, 1);
        }

        var isoPos = IsoUtils.GridToIso(_currentCell.X, _currentCell.Y);
        
        // 1. Находим точку на полу, где должны стоять "ноги" персонажа.
        // Это центр по горизонтали и самый низ по вертикали изометрического ромба.
        float floorAnchorX = IsoUtils.TileWidth / 2f;
        float floorAnchorY = IsoUtils.TileHeight; 

        // 2. Находим точку на спрайте персонажа, которая является его "ногами".
        // Для спрайта 64x64 это центр по ширине и самый низ по высоте.
        float spriteAnchorX = 32f; 
        float spriteAnchorY = 64f; // Если у спрайта есть прозрачность снизу, уменьшите до 55-60f

        // 3. Формула идеального совмещения:
        Position = new Vector2(
            _gridOffsetX + isoPos.X + floorAnchorX - spriteAnchorX,
            _gridOffsetY + isoPos.Y + floorAnchorY - spriteAnchorY
        );
        
        // Z-порядок: +2 гарантирует, что посетитель будет поверх пола (Z=0), 
        // но под мебелью, если он находится "за" ней.
    ZIndex = IsoUtils.GetZOrder(_currentCell.X, _currentCell.Y) + 2;
        }
    
    private void FindNewWanderTarget()
    {
        _currentState = "idle";
        var freeCells = GetWalkableCells();
        if (freeCells.Count == 0) return;
        
        var target = freeCells[GD.RandRange(0, freeCells.Count - 1)];
        _path = FindPath(_currentCell, target);
        _pathIndex = 0;
    }
    
    private List<Vector2I> GetWalkableCells()
    {
        var cells = new List<Vector2I>();
        for (int x = 1; x < _currentRoom.Width - 1; x++)
        {
            for (int y = 1; y < _currentRoom.Height - 1; y++)
            {
                if (IsCellWalkable(new Vector2I(x, y)))
                {
                    cells.Add(new Vector2I(x, y));
                }
            }
        }
        return cells;
    }
    
    // ===== РЕШЕНИЯ =====
    
    private void MakeDecision()
    {
        float roll = GD.Randf();
        
        if (roll < 0.15f)
        {
            StartExiting();
        }
        else if (roll < 0.30f)
        {
            TryTransitionToNeighbor();
        }
    }
    
    // ===== ОСМОТР ЭКСПОНАТОВ =====
    
        private void CheckForFurniture()
    {
        if (_state != VisitorState.Wandering) return;

        foreach (var placed in _currentRoom.PlacedFurnitureList)
        {
            // === НОВАЯ ЛОГИКА: Определяем, интересен ли экспонат ===
            bool isInteresting = false;

            if (placed.Furniture is CollectionExhibit)
            {
                // Собранная коллекция (скелет) ВСЕГДА интересна!
                isInteresting = true; 
            }
            else if (placed.Furniture is DisplayCase)
            {
                // Витрина интересна, только если в ней что-то есть
                isInteresting = placed.GetAllItems().Count > 0; 
            }

            if (!isInteresting) continue;

            // Проверяем все 8 соседних клеток вокруг посетителя
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue; // Пропускаем клетку, где стоит сам посетитель

                    int checkX = _currentCell.X + dx;
                    int checkY = _currentCell.Y + dy;

                    // Проверяем, попадает ли соседняя клетка в границы этой мебели
                    if (checkX >= placed.Position.X && checkX < placed.Position.X + placed.Size.X &&
                        checkY >= placed.Position.Y && checkY < placed.Position.Y + placed.Size.Y)
                    {
                        
                        // 50% шанс, что посетитель решит остановиться и посмотреть
                        if (GD.Randf() < 0.5f) 
                        {
                            StartLooking(placed);
                            return; // Прерываем поиск, мы уже нашли цель
                        }
                    }
                }
            }
        }
    }
    
        private void StartLooking(PlacedFurniture furniture)
    {
        GD.Print($"[{Name}] >>> НАЧИНАЮ ОСМОТР: {furniture.FurnitureTypeId}");
        
        _state = VisitorState.Looking;
        _currentState = "look";
        _currentFrame = 0;
        _animTimer = 0f;
        
        _sprite.Modulate = Colors.White;
        
        _lookTimer = 0f;
        _lookingAt = furniture;
        _path.Clear();
        _pathIndex = 0;
        
        // === НОВОЕ: Вычисляем направление к центру экспоната ===
        // Центр экспоната = его позиция + половина его размера
        Vector2I exhibitCenter = new Vector2I(
            furniture.Position.X + furniture.Size.X / 2,
            furniture.Position.Y + furniture.Size.Y / 2
        );
        
        // Определяем направление от посетителя к экспонату
        DetermineDirection(exhibitCenter);
        
        // Применяем новый кадр с правильным направлением
        ApplyFrameToSprite();
        // ========================================================
        
        _sprite.QueueRedraw();
    }
    
            private void FinishLooking()
    {
        
        _state = VisitorState.Wandering;
        _currentState = "idle";
        _currentFrame = 0;
        _animTimer = 0f;
        
        _sprite.Modulate = Colors.White;
        _sprite.QueueRedraw();
        ApplyFrameToSprite();
        
        _lookingAt = null;
        _decisionTimer = 0f;
        FindNewWanderTarget();
    }
    
    // ===== ПЕРЕХОД МЕЖДУ ЗАЛАМИ =====
    
    private void TryTransitionToNeighbor()
    {
        var availableDoors = new List<KeyValuePair<Direction, Door>>();
        foreach (var kvp in _currentRoom.Doors)
        {
            if (kvp.Value.HasConnection && !kvp.Value.IsExitToStreet)
            {
                availableDoors.Add(kvp);
            }
        }
        
        if (availableDoors.Count == 0) return;
        
        var chosen = availableDoors[GD.RandRange(0, availableDoors.Count - 1)];
        _targetDoorDirection = chosen.Key;
        
        _path = FindPath(_currentCell, chosen.Value.Position);
        _pathIndex = 0;
        _state = VisitorState.Transitioning;
        
        GD.Print($"[Visitor] Moving to neighbor through {chosen.Key} door");
    }
    
    private void CompleteTransition()
    {
        var door = _currentRoom.GetDoor(_targetDoorDirection);
        if (door == null || string.IsNullOrEmpty(door.ConnectedRoomId))
        {
            ReturnToWandering();
            return;
        }
        
        var targetRoom = MuseumSystem.Instance.GetAllRooms().Find(r => r.Id == door.ConnectedRoomId);
        if (targetRoom == null)
        {
            ReturnToWandering();
            return;
        }
        
        _currentRoom = targetRoom;
        
        Direction oppositeDir = _targetDoorDirection switch
        {
            Direction.Top => Direction.Bottom,
            Direction.Bottom => Direction.Top,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => Direction.Top
        };
        
        var entryDoor = targetRoom.GetDoor(oppositeDir);
        if (entryDoor != null)
        {
            _currentCell = entryDoor.Position;
        }
        
        ReturnToWandering();
        // GD.Print($"[Visitor] Entered {targetRoom.DisplayName}");
    }
    
    private void ReturnToWandering()
    {
        _state = VisitorState.Wandering;
        _decisionTimer = 0f;
        UpdateVisualPosition();
        FindNewWanderTarget();
    }
    
    // ===== ВЫХОД ИЗ МУЗЕЯ =====
    
    private void StartExiting()
    {
        _state = VisitorState.Exiting;
        _path.Clear();
        _pathIndex = 0;
        
        if (_currentRoom.IsMainHall)
        {
            var streetDoor = _currentRoom.GetDoor(Direction.Bottom);
            if (streetDoor != null)
            {
                _path = FindPath(_currentCell, streetDoor.Position);
                _pathIndex = 0;
            }
        }
        else
        {
            FindPathToMainHall();
        }
        
        // GD.Print("[Visitor] Decided to leave the museum");
    }
    
    private void FindPathToMainHall()
    {
        var mainHall = MuseumSystem.Instance.GetAllRooms().Find(r => r.IsMainHall);
        if (mainHall == null) return;
        
        Door bestDoor = null;
        Direction bestDir = Direction.Top;
        int bestDist = int.MaxValue;
        
        foreach (var kvp in _currentRoom.Doors)
        {
            if (!kvp.Value.HasConnection || kvp.Value.IsExitToStreet) continue;
            
            var neighbor = MuseumSystem.Instance.GetAllRooms().Find(r => r.Id == kvp.Value.ConnectedRoomId);
            if (neighbor == null) continue;
            
            int dist = Mathf.Abs(neighbor.GlobalPosition.X - mainHall.GlobalPosition.X) +
                       Mathf.Abs(neighbor.GlobalPosition.Y - mainHall.GlobalPosition.Y);
            
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDoor = kvp.Value;
                bestDir = kvp.Key;
            }
        }
        
        if (bestDoor != null)
        {
            _path = FindPath(_currentCell, bestDoor.Position);
            _pathIndex = 0;
            _targetDoorDirection = bestDir;
            _state = VisitorState.Transitioning;
            // _visitorRect.Color = new Color(0.9f, 0.3f, 0.3f);
        }
    }
    
    // ===== УТИЛИТЫ =====
    
    private int CalculateViewingBonus(PlacedFurniture placed)
    {
        int total = 0;
        foreach (var item in placed.Furniture.GetAllItems())
        {
            var resource = GameData.GetResource(item.ResourceId);
            if (resource == null) continue;
            float mult = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(item.Quality);
            total += (int)(resource.BaseMuseumIncome * mult * 0.1f);
        }
        return total;
    }
    
            private List<Vector2I> FindPath(Vector2I start, Vector2I end)
    {
        GD.Print($"[Visitor {Name}] Поиск пути от {start} до {end}");
        
        var queue = new Queue<Vector2I>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var visited = new HashSet<Vector2I>();
        
        queue.Enqueue(start);
        visited.Add(start);
        cameFrom[start] = start;
        
        while (queue.Count > 0)
        {
            var pos = queue.Dequeue();
            if (pos == end) break;
            
            var directions = new Vector2I[] { new(0,-1), new(0,1), new(-1,0), new(1,0) };
            
            foreach (var dir in directions)
            {
                var next = new Vector2I(pos.X + dir.X, pos.Y + dir.Y);
                
                // Жесткая проверка границ
                if (next.X < 0 || next.X >= _currentRoom.Width || 
                    next.Y < 0 || next.Y >= _currentRoom.Height)
                {
                    continue;
                }
                
                // === НОВАЯ ПРОВЕРКА С ЛОГОМ ===
                bool walkable = IsCellWalkable(next);
                
                if (!walkable)
                {
                    // Логируем только если это клетка с мебелью
                    foreach (var placed in _currentRoom.PlacedFurnitureList)
                    {
                        if (next.X >= placed.Position.X && next.X < placed.Position.X + placed.Size.X &&
                            next.Y >= placed.Position.Y && next.Y < placed.Position.Y + placed.Size.Y)
                        {
                            // GD.Print($"[Visitor {Name}] Блокирую клетку {next} (занята {placed.FurnitureTypeId})");
                            break;
                        }
                    }
                    continue;
                }
                // ================================
                
                if (!visited.Contains(next))
                {
                    queue.Enqueue(next);
                    visited.Add(next);
                    cameFrom[next] = pos;
                }
            }
        }
        
        var path = new List<Vector2I>();
        var backtrack = end;
        while (backtrack != start)
        {
            path.Add(backtrack);
            if (!cameFrom.ContainsKey(backtrack)) break;
            backtrack = cameFrom[backtrack];
        }
        path.Reverse();
        
        GD.Print($"[Visitor {Name}] Найден путь длиной {path.Count}");
        return path;
    }

                private bool IsCellWalkable(Vector2I cell)
    {
        // 1. Проверяем границы
        if (cell.X < 0 || cell.X >= _currentRoom.Width || 
            cell.Y < 0 || cell.Y >= _currentRoom.Height)
        {
            return false;
        }
        
        // 2. Проверяем карту занятости
        bool isOccupied = _currentRoom._occupancyGrid[cell.X, cell.Y];
        
        // Логируем только для первых нескольких проверок, чтобы не спамить
        if (cell.X == 5 && cell.Y == 5) // Пример клетки, где должна быть мебель
        {
            GD.Print($"[IsCellWalkable] Клетка ({cell.X}, {cell.Y}): занята={isOccupied}");
        }
        
        if (isOccupied)
        {
            return false;
        }
        
        return true;
    }

        private void UpdateAnimation(float delta)
    {
        _animTimer += delta;
        
        // Если прошло достаточно времени, меняем кадр
        if (_animTimer >= _frameDuration)
        {
            _animTimer = 0f;
            _currentFrame = (_currentFrame + 1) % 4; // Цикл от 0 до 3
            
            ApplyFrameToSprite();
        }
    }
    private void ApplyFrameToSprite()
    {
        if (_sprite == null) return;

        // Определяем направление для имени файла
        string stateDir = _isFlipped ? (_currentBaseDir == "se" ? "ne" : "se") : _currentBaseDir;
        string texturePath = $"res://assets/museum/visitors/{_visitorFolder}/{_currentState}_{stateDir}_{_currentFrame}.png";

        if (ResourceLoader.Exists(texturePath))
        {
            _sprite.Texture = GD.Load<Texture2D>(texturePath);
            _sprite.CustomMinimumSize = new Vector2(64, 64);
            _sprite.Size = new Vector2(64, 64);
            _sprite.FlipH = _isFlipped;
        }
        else
        {
            // Fallback: если ne не найден, берём se и отражаем программно
            string fallbackDir = (stateDir == "ne") ? "se" : "ne";
            string fallbackPath = $"res://assets/museum/visitors/{_visitorFolder}/{_currentState}_{fallbackDir}_{_currentFrame}.png";

            if (ResourceLoader.Exists(fallbackPath))
            {
                _sprite.Texture = GD.Load<Texture2D>(fallbackPath);
                _sprite.CustomMinimumSize = new Vector2(64, 64);
                _sprite.Size = new Vector2(64, 64);
                _sprite.FlipH = !_isFlipped; // Инвертируем отражение
            }
            else
            {
                GD.PrintErr($"[Visitor] Кадр не найден: {texturePath} (fallback тоже не найден: {fallbackPath})");
            }
        }
    }

        private void DetermineDirection(Vector2I target)
    {
        int dx = target.X - _currentCell.X;
        int dy = target.Y - _currentCell.Y;

        // Определяем базовое направление и нужно ли отражение
        if (dx >= 0 && dy >= 0) 
        { 
            _currentBaseDir = "se"; 
            _isFlipped = false; 
        }
        else if (dx <= 0 && dy >= 0) 
        { 
            _currentBaseDir = "se"; 
            _isFlipped = true; // Отражаем SE, получаем SW (влево-вниз)
        }
        else if (dx >= 0 && dy <= 0) 
        { 
            _currentBaseDir = "ne"; 
            _isFlipped = false; 
        }
        else if (dx <= 0 && dy <= 0) 
        { 
            _currentBaseDir = "ne"; 
            _isFlipped = true; // Отражаем NE, получаем NW (влево-вверх)
        }
    }
}