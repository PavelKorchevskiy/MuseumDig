using Godot;
using System.Collections.Generic;

public enum TileType { Empty, Floor, Wall, Door }

public class MuseumLayout
{
    private static MuseumLayout _instance;
    
    public static MuseumLayout Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MuseumLayout();
                _instance.Initialize();
            }
            return _instance;
        }
    }

    // Сетка: X растёт вправо, Y — вниз (вход в музей внизу). Схема (вид сверху):
    //
    //   +----------7-----------+--------8---------+
    //   |        26 x 15       |      31 x 15     |
    //   +----4-----+-----5-----+---6----+----9----+
    //   |  15x15   |   10x20   | 15x15  |  15x31  |
    //   +----2-----+           +---3----+         |
    //   |  15x15   +-----1-----+ 15x15  |         |
    //   |          |   10x10   |        |         |
    //   +----------+--[вход]---+--------+---------+
    //
    //   1 main_hall, 2 left_wing, 3 right_wing, 4 back_left, 5 back_center,
    //   6 back_right, 7 upper_left, 8 upper_right, 9 east_wing.
    //   Комнаты 7-9 закрыты и покупаются за монеты (Room.UnlockCost).
    public const int GridWidth = 60;
    public const int GridHeight = 49;

    public TileType[,] Grid { get; private set; }
    public int[,] RoomIndex { get; private set; }
    public List<Room> Rooms { get; private set; }

    private string _activeRoomId = "main_hall";

    /// <summary>
    /// Проход в стене между двумя комнатами. Появляется только когда открыты
    /// обе комнаты: до покупки комнаты стена на его месте сплошная.
    /// RoomB == null — проход наружу (главный вход).
    /// </summary>
    private class Doorway
    {
        public string RoomA;
        public string RoomB;
        public Vector2I[] Cells;
    }

    private readonly List<Doorway> _doorways = new();

    // Приватный конструктор
    public MuseumLayout() { }

    private void Initialize()
    {
        Grid = new TileType[GridWidth, GridHeight];
        RoomIndex = new int[GridWidth, GridHeight];
        Rooms = new List<Room>();
        CreateRooms();
        RebuildGrid();
        GD.Print("[MuseumLayout] ✅ Инициализирован как ленивый синглтон.");
    }

    private void CreateRooms()
    {
        // === Комнаты (id, левый верхний угол пола, ширина, высота) ===
        var upperLeft = AddRoom("upper_left", new Vector2I(1, 1), 26, 15, false, unlockCost: 200000);    // 7
        var upperRight = AddRoom("upper_right", new Vector2I(28, 1), 31, 15, false, unlockCost: 300000); // 8
        var backLeft = AddRoom("back_left", new Vector2I(1, 17), 15, 15, false);                         // 4
        var backCenter = AddRoom("back_center", new Vector2I(17, 17), 10, 20, false);                    // 5
        var backRight = AddRoom("back_right", new Vector2I(28, 17), 15, 15, false);                      // 6
        var eastWing = AddRoom("east_wing", new Vector2I(44, 17), 15, 31, false, unlockCost: 400000);    // 9
        var leftWing = AddRoom("left_wing", new Vector2I(1, 33), 15, 15, false);                         // 2
        var mainHall = AddRoom("main_hall", new Vector2I(17, 38), 10, 10, true);                         // 1
        var rightWing = AddRoom("right_wing", new Vector2I(28, 33), 15, 15, false);                      // 3

        // === Проходы (по 2 клетки в общей стене) ===
        AddDoorway("main_hall", null, (21, 48), (22, 48));            // главный вход
        AddDoorway("main_hall", "left_wing", (16, 42), (16, 43));
        AddDoorway("main_hall", "right_wing", (27, 42), (27, 43));
        AddDoorway("main_hall", "back_center", (21, 37), (22, 37));
        AddDoorway("back_center", "back_left", (16, 24), (16, 25));
        AddDoorway("back_center", "back_right", (27, 24), (27, 25));
        AddDoorway("left_wing", "back_left", (8, 32), (9, 32));
        AddDoorway("right_wing", "back_right", (35, 32), (36, 32));
        AddDoorway("back_left", "upper_left", (8, 16), (9, 16));      // 4 <-> 7
        AddDoorway("back_center", "upper_left", (21, 16), (22, 16));  // 5 <-> 7
        AddDoorway("back_right", "upper_right", (35, 16), (36, 16));  // 6 <-> 8
        AddDoorway("back_right", "east_wing", (43, 24), (43, 25));    // 6 <-> 9
        AddDoorway("right_wing", "east_wing", (43, 40), (43, 41));    // 3 <-> 9
        AddDoorway("upper_right", "east_wing", (51, 16), (52, 16));   // 8 <-> 9

        // === Смежность для освещения: комнаты, между которыми есть проход ===
        foreach (var door in _doorways)
        {
            if (door.RoomB == null) continue;
            var a = GetRoom(door.RoomA);
            var b = GetRoom(door.RoomB);
            if (!a.AdjacentRoomIds.Contains(b.Id)) a.AdjacentRoomIds.Add(b.Id);
            if (!b.AdjacentRoomIds.Contains(a.Id)) b.AdjacentRoomIds.Add(a.Id);
        }
    }

    private void AddDoorway(string roomA, string roomB, params (int x, int y)[] cells)
    {
        var door = new Doorway { RoomA = roomA, RoomB = roomB, Cells = new Vector2I[cells.Length] };
        for (int i = 0; i < cells.Length; i++)
        {
            door.Cells[i] = new Vector2I(cells[i].x, cells[i].y);
        }
        _doorways.Add(door);
    }

    /// <summary>
    /// Заново заполняет сетку (пол, стены, проходы) по текущему состоянию комнат.
    /// Вызывать после изменения Room.IsUnlocked: покупки комнаты или загрузки
    /// сохранения. Массивы Grid/RoomIndex не пересоздаются — на них держат
    /// ссылки посетители и менеджер посетителей.
    ///
    /// Закрытые комнаты тоже есть в сетке (пол и стены): они рисуются
    /// затемнёнными. Попасть в них нельзя, потому что проходов к ним нет.
    /// </summary>
    public void RebuildGrid()
    {
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                Grid[x, y] = TileType.Empty;
                RoomIndex[x, y] = -1;
            }

        // Пол и стены
        for (int i = 0; i < Rooms.Count; i++)
        {
            var room = Rooms[i];
            int ox = room.GlobalOffset.X;
            int oy = room.GlobalOffset.Y;

            for (int x = ox - 1; x <= ox + room.Width; x++)
            {
                for (int y = oy - 1; y <= oy + room.Height; y++)
                {
                    if (x >= 0 && x < GridWidth && y >= 0 && y < GridHeight)
                    {
                        bool isInterior = (x >= ox && x < ox + room.Width && y >= oy && y < oy + room.Height);
                        if (Grid[x, y] == TileType.Empty)
                        {
                            Grid[x, y] = isInterior ? TileType.Floor : TileType.Wall;
                            RoomIndex[x, y] = i;
                        }
                    }
                }
            }
        }

        // Проходы — только между двумя открытыми комнатами
        foreach (var door in _doorways)
        {
            var a = GetRoom(door.RoomA);
            var b = door.RoomB != null ? GetRoom(door.RoomB) : null;

            if (a == null || !a.IsUnlocked) continue;
            if (door.RoomB != null && (b == null || !b.IsUnlocked)) continue;

            foreach (var cell in door.Cells)
            {
                Grid[cell.X, cell.Y] = TileType.Door;
            }
        }
    }

    private Room AddRoom(string id, Vector2I offset, int w, int h, bool isMain, int unlockCost = 0)
    {
        var room = new Room
        {
            Id = id,
            DisplayName = id,
            GlobalOffset = offset,
            Width = w,
            Height = h,
            IsMainHall = isMain,
            UnlockCost = unlockCost,
            IsUnlocked = unlockCost <= 0 // платные комнаты закрыты, пока их не купят
        };
        room.InitializeGrid();
        Rooms.Add(room);
        return room;
    }

    public string ActiveRoomId { get => _activeRoomId; set => _activeRoomId = value; }

    public float GetRoomAlpha(string roomId)
    {
        if (roomId == _activeRoomId) return 1.0f;
        var activeRoom = GetRoom(_activeRoomId);
        if (activeRoom != null && activeRoom.AdjacentRoomIds.Contains(roomId)) return 0.55f;
        return 0.35f;
    }

    public Vector2I GetRoomGlobalCenter(string roomId)
    {
        var room = GetRoom(roomId);
        return room != null ? new Vector2I(room.GlobalOffset.X + room.Width / 2, room.GlobalOffset.Y + room.Height / 2) : Vector2I.Zero;
    }

    public Vector2I GetStreetExitPosition()
    {
        // Первая клетка главного входа (проход без второй комнаты)
        foreach (var door in _doorways)
        {
            if (door.RoomB == null) return door.Cells[0];
        }
        return Vector2I.Zero;
    }

    public Room GetRoom(string roomId) => Rooms.Find(r => r.Id == roomId);

        /// <summary>
    /// Проверяет, занята ли указанная глобальная клетка мебелью в ЛЮБОЙ комнате
    /// </summary>
    public bool IsGlobalCellOccupiedByFurniture(Vector2I globalPos)
    {
        foreach (var room in Rooms)
        {
            if (!room.IsUnlocked) continue;
            
            if (room.IsCellOccupiedByFurniture(globalPos))
            {
                return true;
            }
        }
        return false;
    }
}