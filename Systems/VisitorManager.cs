using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class VisitorManager : CanvasLayer
{
    public static VisitorManager Instance { get; private set; }
    
    private List<Visitor> _visitors = new();
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 10.0f;

    private Control _roomContainer;
    private int _gridOffsetX = 500;
    private int _gridOffsetY = 100;
    
    public override void _Ready()
    {
        Instance = this;
        Layer = 25; // Поверх всего
        Name = "VisitorManager";
    }
    
    public override void _Process(double delta)
    {
        _spawnTimer += (float)delta;
        
        int maxVisitors = GetMaxVisitors();
        
        if (_spawnTimer >= SpawnInterval && _visitors.Count < maxVisitors)
        {
            _spawnTimer = 0f;
            SpawnVisitor();
        }
        
        _visitors.RemoveAll(v => v == null || !IsInstanceValid(v));
        
        // Обновляем видимость всех посетителей
        UpdateVisitorVisibility();
    }
    
    private int GetMaxVisitors()
    {
        if (MuseumSystem.Instance == null) return 0;
        return MuseumSystem.Instance.GetAllRooms().Count * 2;
    }
    
    private void UpdateVisitorVisibility()
    {
        _visitors.RemoveAll(v => v == null || !IsInstanceValid(v));
    }
    
        private void SpawnVisitor()
    {
        // Спавним в ТЕКУЩЕЙ комнате
        var currentRoom = MuseumSystem.Instance?.GetCurrentRoom();
        if (currentRoom == null) return;
        
        // === ИСПРАВЛЕНИЕ: Явно ищем НИЖНЮЮ дверь (выход на улицу) ===
        Door spawnDoor = null;
        
        if (currentRoom.Doors.ContainsKey(Direction.Bottom))
        {
            spawnDoor = currentRoom.Doors[Direction.Bottom];
        }
        else
        {
            // Запасной вариант: ищем любую дверь с флагом IsExitToStreet
            spawnDoor = currentRoom.Doors.Values.FirstOrDefault(d => d.IsExitToStreet);
        }
        
        if (spawnDoor == null)
        {
            GD.PrintErr("[VisitorManager] В текущей комнате нет нижней двери для спавна!");
            return;
        }
        // ================================================================
        
        var visitor = new Visitor();
        visitor.Name = $"Visitor_{GD.RandRange(1000, 9999)}";
        
        if (_roomContainer != null)
        {
            _roomContainer.AddChild(visitor);
        }
        else
        {
            AddChild(visitor);
        }
        
        visitor.Initialize(currentRoom, spawnDoor.Position, _gridOffsetX, _gridOffsetY, "1");
        
        _visitors.Add(visitor);
        GD.Print($"[VisitorManager] Спавн посетителя в {currentRoom.Id} через нижнюю дверь (всего: {_visitors.Count}/{GetMaxVisitors()})");
    }

    public void SetRoomContainer(Control roomContainer, int gridOffsetX, int gridOffsetY)
    {
        _roomContainer = roomContainer;
        _gridOffsetX = gridOffsetX;
        _gridOffsetY = gridOffsetY;
        GD.Print($"[VisitorManager] Привязан к комнате. Смещения: {_gridOffsetX}, {_gridOffsetY}");
    }
}