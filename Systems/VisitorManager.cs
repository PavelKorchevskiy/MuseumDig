using Godot;
using System;
using System.Collections.Generic;

public class VisitorManager
{
    private static VisitorManager _instance;
    
    public static VisitorManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new VisitorManager();
                _instance.Initialize();
            }
            return _instance;
        }
    }

    private List<Visitor> _visitors = new List<Visitor>();
    private MuseumLayout _layout;
    private bool _isInitialized = false;
    
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 3.0f;
    private int _visitorSpawnCounter = 0;

    private VisitorManager() { }

    private void Initialize()
    {
        _layout = MuseumLayout.Instance;
        if (_layout != null)
        {
            _isInitialized = true;
            GD.Print($"[VisitorManager] ✅ Инициализирован как чистый C# синглтон. Макс. посетителей: {MuseumSystem.Instance?.CalculateMaxVisitors() ?? 0}");
        }
    }

    public void ProcessDelta(float delta)
    {
        if (!_isInitialized)
        {
            Initialize();
            if (!_isInitialized) return;
        }

        // Очистка мёртвых посетителей
        _visitors.RemoveAll(v => !GodotObject.IsInstanceValid(v));

        // Спавн
        int maxVisitors = MuseumSystem.Instance?.CalculateMaxVisitors() ?? 0;

        if (_visitors.Count < maxVisitors)
        {
            _spawnTimer += delta;
            if (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer = 0f;
                SpawnNewVisitor();
            }
        }
        else
        {
            _spawnTimer = 0f;
        }
    }

     private void SpawnNewVisitor()
    {
        if (_layout == null) return;

        var mainHall = _layout.GetRoom("main_hall");
        if (mainHall == null) return;

        // Получаем концы дорожки
        var (leftEnd, rightEnd) = MuseumBackground.GetWayEndPoints();
        
        // Случайно выбираем один из концов
        Vector2I spawnPos = (GD.Randf() < 0.5f) ? leftEnd : rightEnd;

        // Проверяем, что позиция проходимая
        if (spawnPos.X < 0 || spawnPos.X >= MuseumLayout.GridWidth ||
            spawnPos.Y < 0 || spawnPos.Y >= MuseumLayout.GridHeight ||
            (_layout.Grid[spawnPos.X, spawnPos.Y] != TileType.Floor && 
             _layout.Grid[spawnPos.X, spawnPos.Y] != TileType.Door))
        {
            spawnPos = FindNearestWalkable(spawnPos);
        }

        var visitor = new Visitor();
        visitor.Initialize(_layout, spawnPos, mainHall.Id);
        
        visitor.AddToSceneTree();
        _visitors.Add(visitor);

        MuseumSystem.Instance?.OnVisitorEntered();
        CheckAndResetTicketDrop();
        
        int max = MuseumSystem.Instance?.CalculateMaxVisitors() ?? 0;
        GD.Print($"[VisitorManager] 🚶 Новый посетитель на ({spawnPos.X},{spawnPos.Y})! Всего: {_visitors.Count}/{max}");
    }

    private Vector2I FindNearestWalkable(Vector2I from)
    {
        for (int radius = 1; radius < 5; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int x = from.X + dx;
                    int y = from.Y + dy;

                    if (x < 0 || x >= MuseumLayout.GridWidth || y < 0 || y >= MuseumLayout.GridHeight)
                        continue;

                    if (_layout.Grid[x, y] == TileType.Floor || _layout.Grid[x, y] == TileType.Door)
                    {
                        return new Vector2I(x, y);
                    }
                }
            }
        }
        return from;
    }

    public bool CheckAndResetTicketDrop()
    {
        _visitorSpawnCounter++;
        if (_visitorSpawnCounter >= 20)
        {
            _visitorSpawnCounter = 0;
            return true;
        }
        return false;
    }
}