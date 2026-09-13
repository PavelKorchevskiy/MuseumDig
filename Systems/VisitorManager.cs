using Godot;
using System.Collections.Generic;

public partial class VisitorManager : Node
{
    public static VisitorManager Instance { get; private set; }

    private List<Visitor> _visitors = new List<Visitor>();
    private MuseumLayout _layout;
    private bool _isInitialized = false;

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
        }
    }

    private void TryInitialize()
    {
        // Ищем узел по типу (самый надёжный способ в Godot)
        var globalView = FindNodeByType<GlobalRoomViewUI>(GetTree().CurrentScene);

        if (globalView != null)
        {
            _layout = MuseumLayout.Instance; // ТЕПЕРЬ БЕРЁМ НАПРЯМУЮ ИЗ СИНГЛТОНА

            if (_layout != null)
            {
                _isInitialized = true;
                GD.Print("[VisitorManager] ✅ Инициализация успешна! Layout получен.");
                SpawnInitialVisitors();
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

    private void SpawnInitialVisitors()
    {
        var mainHall = _layout.GetRoom("main_hall");
        if (mainHall == null)
        {
            GD.PrintErr("[VisitorManager] ❌ ОШИБКА: Комната 'main_hall' не найдена в Layout!");
            return;
        }

        GD.Print($"[VisitorManager] ✅ Найден главный зал: '{mainHall.Id}'");
        GD.Print($"[VisitorManager]    Глобальный оффсет: {mainHall.GlobalOffset}, Размер: {mainHall.Width}x{mainHall.Height}");

        // Спавним 5 посетителей
        for (int i = 0; i < 20; i++)
        {
            SpawnVisitorInRoom(mainHall);
        }
    }

    // === ИСПРАВЛЕНО: RoomConfig заменён на Room ===
    private void SpawnVisitorInRoom(Room room)
    {
        var visitor = new Visitor();
        Vector2I startPos;

        // Поскольку мы убрали список Doors, мы просто используем известные координаты главного входа
        // В MuseumLayout.BuildLayout главный вход находится на (21, 32) и (22, 32)
        if (room.Id == "main_hall")
        {
            // Спавним чуть внутри комнаты (на 1 клетку выше входа)
            startPos = new Vector2I(21, 31);
            GD.Print($"[VisitorManager] 🚶 Спавн посетителя у главного входа: {startPos}");
        }
        else
        {
            // Для других комнат спавним в центре
            startPos = new Vector2I(
                room.GlobalOffset.X + room.Width / 2,
                room.GlobalOffset.Y + room.Height / 2
            );
            GD.Print($"[VisitorManager] 🚶 Спавн посетителя в центре комнаты {room.Id}: {startPos}");
        }

        // Проверка границ
        if (startPos.X < 0 || startPos.X >= MuseumLayout.GridWidth ||
            startPos.Y < 0 || startPos.Y >= MuseumLayout.GridHeight)
        {
            GD.PrintErr($"[VisitorManager] Позиция {startPos} вне сетки! Используем центр.");
            startPos = new Vector2I(room.GlobalOffset.X + room.Width / 2, room.GlobalOffset.Y + room.Height / 2);
        }
        // Проверка проходимости
        else if (_layout.Grid[startPos.X, startPos.Y] != TileType.Floor &&
                 _layout.Grid[startPos.X, startPos.Y] != TileType.Door)
        {
            GD.PrintErr($"[VisitorManager] Позиция {startPos} непроходима (тайл: {_layout.Grid[startPos.X, startPos.Y]})! Ищем ближайшую...");
            startPos = FindNearestWalkable(startPos);
        }

        visitor.Initialize(_layout, startPos, room.Id);
        GetTree().CurrentScene.AddChild(visitor);
        _visitors.Add(visitor);
    }

    /// <summary>
    /// Ищет ближайшую проходимую клетку к указанной позиции
    /// </summary>
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

                    if (x < 0 || x >= MuseumLayout.GridWidth ||
                        y < 0 || y >= MuseumLayout.GridHeight)
                        continue;

                    if (_layout.Grid[x, y] == TileType.Floor || _layout.Grid[x, y] == TileType.Door)
                    {
                        return new Vector2I(x, y);
                    }
                }
            }
        }
        return from; // Если ничего не нашли, возвращаем исходную
    }
}