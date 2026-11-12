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

        // === НОВОЕ: Деревья поверх травы и дорожки ===
        AddTrees();
    }

        // === Структура для размещения дерева ===
    private struct TreePlacement
    {
        public int GridX;       // X координата основания (в клетках сетки)
        public int GridY;       // Y координата основания (в клетках сетки)
        public Vector2 Offset;  // Дополнительное смещение в пикселях (для точной настройки)
        public int Variant;     // Вариант дерева: 1, 2 или 3 (0 = случайный)
    }

    // === МАССИВ ДЕРЕВЬЕВ — РЕДАКТИРУЙТЕ ЗДЕСЬ ===
    // Количество = длина массива. Чтобы добавить дерево — добавьте новую строку.
    // Чтобы убрать — удалите строку.
    private static readonly TreePlacement[] TreePlacements = new TreePlacement[]
    {
        // Левая сторона (западнее музея)
        new TreePlacement { GridX = -10, GridY = 36, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = -8,  GridY = 41, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = -5,  GridY = 48, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = -12, GridY = 54, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = -7,  GridY = 22, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = -11, GridY = 8,  Offset = Vector2.Zero, Variant = 0 },

        // Правая сторона (восточнее музея)
        new TreePlacement { GridX = 64,  GridY = 12, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 67,  GridY = 26, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 63,  GridY = 38, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 66,  GridY = 46, Offset = Vector2.Zero, Variant = 0 },

        // Нижняя часть (южнее развилки)
        new TreePlacement { GridX = 10,  GridY = 61, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 25,  GridY = 60, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 20,  GridY = 66, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 33,  GridY = 64, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 45,  GridY = 63, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 60,  GridY = 59, Offset = Vector2.Zero, Variant = 0 },
        new TreePlacement { GridX = 53,  GridY = 67, Offset = Vector2.Zero, Variant = 0 },

    };

    // Путь к папке с деревьями
    private const string TreePath = "res://assets/museum/background/decor/tree";
    private const int TreeVariantCount = 3;

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

        // Сначала собираем список клеток травы (без создания узлов)
        var grassCells = new List<Vector2I>();
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (IsInsideAnyRoom(x, y)) continue;
                if (_wayTiles.Contains(new Vector2I(x, y))) continue;

                grassCells.Add(new Vector2I(x, y));
            }
        }
        RenderGroundLayer(grassCells, GrassPath, GrassVariantCount);

        // Потом дорожка
        var wayCells = new List<Vector2I>();
        foreach (var cell in _wayTiles)
        {
            if (IsInsideAnyRoom(cell.X, cell.Y)) continue;
            wayCells.Add(cell);
        }
        RenderGroundLayer(wayCells, WayPath, WayVariantCount);

        GD.Print($"[MuseumBackground] Трава: {grassCells.Count} клеток, дорожка: {wayCells.Count} клеток. Узлов фона создано: {GetChildCount()}");
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

    /// <summary>
    /// Рисует набор клеток. Группировка по текстуре оставлена только чтобы
    /// не грузить один и тот же файл (GD.Load) по многу раз подряд -
    /// сам узел по-прежнему создаётся на каждую клетку (см. CreateSpriteTiles),
    /// но это Sprite2D, а не Control/TextureRect: без раскладки интерфейса,
    /// обработки ввода и темы - именно это было причиной тормозов, а не
    /// количество узлов само по себе. Одинаковые соседние Sprite2D с общей
    /// текстурой и материалом Godot и так объединяет в один draw call
    /// (2D canvas batching), так что по производительности это всё ещё
    /// на порядки легче, чем тысячи TextureRect.
    /// </summary>
    private void RenderGroundLayer(List<Vector2I> cells, string basePath, int variantCount)
    {
        var groups = new Dictionary<string, List<Vector2I>>();

        foreach (var cell in cells)
        {
            string path = ResolveVariantPath(cell.X, cell.Y, basePath, variantCount);
            if (path == null) continue;

            if (!groups.TryGetValue(path, out var list))
            {
                list = new List<Vector2I>();
                groups[path] = list;
            }
            list.Add(cell);
        }

        foreach (var kvp in groups)
        {
            CreateSpriteTiles(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Выбирает файл текстуры для клетки — та же детерминированная логика,
    /// что была в старом AddTile: один и тот же хеш координат всегда даёт
    /// один и тот же вариант, поэтому расположение травы не меняется между
    /// перезапусками и не "мерцает" при перестройке сцены.
    /// </summary>
    private string ResolveVariantPath(int gx, int gy, string basePath, int variantCount)
    {
        int variant = (HashCoordinates(gx, gy, 200) % variantCount) + 1;
        string path = $"{basePath}{variant}.png";

        if (!ResourceLoader.Exists(path))
        {
            path = $"{basePath}1.png";
            if (!ResourceLoader.Exists(path)) return null;
        }
        return path;
    }

    /// <summary>
    /// Создаёт по одному Sprite2D на клетку этой группы (все клетки здесь уже
    /// используют одну и ту же текстуру-вариант). Из текстуры берётся только
    /// верхняя ромбовидная "крышка" тайла, которая в натуральную величину равна
    /// клетке 64×32 - ровно как у плиток пола, поэтому сетки травы, дорожки и
    /// пола совпадают. Якорь - верхний левый угол клетки (Centered = false).
    /// От MultiMeshInstance2D/QuadMesh отказался: это 3D-примитив, у его UV
    /// в 2D-контексте возможен вертикальный флип, который не проверить без
    /// запуска движка - а Sprite2D ведёт себя предсказуемо и доказанно работает
    /// (тот же узел уже используется для пола).
    /// </summary>
    private void CreateSpriteTiles(string texturePath, List<Vector2I> cells)
    {
        if (cells.Count == 0) return;

        var texture = GD.Load<Texture2D>(texturePath);

        // === Берём только "крышку" тайла, без боковых стенок ===
        // Тайлы нарисованы объёмными: сверху зелёная ромбовидная крышка, под ней
        // коричневые боковые стенки (земля). Файл при этом квадратный (64×64) и
        // вокруг рисунка есть прозрачные поля (у травы по 11px сверху и снизу).
        //
        // Если рисовать весь рисунок целиком, коричневые стенки лезут в соседние
        // клетки и выглядят как коричневые зазоры между травой. Поэтому:
        //   1) находим прямоугольник непрозрачных пикселей (GetUsedRect);
        //   2) отрезаем от него сверху ровно крышку - изометрический ромб,
        //      у которого высота вдвое меньше ширины (64×32 для этих текстур).
        // Соседние тайлы при правильном порядке отрисовки (ZIndex ниже) полностью
        // накрывают стенки друг друга, поэтому коричневое остаётся только по
        // внешнему краю газона, где соседей нет.
        Rect2 region = new Rect2(0, 0, texture.GetWidth(), texture.GetHeight());

        var image = texture.GetImage();
        if (image != null)
        {
            if (image.IsCompressed() && image.Decompress() != Error.Ok)
            {
                image = null;
            }

            if (image != null)
            {
                var used = image.GetUsedRect();
                if (used.Size.X > 0 && used.Size.Y > 0)
                {
                    // Высота крышки = половина её ширины (пропорция изометрии 2:1)
                    float capHeight = Mathf.Min(used.Size.X / 2f, used.Size.Y);
                    region = new Rect2(used.Position.X, used.Position.Y, used.Size.X, capHeight);
                }
            }
        }

        // Крышка по размеру уже равна клетке (64×32), так что масштаб выходит 1:1.
        // Формула оставлена общей на случай текстур другого разрешения.
        Vector2 scale = new Vector2(
            IsoUtils.TileWidth / region.Size.X,
            IsoUtils.TileHeight / region.Size.Y
        );

        foreach (var cell in cells)
        {
            var isoPos = IsoUtils.GridToIso(cell.X, cell.Y);
            Vector2 topLeft = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y);

            var sprite = new Sprite2D();
            sprite.Texture = texture;
            sprite.RegionEnabled = true;   // рисуем только непрозрачную часть файла
            sprite.RegionRect = region;
            sprite.Centered = false;       // Position = верхний левый угол клетки
            sprite.Position = topLeft;
            sprite.Scale = scale;

            // Как и раньше у пола (ZIndex = GetZOrder(x,y)), но со сдвигом -100,
            // чтобы фон гарантированно оставался позади пола/стен/мебели.
            sprite.ZIndex = -100 + IsoUtils.GetZOrder(cell.X, cell.Y);

            AddChild(sprite);

            AddCoordinateLabel(cell.X, cell.Y, topLeft);
        }
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

        /// <summary>
    /// Отрисовывает все деревья из массива TreePlacements.
    /// </summary>
    private void AddTrees()
    {
        for (int i = 0; i < TreePlacements.Length; i++)
        {
            var placement = TreePlacements[i];

            if (!IsValidTreePosition(placement.GridX, placement.GridY))
            {
                GD.PrintErr($"[MuseumBackground] ⚠️ Дерево #{i} на ({placement.GridX},{placement.GridY}) в запретной зоне (на дорожке, в комнате или между дорожкой и музеем)");
                continue;
            }

            AddTree(placement, i);
        }

        GD.Print($"[MuseumBackground] Создано деревьев: {TreePlacements.Length}");
    }

    /// <summary>
    /// Проверяет, можно ли разместить дерево в этой позиции.
    /// Запрещено: на дорожке, внутри комнаты, между дорожкой и музеем.
    /// </summary>
    private bool IsValidTreePosition(int x, int y)
    {
        // 1. На дорожке — нельзя
        if (IsWayTile(x, y)) return false;

        // 2. Внутри комнаты — нельзя
        if (IsInsideAnyRoom(x, y)) return false;

        // 3. Между дорожкой и музеем — нельзя
        // Это зона южнее южной стены музея, но севернее горизонтальной развилки
        var mainHall = MuseumLayout.Instance.GetRoom("main_hall");
        if (mainHall != null)
        {
            int museumSouth = mainHall.GlobalOffset.Y + mainHall.Height;
            int branchY = museumSouth + WayLengthFromEntrance;

            if (y > museumSouth && y < branchY)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Отрисовывает одно дерево.
    /// </summary>
    private void AddTree(TreePlacement placement, int index)
    {
                GD.Print($"[Tree #{index}] Base: ({placement.GridX},{placement.GridY}), IsoPos: {IsoUtils.GridToIso(placement.GridX, placement.GridY)}");

        // Выбираем вариант: указанный или случайный (детерминированный)
        int variant = placement.Variant;
        if (variant <= 0 || variant > TreeVariantCount)
        {
            variant = (HashCoordinates(placement.GridX, placement.GridY, 300 + index) % TreeVariantCount) + 1;
        }

        string path = $"{TreePath}{variant}.png";
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"[MuseumBackground] Дерево не найдено: {path}");
            return;
        }

        var texture = GD.Load<Texture2D>(path);
        var sprite = new TextureRect();
        sprite.Texture = texture;
        sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        sprite.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Размер = реальный размер текстуры
        float treeWidth = texture.GetWidth();
        float treeHeight = texture.GetHeight();
        sprite.Size = new Vector2(treeWidth, treeHeight);

        // === ПОЗИЦИОНИРОВАНИЕ ===
        // Основание дерева = центр клетки (isoPos)
        // Сам спрайт центрируется по горизонтали, нижний край совпадает с основанием
        var isoPos = IsoUtils.GridToIso(placement.GridX, placement.GridY);

        sprite.Position = new Vector2(
            GridOffsetX + isoPos.X + IsoUtils.TileWidth / 2f - treeWidth / 2f + placement.Offset.X,
            GridOffsetY + isoPos.Y + IsoUtils.TileHeight - treeHeight + placement.Offset.Y
        );

        // Z-индекс: по основанию дерева + смещение, чтобы ствол был виден
        sprite.ZIndex = IsoUtils.GetZOrder(placement.GridX, placement.GridY) + 10;

        AddChild(sprite);
    }

        /// <summary>
    /// Создаёт Label с координатами над тайлом (только в DevMode).
    /// </summary>
    private void AddCoordinateLabel(int gx, int gy, Vector2 tilePosition)
    {
        if (!DevMode.Instance.ShowGridCoords) return;

        var label = new Label();
        label.Text = $"{gx},{gy}";
        label.AddThemeFontSizeOverride("font_size", 10);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;

        // Чёрный контур для читаемости на любом фоне
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 2);

        // Позиция: над центром тайла
        label.Position = new Vector2(
            tilePosition.X + IsoUtils.TileWidth / 2f - 20f, // -20 = половина ширины Label
            tilePosition.Y - 5f
        );
        label.CustomMinimumSize = new Vector2(40, 15);

        // Z-индекс выше тайлов фона
        label.ZIndex = IsoUtils.GetZOrder(gx, gy) + 20;

        AddChild(label);
    }
}
