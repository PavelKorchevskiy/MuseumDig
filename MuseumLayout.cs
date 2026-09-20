using Godot;
using System.Collections.Generic;

public enum TileType { Empty, Floor, Wall, Door }

public class MuseumLayout
{
    public static MuseumLayout Instance { get; private set; }

    public const int GridWidth = 44;
    public const int GridHeight = 33;

    public TileType[,] Grid { get; private set; }
    public int[,] RoomIndex { get; private set; }
    public List<Room> Rooms { get; private set; }

    private string _activeRoomId = "main_hall";

    public MuseumLayout()
    {
        Instance = this;
        Grid = new TileType[GridWidth, GridHeight];
        RoomIndex = new int[GridWidth, GridHeight];
        Rooms = new List<Room>();
        BuildLayout();
    }

    private void BuildLayout()
    {
        // 1. Очистка
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                Grid[x, y] = TileType.Empty;
                RoomIndex[x, y] = -1;
            }

        // 2. Создание комнат
        var backLeft = AddRoom("back_left", new Vector2I(1, 1), 15, 15, false);
        var backCenter = AddRoom("back_center", new Vector2I(17, 1), 10, 20, false);
        var backRight = AddRoom("back_right", new Vector2I(28, 1), 15, 15, false);
        var leftWing = AddRoom("left_wing", new Vector2I(1, 17), 15, 15, false);
        var mainHall = AddRoom("main_hall", new Vector2I(17, 22), 10, 10, true);
        var rightWing = AddRoom("right_wing", new Vector2I(28, 17), 15, 15, false);

        // 3. Настройка смежности для освещения
        mainHall.AdjacentRoomIds.AddRange(new[] { "left_wing", "right_wing", "back_center" });
        leftWing.AdjacentRoomIds.AddRange(new[] { "main_hall", "back_left" });
        rightWing.AdjacentRoomIds.AddRange(new[] { "main_hall", "back_right" });
        backCenter.AdjacentRoomIds.AddRange(new[] { "main_hall", "back_left", "back_right" });
        backLeft.AdjacentRoomIds.Add("back_center");
        backRight.AdjacentRoomIds.Add("back_center");

        // 4. Заполнение сетки (Пол и Стены)
        for (int i = 0; i < Rooms.Count; i++)
        {
            var room = Rooms[i];
            if (!room.IsUnlocked) continue; // ЗАКРЫТЫЕ КОМНАТЫ НЕ РИСУЮТСЯ В СЕТКЕ

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

        // 5. Проходы (просто меняем тип тайла)
        Grid[21, 32] = TileType.Door; Grid[22, 32] = TileType.Door; // Главный вход
        Grid[16, 26] = TileType.Door; Grid[16, 27] = TileType.Door; // main <-> left
        Grid[27, 26] = TileType.Door; Grid[27, 27] = TileType.Door; // main <-> right
        Grid[21, 21] = TileType.Door; Grid[22, 21] = TileType.Door; // main <-> back
        Grid[16, 8] = TileType.Door; Grid[16, 9] = TileType.Door;   // back_center <-> back_left
        Grid[27, 8] = TileType.Door; Grid[27, 9] = TileType.Door;   // back_center <-> back_right
        Grid[8, 16] = TileType.Door; Grid[9, 16] = TileType.Door;   // left <-> back_left
        Grid[35, 16] = TileType.Door; Grid[36, 16] = TileType.Door; // right <-> back_right
    }

    private Room AddRoom(string id, Vector2I offset, int w, int h, bool isMain)
    {
        var room = new Room
        {
            Id = id,
            DisplayName = id == "main_hall" ? "Главный зал" : $"Зал {id}",
            GlobalOffset = offset,
            Width = w,
            Height = h,
            IsMainHall = isMain
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
        // В BuildLayout мы задали главный вход на (21, 32) и (22, 32)
        // Возвращаем одну из этих клеток
        return new Vector2I(21, 32);
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