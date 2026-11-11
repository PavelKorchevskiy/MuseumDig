using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

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

    // Интервал спавна подстраивается под развитие музея (см. ProcessDelta):
    // чем больше лимит посетителей, тем чаще он пытается спавнить, чтобы
    // освобождающиеся места не простаивали. Эти границы задают "пол" и "потолок".
    public const float MinSpawnInterval = 1.0f; // не чаще раза в секунду, даже для огромного музея
    public const float MaxSpawnInterval = 3.0f; // не реже, даже для крошечного музея на старте

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

        int maxVisitors = MuseumSystem.Instance?.CalculateMaxVisitors() ?? 0;

        // === НОВОЕ: спавн тикает ВСЕГДА, независимо от заполненности ===
        // Если музей полон, посетитель всё равно появится на дорожке, просто
        // развернётся и уйдёт на другой конец без входа (см. SpawnNewVisitor).
        // Интервал привязан к текущей вместимости музея: Visitor.BaseVisitDuration / maxVisitors
        // даёт ровно тот темп, с которым реально освобождаются места при фиксированном
        // времени визита - так рост лимита посетителей напрямую даёт больше входов в минуту.
        float desiredInterval = Visitor.BaseVisitDuration / Mathf.Max(maxVisitors, 1);
        float spawnInterval = Mathf.Clamp(desiredInterval, MinSpawnInterval, MaxSpawnInterval);

        _spawnTimer += delta;
        if (_spawnTimer >= spawnInterval)
        {
            _spawnTimer = 0f;
            SpawnNewVisitor(maxVisitors);
        }
    }

        /// <summary>
    /// Возвращает количество посетителей, которые реально находятся в музее
    /// (т.е. были допущены внутрь, а не развернулись от входа).
    /// </summary>
    public int GetActiveVisitorsCount()
    {
        _visitors.RemoveAll(v => !GodotObject.IsInstanceValid(v));
        return _visitors.Count(v => v.IsAdmitted);
    }

     private void SpawnNewVisitor(int maxVisitors)
    {
        if (_layout == null) return;

        var mainHall = _layout.GetRoom("main_hall");
        if (mainHall == null) return;

        // Получаем концы дорожки
        var (leftEnd, rightEnd) = MuseumBackground.GetWayEndPoints();

        // Случайно выбираем один из концов для спавна, другой - для разворота
        bool spawnAtLeft = GD.Randf() < 0.5f;
        Vector2I spawnPos = spawnAtLeft ? leftEnd : rightEnd;
        Vector2I oppositeEnd = spawnAtLeft ? rightEnd : leftEnd;

        // Проверяем, что позиция проходимая
        if (spawnPos.X < 0 || spawnPos.X >= MuseumLayout.GridWidth ||
            spawnPos.Y < 0 || spawnPos.Y >= MuseumLayout.GridHeight ||
            (_layout.Grid[spawnPos.X, spawnPos.Y] != TileType.Floor &&
             _layout.Grid[spawnPos.X, spawnPos.Y] != TileType.Door))
        {
            spawnPos = FindNearestWalkable(spawnPos);
        }

        int admittedCount = GetActiveVisitorsCount();
        bool hasRoom = admittedCount < maxVisitors;

        var visitor = new Visitor();

        if (hasRoom)
        {
            visitor.Initialize(_layout, spawnPos, mainHall.Id);
            visitor.AddToSceneTree();
            _visitors.Add(visitor);

            MuseumSystem.Instance?.OnVisitorEntered();
            CheckAndResetTicketDrop();

            GD.Print($"[VisitorManager] 🚶 Новый посетитель вошёл на ({spawnPos.X},{spawnPos.Y})! Всего: {admittedCount + 1}/{maxVisitors}");
        }
        else
        {
            // Музей полон: посетитель спавнится на дорожке, но сразу разворачивается
            // к другому её концу и уходит, не заходя внутрь и не принося денег.
            visitor.InitializeTurnedAway(_layout, spawnPos, oppositeEnd);
            visitor.AddToSceneTree();
            _visitors.Add(visitor);

            GD.Print($"[VisitorManager] 🚫 Музей полон ({admittedCount}/{maxVisitors}), посетитель прошёл мимо.");
        }
    }

    /// <summary>
    /// Спавнит посетителей сразу ВНУТРИ музея (на случайных проходимых клетках
    /// открытых комнат), а не у входа на дорожке. Нужно для догонки: когда
    /// игрок возвращается с раскопок, доход за посетителей уже начислен одним
    /// куском (MuseumSystem.CalculateVisitorCatchUpIncome) — но все старые
    /// Visitor-узлы были уничтожены вместе со сценой музея при переходе на
    /// раскопки, и без этого музей выглядел бы пустым, пока новые посетители
    /// не дойдут от входа своим ходом.
    /// </summary>
    public void SpawnCatchUpVisitors(int count)
    {
        if (!_isInitialized)
        {
            Initialize();
            if (!_isInitialized) return;
        }
        if (count <= 0) return;

        var floorCells = new List<Vector2I>();
        for (int x = 0; x < MuseumLayout.GridWidth; x++)
        {
            for (int y = 0; y < MuseumLayout.GridHeight; y++)
            {
                if (_layout.Grid[x, y] != TileType.Floor) continue;

                var cell = new Vector2I(x, y);
                if (_layout.IsGlobalCellOccupiedByFurniture(cell)) continue;

                int roomIdx = _layout.RoomIndex[x, y];
                if (roomIdx < 0 || roomIdx >= _layout.Rooms.Count) continue;
                if (!_layout.Rooms[roomIdx].IsUnlocked) continue;

                floorCells.Add(cell);
            }
        }

        if (floorCells.Count == 0) return;

        _visitors.RemoveAll(v => !GodotObject.IsInstanceValid(v));

        int admittedCount = GetActiveVisitorsCount();
        int maxVisitors = MuseumSystem.Instance?.CalculateMaxVisitors() ?? 0;
        int toSpawn = Mathf.Min(count, Mathf.Max(0, maxVisitors - admittedCount));

        for (int i = 0; i < toSpawn; i++)
        {
            var cell = floorCells[GD.RandRange(0, floorCells.Count - 1)];
            int roomIdx = _layout.RoomIndex[cell.X, cell.Y];
            string roomId = _layout.Rooms[roomIdx].Id;

            var visitor = new Visitor();
            visitor.Initialize(_layout, cell, roomId);
            visitor.AddToSceneTree();
            _visitors.Add(visitor);
        }

        if (toSpawn > 0)
            GD.Print($"[VisitorManager] 🚶 Догонка: сразу внутри музея появилось {toSpawn} посетителей.");
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