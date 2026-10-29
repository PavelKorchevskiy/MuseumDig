using Godot;
using System;
using System.Collections.Generic;

public partial class MuseumBackground : Node2D
{
    private const string GrassPath = "res://assets/museum/background/grass/";
    private const string WayPath = "res://assets/museum/background/way/";
    private const int GrassVariantCount = 5;
    private const int WayVariantCount = 8;
    
    private const int BorderThickness = 20;

    // === Настройки дорожки ===
    private const int WayLengthFromEntrance = 4;
    private const int VerticalWayWidth = 2;     // Теперь 2 тайла
    private const int HorizontalWayWidth = 2;

    private float GridOffsetX => MuseumConstants.GridOffsetX;
    private float GridOffsetY => MuseumConstants.GridOffsetY;

     private static HashSet<Vector2I> _wayTiles = new HashSet<Vector2I>();

    public void Setup(Museum museum)
    {
        ZIndex = -100;

        Vector2I entrance = FindEntrancePosition();
        GenerateWay(entrance);
        GenerateGrass();
    }

    private Vector2I FindEntrancePosition()
    {
        var mainHall = MuseumLayout.Instance.GetRoom("main_hall");
        if (mainHall == null) return new Vector2I(0, 0);

        int ox = mainHall.GlobalOffset.X;
        int oy = mainHall.GlobalOffset.Y;
        int southY = oy + mainHall.Height;

        var southDoors = new List<int>();
        for (int x = ox - 1; x <= ox + mainHall.Width; x++)
        {
            if (x >= 0 && x < MuseumLayout.GridWidth &&
                southY >= 0 && southY < MuseumLayout.GridHeight &&
                MuseumLayout.Instance.Grid[x, southY] == TileType.Door)
            {
                southDoors.Add(x);
            }
        }

        if (southDoors.Count == 0)
            return new Vector2I(ox + mainHall.Width / 2, southY);

        int centerX = (southDoors[0] + southDoors[southDoors.Count - 1]) / 2;
        return new Vector2I(centerX, southY);
    }

    private (bool ok, int minX, int maxX, int minY, int maxY) GetMuseumBounds()
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            int ox = room.GlobalOffset.X;
            int oy = room.GlobalOffset.Y;

            minX = Math.Min(minX, ox);
            maxX = Math.Max(maxX, ox + room.Width - 1);
            minY = Math.Min(minY, oy);
            maxY = Math.Max(maxY, oy + room.Height - 1);
        }

        if (minX == int.MaxValue)
            return (false, 0, 0, 0, 0);

        return (true, minX, maxX, minY, maxY);
    }

    private void GenerateWay(Vector2I entrance)
    {
        _wayTiles.Clear();

        var bounds = GetMuseumBounds();
        if (!bounds.ok) return;

        int ex = entrance.X;
        int ey = entrance.Y;

        // === Вертикальная часть от входа: ширина 2 тайла, ниже и правее ===
        for (int dy = 0; dy < WayLengthFromEntrance; dy++)
        {
            for (int w = 0; w < VerticalWayWidth; w++)
            {
                _wayTiles.Add(new Vector2I(ex + w, ey + dy));
            }
        }

        // === Параллельная музею дорога: на всю длину травы ===
        // Длина = границы музея + по 20 тайлов травы слева и справа
        int branchY = ey + WayLengthFromEntrance;

        int branchStartX = bounds.minX - BorderThickness;
        int branchEndX = bounds.maxX + BorderThickness;

        for (int x = branchStartX; x <= branchEndX; x++)
        {
            for (int w = 0; w < HorizontalWayWidth; w++)
            {
                _wayTiles.Add(new Vector2I(x, branchY + w));
            }
        }

        GD.Print($"[MuseumBackground] Вход: ({ex},{ey}), тайлов дорожки: {_wayTiles.Count}");
    }

    private void GenerateGrass()
    {
        var bounds = GetMuseumBounds();
        if (!bounds.ok) return;

        int minX = bounds.minX - BorderThickness;
        int maxX = bounds.maxX + BorderThickness;
        int minY = bounds.minY - BorderThickness;
        int maxY = bounds.maxY + BorderThickness;

        // Сначала трава
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (IsInsideAnyRoom(x, y)) continue;
                if (_wayTiles.Contains(new Vector2I(x, y))) continue;

                AddTile(x, y, GrassPath, GrassVariantCount);
            }
        }

        // Потом дорожка
        foreach (var cell in _wayTiles)
        {
            if (IsInsideAnyRoom(cell.X, cell.Y)) continue;
            AddTile(cell.X, cell.Y, WayPath, WayVariantCount);
        }

        GD.Print($"[MuseumBackground] Создано тайлов фона: {GetChildCount()}");
    }

    private bool IsInsideAnyRoom(int x, int y)
    {
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            int ox = room.GlobalOffset.X;
            int oy = room.GlobalOffset.Y;

            if (x >= ox && x < ox + room.Width &&
                y >= oy && y < oy + room.Height)
            {
                return true;
            }
        }
        return false;
    }

    private void AddTile(int gx, int gy, string basePath, int variantCount)
    {
        int variant = (HashCoordinates(gx, gy, 200) % variantCount) + 1;
        string path = $"{basePath}{variant}.png";

        if (!ResourceLoader.Exists(path))
        {
            path = $"{basePath}1.png";
            if (!ResourceLoader.Exists(path)) return;
        }

        var sprite = new TextureRect();
        sprite.Texture = GD.Load<Texture2D>(path);
        sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        sprite.MouseFilter = Control.MouseFilterEnum.Ignore;

        var isoPos = IsoUtils.GridToIso(gx, gy);

        sprite.Position = new Vector2(
            GridOffsetX + isoPos.X,
            GridOffsetY + isoPos.Y
        );
        sprite.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
        sprite.ZIndex = -100 + (gx + gy);

        AddChild(sprite);
    }

    private static int HashCoordinates(int x, int y, int salt)
    {
        long hash = x * 73856093L ^ y * 19349663L ^ salt * 83492791L;
        return Mathf.Abs((int)(hash & 0x7FFFFFFF));
    }

    
        /// <summary>
    /// Возвращает две точки концов параллельной дорожки (левый и правый).
    /// </summary>
    public static (Vector2I leftEnd, Vector2I rightEnd) GetWayEndPoints()
    {
        int minX = int.MaxValue, maxX = int.MinValue;
        int entranceY = 0;

        var mainHall = MuseumLayout.Instance.GetRoom("main_hall");
        if (mainHall != null)
        {
            entranceY = mainHall.GlobalOffset.Y + mainHall.Height + WayLengthFromEntrance;
        }

        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            int ox = room.GlobalOffset.X;
            minX = Math.Min(minX, ox);
            maxX = Math.Max(maxX, ox + room.Width - 1);
        }

        if (minX == int.MaxValue)
            return (new Vector2I(0, 0), new Vector2I(0, 0));

        int leftEndX = minX - BorderThickness;
        int rightEndX = maxX + BorderThickness;

        return (
            new Vector2I(leftEndX, entranceY),
            new Vector2I(rightEndX, entranceY)
        );
    }

    public static bool IsWayTile(int x, int y)
    {
        return _wayTiles.Contains(new Vector2I(x, y));
    }
}