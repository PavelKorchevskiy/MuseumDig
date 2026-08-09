using Godot;
using System.Collections.Generic;

public partial class VisitorManager : CanvasLayer
{
    public static VisitorManager Instance { get; private set; }
    
    private List<Visitor> _visitors = new();
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 5.0f;
    
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
        return MuseumSystem.Instance.GetAllRooms().Count * 5;
    }
    
    private void UpdateVisitorVisibility()
    {
        var museum = GetTree().CurrentScene as Museum;
        bool inMuseum = museum != null;
        Room playerRoom = inMuseum ? MuseumSystem.Instance?.GetCurrentRoom() : null;
        
        foreach (var visitor in _visitors)
        {
            if (visitor == null || !IsInstanceValid(visitor)) continue;
            
            // Показываем только если игрок в музее и в том же зале
            visitor.Visible = inMuseum && playerRoom != null && 
                             visitor.CurrentRoom != null && 
                             visitor.CurrentRoom.Id == playerRoom.Id;
        }
    }
    
    private void SpawnVisitor()
    {
        var mainHall = MuseumSystem.Instance?.GetAllRooms().Find(r => r.IsMainHall);
        if (mainHall == null) return;
        
        var streetDoor = mainHall.GetDoor(Direction.Bottom);
        if (streetDoor == null || !streetDoor.IsExitToStreet) return;
        
        var visitor = new Visitor();
        visitor.Name = $"Visitor_{GD.RandRange(1000, 9999)}";
        
        AddChild(visitor);
        visitor.Initialize(mainHall, streetDoor.Position);
        
        _visitors.Add(visitor);
        GD.Print($"[VisitorManager] Spawned visitor (total: {_visitors.Count}/{GetMaxVisitors()})");
    }
}