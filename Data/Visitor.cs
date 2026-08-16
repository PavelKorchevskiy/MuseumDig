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
    private const float MoveInterval = 0.3f;
    
    private float _lookTimer = 0f;
    private const float LookDuration = 2.0f;
    
    private float _decisionTimer = 0f;
    private const float DecisionInterval = 5.0f;
    
    private VisitorState _state = VisitorState.Wandering;
    private PlacedFurniture _lookingAt = null;
    
    private Room _currentRoom;
    private Direction _targetDoorDirection;
    
    private ColorRect _visitorRect;
    
    // НОВОЕ: Публичное свойство для проверки видимости
    public Room CurrentRoom => _currentRoom;
    
    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(30, 30);
        MouseFilter = MouseFilterEnum.Ignore;
        
        _visitorRect = new ColorRect();
        _visitorRect.Size = new Vector2(30, 30);
        _visitorRect.Color = new Color(0.9f, 0.5f, 0.2f);
        _visitorRect.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_visitorRect);
        
        var label = new Label();
        label.Text = "👤";
        label.Position = new Vector2(5, 2);
        label.AddThemeFontSizeOverride("font_size", 18);
        label.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(label);
    }
    
    public void Initialize(Room room, Vector2I startCell)
    {
        _currentRoom = room;
        _currentCell = startCell;
        _state = VisitorState.Wandering;
        UpdateVisualPosition();
        FindNewWanderTarget();
    }
    
    public override void _Process(double delta)
    {
        // ИСПРАВЛЕНИЕ: Убрали проверку Visible — логика работает всегда
        // Видимость управляется из VisitorManager
        
        switch (_state)
        {
            case VisitorState.Wandering:
                ProcessWandering((float)delta);
                break;
            case VisitorState.Looking:
                ProcessLooking((float)delta);
                break;
            case VisitorState.Transitioning:
                ProcessTransitioning((float)delta);
                break;
            case VisitorState.Exiting:
                ProcessExiting((float)delta);
                break;
        }
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
        if (_lookTimer >= LookDuration)
        {
            FinishLooking();
        }
    }
    
    private void ProcessTransitioning(float delta)
    {
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
        
        _currentCell = _path[_pathIndex];
        _pathIndex++;
        UpdateVisualPosition();
        CheckForFurniture();
    }
    
    private void UpdateVisualPosition()
{
    // Изометрическая позиция
    var isoPos = IsoUtils.GridToIso(_currentCell.X, _currentCell.Y);
    
    Position = new Vector2(
        500 + isoPos.X, // GridOffsetX из RoomViewUI
        100 + isoPos.Y  // GridOffsetY из RoomViewUI
    );
    
    // Z-порядок для правильной отрисовки
    ZIndex = IsoUtils.GetZOrder(_currentCell.X, _currentCell.Y) + 2;
}
    
    private void FindNewWanderTarget()
    {
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
                if (_currentRoom.IsWalkable(x, y))
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
            if (placed.Furniture.GetAllItems().Count == 0) continue;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int checkX = _currentCell.X + dx;
                    int checkY = _currentCell.Y + dy;
                    
                    if (checkX >= placed.Position.X && checkX < placed.Position.X + placed.Size.X &&
                        checkY >= placed.Position.Y && checkY < placed.Position.Y + placed.Size.Y)
                    {
                        if (GD.Randf() < 0.4f)
                        {
                            StartLooking(placed);
                            return;
                        }
                    }
                }
            }
        }
    }
    
    private void StartLooking(PlacedFurniture furniture)
    {
        _state = VisitorState.Looking;
        _lookTimer = 0f;
        _lookingAt = furniture;
        _visitorRect.Color = new Color(0.2f, 0.9f, 0.2f);
        _path.Clear();
        _pathIndex = 0;
    }
    
    private void FinishLooking()
    {
        _state = VisitorState.Wandering;
        _visitorRect.Color = new Color(0.9f, 0.5f, 0.2f);
        
        if (_lookingAt != null)
        {
            int bonus = CalculateViewingBonus(_lookingAt);
            if (bonus > 0) Wallet.Instance.AddCoins(bonus);
        }
        
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
        _visitorRect.Color = new Color(0.5f, 0.5f, 0.9f);
        
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
        GD.Print($"[Visitor] Entered {targetRoom.DisplayName}");
    }
    
    private void ReturnToWandering()
    {
        _state = VisitorState.Wandering;
        _visitorRect.Color = new Color(0.9f, 0.5f, 0.2f);
        _decisionTimer = 0f;
        UpdateVisualPosition();
        FindNewWanderTarget();
    }
    
    // ===== ВЫХОД ИЗ МУЗЕЯ =====
    
    private void StartExiting()
    {
        _state = VisitorState.Exiting;
        _visitorRect.Color = new Color(0.9f, 0.3f, 0.3f);
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
            _visitorRect.Color = new Color(0.9f, 0.3f, 0.3f);
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
                if (!visited.Contains(next) && _currentRoom.IsWalkable(next.X, next.Y))
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
        return path;
    }
}