using Godot;
using System.Collections.Generic;

public partial class VisitorManager : Node
{
    public static VisitorManager Instance { get; private set; }

    private List<Visitor> _visitors = new List<Visitor>();
    private MuseumLayout _layout;
    private bool _isInitialized = false;
    
    // Таймер для контроля частоты появления
    private float _spawnTimer = 0f;
    private const float SpawnInterval = 3.0f; // Проверяем возможность спавна каждые 3 секунды

    public override void _Ready()
    {
        Instance = this;
        TryInitialize();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            TryInitialize();
            return;
        }

        // 1. ОЧИСТКА: Удаляем из списка тех, кто уже был уничтожен (QueueFree)
        _visitors.RemoveAll(v => !GodotObject.IsInstanceValid(v));

        // 2. ПРОВЕРКА ЛИМИТА: Сколько посетителей должно быть сейчас?
        int maxVisitors = MuseumSystem.Instance.CalculateMaxVisitors();

        // 3. СПАВН: Если текущих меньше максимума, запускаем таймер
        if (_visitors.Count < maxVisitors)
        {
            _spawnTimer += (float)delta;
            if (_spawnTimer >= SpawnInterval)
            {
                _spawnTimer = 0f;
                SpawnNewVisitor();
            }
        }
        else
        {
            // Сбрасываем таймер, если лимит достигнут, чтобы не было задержки при увеличении лимита
            _spawnTimer = 0f;
        }
    }

    private void TryInitialize()
    {
        var globalView = FindNodeByType<GlobalRoomViewUI>(GetTree().CurrentScene);
        if (globalView != null)
        {
            _layout = MuseumLayout.Instance;
            if (_layout != null)
            {
                _isInitialized = true;
                GD.Print($"[VisitorManager] ✅ Инициализация успешна! Макс. посетителей: {MuseumSystem.Instance.CalculateMaxVisitors()}");
            }
        }
    }

    private T FindNodeByType<T>(Node node) where T : class
    {
        if (node is T result) return result;
        foreach (var child in node.GetChildren())
        {
            var found = FindNodeByType<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private void SpawnNewVisitor()
    {
        var mainHall = _layout.GetRoom("main_hall");
        if (mainHall == null) return;

        var visitor = new Visitor();
        Vector2I startPos = new Vector2I(21, 31); // Главный вход

        // Проверка проходимости (на случай, если вход заблокировали мебелью)
        if (_layout.Grid[startPos.X, startPos.Y] != TileType.Floor && _layout.Grid[startPos.X, startPos.Y] != TileType.Door)
        {
            startPos = FindNearestWalkable(startPos);
        }

        visitor.Initialize(_layout, startPos, mainHall.Id);
        GetTree().CurrentScene.AddChild(visitor);
        _visitors.Add(visitor);

        // === ГЛАВНОЕ: Начисляем деньги за вход ===
        MuseumSystem.Instance.OnVisitorEntered();
        
        GD.Print($"[VisitorManager] 🚶 Новый посетитель! Всего в музее: {_visitors.Count} / {MuseumSystem.Instance.CalculateMaxVisitors()}");
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
}