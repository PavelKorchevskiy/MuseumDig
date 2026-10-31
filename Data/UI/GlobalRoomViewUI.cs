using Godot;
using System;
using System.Collections.Generic;

public partial class GlobalRoomViewUI : Node2D
{
    private float GridOffsetX => MuseumConstants.GridOffsetX;
    private float GridOffsetY => MuseumConstants.GridOffsetY;

       // Путь к спрайту арки (128x128)
    private const string EntranceArchPath = "res://assets/museum/doors/main.png";
    private const string MainHallRoomId = "main_hall";
    
        // === Низкая стена для передних граней (юг и восток) ===
    private const string LowWallPath = "res://assets/museum/wall/cuted.png";
    private const float LowWallAdjustY = 42f;  // Базовое смещение как у высоких стен
    
    // Ручная настройка положения (X: влево/вправо, Y: вверх/вниз)
    private static readonly Vector2 LowWallOffset = new Vector2(0f, 0f);
    
    // Настройка отражения (поменяйте, если грань отрисована зеркально)
    private const bool FlipSouthLowWall = false;  // грань y = oy + Height
    private const bool FlipEastLowWall = true;    // грань x = ox + Width

    private static readonly Vector2 EntranceOffset = new Vector2(0f, -32f);

    private bool _isInitialized = false;

    private Dictionary<string, Node2D> _roomContainers = new Dictionary<string, Node2D>();


    public override void _Ready()
    {
         if (_isInitialized)
        {
            GD.Print("[GlobalRoomView] ⚠️ _Ready() вызван повторно, пропускаем инициализацию");
            return;
        }
        
        _isInitialized = true;
        // MuseumLayout теперь синглтон, он уже создан в MuseumSystem или создастся при первом обращении
        RenderAllRooms();
    }

    public void RenderAllRooms()
    {
        // // 1. Удаляем старые спрайты мебели и экспонатов (прямые дети GlobalRoomViewUI)
        // var toRemove = new List<Node>();
        // foreach (var child in GetChildren())
        // {
        //     // Пропускаем контейнеры комнат, чтобы не удалить их
        //     if (child is Node2D container && _roomContainers.ContainsValue(container))
        //         continue;
            
        //     // Помечаем на удаление все TextureRect (это старая мебель и экспонаты)
        //     if (child is TextureRect)
        //     {
        //         toRemove.Add(child);
        //     }
        // }
        // foreach (var node in toRemove)
        // {
        //     node.QueueFree();
        // }

        // 2. Очищаем контейнеры комнат
        foreach (var container in _roomContainers.Values)
            container.QueueFree();
        _roomContainers.Clear();

        // 3. Создаём и рендерим комнаты
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            if (!room.IsUnlocked) continue;

            var container = new Node2D { Name = room.Id };
            AddChild(container);
            _roomContainers[room.Id] = container;

            // RenderRoom сам отрисует пол, стены и двери внутри этого контейнера
            RenderRoom(room, container);
        }

        // 4. Рендерим мебель поверх всего
        RenderAllFurniture();
    }

     private void RenderRoom(Room room, Node2D container)
    {
        RenderFloor(room, container);
        RenderWalls(room, container);
        RenderLowWalls(room, container);
        RenderDoorFloors(room, container); 
    }

        /// <summary>
    /// ЛЁГКОЕ обновление: только меняет затемнение существующих контейнеров.
    /// НЕ пересоздаёт спрайты. Вызывается при смене активной комнаты.
    /// </summary>
    public void UpdateRoomDarknessOnly()
    {
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            if (_roomContainers.TryGetValue(room.Id, out var container))
            {
                float darkness = GetRoomDarkness(room.Id);
                container.Modulate = new Color(darkness, darkness, darkness, 1.0f);
            }
        }
    }


    private void RenderFloor(Room room, Node2D container)
    {
        int ox = room.GlobalOffset.X;
        int oy = room.GlobalOffset.Y;

        for (int x = ox; x < ox + room.Width; x++)
        {
            for (int y = oy; y < oy + room.Height; y++)
            {
                var tile = MuseumLayout.Instance.Grid[x, y];
                
                // Рисуем пол ТАМ ЖЕ, где и проходы (TileType.Door)
                if (tile == TileType.Floor || tile == TileType.Door)
                {
                    var isoPos = IsoUtils.GridToIso(x, y);
                    string texturePath = ((x + y) % 2 == 0)
                        ? "res://assets/museum/floor/1.png"
                        : "res://assets/museum/floor/2.png";

                    if (!ResourceLoader.Exists(texturePath)) continue;

                    var floorTexture = GD.Load<Texture2D>(texturePath);
                    var tileRect = new TextureRect();
                    tileRect.Position = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y);
                    tileRect.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
                    tileRect.Texture = floorTexture;
                    tileRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
                    tileRect.MouseFilter = Control.MouseFilterEnum.Stop;
                    tileRect.ZIndex = IsoUtils.GetZOrder(x, y);

                    tileRect.GuiInput += (@event) =>
                    {
                        if (@event is InputEventMouseButton mouseEvent &&
                            mouseEvent.Pressed &&
                            mouseEvent.ButtonIndex == MouseButton.Left)
                        {
                            OnRoomClicked(room.Id);
                        }
                    };

                    container.AddChild(tileRect);
                }
            }
        }
    }

    private void RenderWalls(Room room, Node2D container) // БЫЛО: RoomConfig
    {
        string wallTexturePath = "res://assets/museum/wall/wall.png";
        if (!ResourceLoader.Exists(wallTexturePath)) return;

        var wallTexture = GD.Load<Texture2D>(wallTexturePath);
        float wallWidth = wallTexture.GetWidth();
        float wallHeight = wallTexture.GetHeight();
        float wallAdjustY = 42f;

        int ox = room.GlobalOffset.X;
        int oy = room.GlobalOffset.Y;

        // === ВЕРХНЯЯ СТЕНА ===
        for (int x = ox - 1; x <= ox + room.Width; x++)
        {
            // ПРОВЕРКА НАПРОСТУЮ ПО СЕТКЕ: если тут проход, стену не рисуем
            if (MuseumLayout.Instance.Grid[x, oy - 1] == TileType.Door) continue;

            var isoPos = IsoUtils.GridToIso(x, oy - 1);
            var wallRect = new TextureRect();
            wallRect.Position = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y - wallHeight + wallAdjustY);
            wallRect.Size = new Vector2(wallWidth, wallHeight);
            wallRect.Texture = wallTexture;
            wallRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            wallRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            wallRect.FlipH = false;
            wallRect.ZIndex = IsoUtils.GetZOrder(x, oy - 1) + 5;
            container.AddChild(wallRect);
        }

        // === ЛЕВАЯ СТЕНА ===
        for (int y = oy - 1; y <= oy + room.Height; y++)
        {
            // ПРОВЕРКА НАПРОСТУЮ ПО СЕТКЕ: если тут проход, стену не рисуем
            if (MuseumLayout.Instance.Grid[ox - 1, y] == TileType.Door) continue;

            var isoPos = IsoUtils.GridToIso(ox - 1, y);
            var wallRect = new TextureRect();
            wallRect.Position = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y - wallHeight + wallAdjustY);
            wallRect.Size = new Vector2(wallWidth, wallHeight);
            wallRect.Texture = wallTexture;
            wallRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            wallRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            wallRect.FlipH = true;
            wallRect.ZIndex = IsoUtils.GetZOrder(ox - 1, y) + 5;
            container.AddChild(wallRect);
        }
    }

       private void RenderDoorFloors(Room room, Node2D container)
    {
        int ox = room.GlobalOffset.X;
        int oy = room.GlobalOffset.Y;

        // === НОВОЕ: Для main_hall рисуем арку один раз ===
        if (room.Id == MainHallRoomId)
        {
            DrawMainEntrance(room, container);
        }

        // Обычная отрисовка полов для всех дверей
        for (int x = ox - 1; x <= ox + room.Width; x++)
        {
            CheckAndDrawDoorFloor(x, oy - 1, container);
            CheckAndDrawDoorFloor(x, oy + room.Height, container);
        }
        for (int y = oy - 1; y <= oy + room.Height; y++)
        {
            CheckAndDrawDoorFloor(ox - 1, y, container);
            CheckAndDrawDoorFloor(ox + room.Width, y, container);
        }
    }

     private void CheckAndDrawDoorFloor(int x, int y, Node2D container)
    {
        if (x < 0 || x >= MuseumLayout.GridWidth || y < 0 || y >= MuseumLayout.GridHeight)
            return;
            
        if (MuseumLayout.Instance.Grid[x, y] != TileType.Door)
            return;

        // Обычный пол для всех дверей (арка рисуется отдельно)
        var isoPos = IsoUtils.GridToIso(x, y);
        string texturePath = ((x + y) % 2 == 0)
            ? "res://assets/museum/floor/1.png"
            : "res://assets/museum/floor/2.png";

        if (ResourceLoader.Exists(texturePath))
        {
            var floorTexture = GD.Load<Texture2D>(texturePath);
            var tileRect = new TextureRect();
            tileRect.Position = new Vector2(GridOffsetX + isoPos.X, GridOffsetY + isoPos.Y);
            tileRect.Size = new Vector2(IsoUtils.TileWidth, IsoUtils.TileHeight);
            tileRect.Texture = floorTexture;
            tileRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
            tileRect.MouseFilter = Control.MouseFilterEnum.Ignore;
            tileRect.ZIndex = IsoUtils.GetZOrder(x, y);
            
            container.AddChild(tileRect);
        }
    }
        /// <summary>
    /// Отрисовывает арку главного входа (один спрайт 128x128).
    /// </summary>
    private void DrawMainEntrance(Room room, Node2D container)
    {
        if (!ResourceLoader.Exists(EntranceArchPath))
        {
            GD.PrintErr($"[GlobalRoomView] Арка не найдена: {EntranceArchPath}");
            return;
        }

        int ox = room.GlobalOffset.X;
        int oy = room.GlobalOffset.Y;

        // Находим все двери main_hall
        var doorTiles = new List<Vector2I>();
        
        for (int x = ox - 1; x <= ox + room.Width; x++)
        {
            if (IsDoorAt(x, oy - 1)) doorTiles.Add(new Vector2I(x, oy - 1));
            if (IsDoorAt(x, oy + room.Height)) doorTiles.Add(new Vector2I(x, oy + room.Height));
        }
        for (int y = oy - 1; y <= oy + room.Height; y++)
        {
            if (IsDoorAt(ox - 1, y)) doorTiles.Add(new Vector2I(ox - 1, y));
            if (IsDoorAt(ox + room.Width, y)) doorTiles.Add(new Vector2I(ox + room.Width, y));
        }

        if (doorTiles.Count == 0)
        {
            GD.PrintErr("[GlobalRoomView] У main_hall нет дверей!");
            return;
        }

        // Находим центр всех дверей (средняя точка)
        float sumX = 0, sumY = 0;
        foreach (var tile in doorTiles)
        {
            sumX += tile.X;
            sumY += tile.Y;
        }
        float centerX = sumX / doorTiles.Count;
        float centerY = sumY / doorTiles.Count;

        // Конвертируем в изометрические координаты
        var isoPos = IsoUtils.GridToIso((int)centerX, (int)centerY);

        // Загружаем текстуру арки
        var archTexture = GD.Load<Texture2D>(EntranceArchPath);
        
        // Создаём спрайт арки
        float archSize = 256f;
        var archRect = new TextureRect();
        archRect.Texture = archTexture;
        archRect.Size = new Vector2(archSize, archSize); // Фиксированный размер
        archRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        archRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        // Позиционируем: центр арки в центре входа + смещение
        archRect.Position = new Vector2(
            GridOffsetX + isoPos.X + EntranceOffset.X - 280,
            GridOffsetY + isoPos.Y + EntranceOffset.Y - 45 
        );
        
        // Z-индекс: поверх пола, но под стенами
        archRect.ZIndex = IsoUtils.GetZOrder((int)centerX, (int)centerY) + 10;
        
        container.AddChild(archRect);
        
        GD.Print($"[GlobalRoomView] Арка входа размещена в центре ({centerX:F1},{centerY:F1}) с offset={EntranceOffset}");
    }

    /// <summary>
    /// Проверяет, является ли клетка дверью (проходом).
    /// </summary>
    private bool IsDoorAt(int x, int y)
    {
        if (x < 0 || x >= MuseumLayout.GridWidth || y < 0 || y >= MuseumLayout.GridHeight)
            return false;
        return MuseumLayout.Instance.Grid[x, y] == TileType.Door;
    }

        /// <summary>
    /// Возвращает коэффициент затемнения для комнаты (0.0 = черная, 1.0 = полная яркость)
    /// </summary>
    private float GetRoomDarkness(string roomId)
    {
        if (roomId == MuseumLayout.Instance.ActiveRoomId) return 1.0f; // Активная комната — полная яркость
        
        var activeRoom = MuseumLayout.Instance.GetRoom(MuseumLayout.Instance.ActiveRoomId);
        if (activeRoom != null && activeRoom.AdjacentRoomIds.Contains(roomId))
        {
            return 0.6f; // Соседняя комната — 60% яркости (темная, но видимая)
        }

        return 0.35f; // Далекая комната — 35% яркости (очень темная)
    }

        private void OnRoomClicked(string roomId)
    {
        var room = MuseumLayout.Instance.GetRoom(roomId);
        if (room != null && !room.IsUnlocked)
        {
            return;
        }

        MuseumLayout.Instance.ActiveRoomId = roomId;
        UpdateRoomVisibility();

        // === НАДЕЖНЫЙ ПОИСК УЗЛА Museum ===
        // Способ 1: Приведение типа (если Museum - это корень сцены)
        Museum museum = GetTree().CurrentScene as Museum;
        
        // Способ 2: Если не сработало, ищем узел по типу среди детей
        if (museum == null)
        {
            foreach (var child in GetTree().CurrentScene.GetChildren())
            {
                if (child is Museum m)
                {
                    museum = m;
                    break;
                }
            }
        }

            
            // === НАДЕЖНЫЙ ПОИСК КАМЕРЫ ===
            // Ищем сначала по имени "Camera2D"
            var camera = museum.GetNodeOrNull<CameraController>("Camera2D");
            
            // Если не нашли по имени, ищем первый дочерний узел нужного типа
            if (camera == null)
            {
                foreach (var child in museum.GetChildren())
                {
                    if (child is CameraController cc)
                    {
                        camera = cc;
                        break;
                    }
                }
            }

            if (camera != null)
            {
                Vector2I center = MuseumLayout.Instance.GetRoomGlobalCenter(roomId);
                camera.MoveToRoomCenter(center);
            }
            else
            {
                GD.PrintErr("❌ [GlobalRoomView] CameraController не найден в узле Museum!");
                
            }
        
    }

        public void UpdateRoomVisibility()
    {
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            if (_roomContainers.TryGetValue(room.Id, out var container))
            {
                float darkness = GetRoomDarkness(room.Id);
                container.Modulate = new Color(darkness, darkness, darkness, 1.0f);
            }
        }
    }

    public void UpdateActiveRoomByCameraCenter(Vector2 cameraCenter)
    {
        string closestRoom = null;
        float minDistance = float.MaxValue;
        
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            if (!room.IsUnlocked) continue;

            Vector2I roomCenter = MuseumLayout.Instance.GetRoomGlobalCenter(room.Id);
            Vector2 roomCenterScreen = new Vector2(
                GridOffsetX + IsoUtils.GridToIso(roomCenter.X, roomCenter.Y).X,
                GridOffsetY + IsoUtils.GridToIso(roomCenter.X, roomCenter.Y).Y
            );
            
            float distance = cameraCenter.DistanceTo(roomCenterScreen);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestRoom = room.Id;
            }
        }
        
        if (closestRoom != null && closestRoom != MuseumLayout.Instance.ActiveRoomId)
        {
            GD.Print($"📷 Камера переместилась, активная комната: {closestRoom}");
            MuseumLayout.Instance.ActiveRoomId = closestRoom;
            UpdateRoomVisibility();
        }
    }

       private void RenderAllFurniture()
    {
        foreach (var room in MuseumLayout.Instance.Rooms)
        {
            if (!room.IsUnlocked) continue;
            
            // Получаем контейнер этой комнаты
            if (!_roomContainers.TryGetValue(room.Id, out var container)) continue;
            
            foreach (var placed in room.PlacedFurnitureList)
            {
                // === ИЗМЕНЕНО: передаём container ===
                RenderFurnitureItem(room, placed, container);
            }
        }
    }

    private void RenderFurnitureItem(Room room, PlacedFurniture placed, Node2D container)
    {
        
        int globalX = room.GlobalOffset.X + placed.Position.X;
        int globalY = room.GlobalOffset.Y + placed.Position.Y;

        var isoPos = IsoUtils.GridToIso(globalX, globalY);
        
        // === 1. ДИНАМИЧЕСКОЕ ОПРЕДЕЛЕНИЕ ПУТИ К ТЕКСТУРЕ ===
        string texturePath;
        if (placed.Furniture is CollectionExhibit)
        {
            // Соглашение: папка с именем коллекции содержит full.png
            texturePath = $"res://assets/museum/items/{placed.FurnitureTypeId}/full.png";
        }
        else
        {
            // Для обычных витрин и другой мебели
            texturePath = GetFurnitureTexturePath(placed.FurnitureTypeId);
        }

        if (!ResourceLoader.Exists(texturePath))
        {
            GD.PrintErr($"[GlobalRoomView] Текстура не найдена: {texturePath}. Проверьте папку assets/museum/items/{placed.FurnitureTypeId}/");
            texturePath = "res://icon.svg";
        }

        var texture = GD.Load<Texture2D>(texturePath);
        var sprite = new TextureRect();
        sprite.Texture = texture;
        sprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        sprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        sprite.MouseFilter = Control.MouseFilterEnum.Ignore;

        float realWidth = texture.GetWidth();
        float realHeight = texture.GetHeight();
        sprite.Size = new Vector2(realWidth, realHeight);

                // === 2. ПОЗИЦИОНИРОВАНИЕ ===
        float centerX;
        float centerY;

        if (placed.Size.X > 1 || placed.Size.Y > 1)
        {
            // Для больших объектов: используем НИЖНИЙ ЦЕНТР footprint'а
            // В изометрии "земля" — это нижний ряд плиток, а не геометрический центр
            int anchorGridX = globalX + placed.Size.X - 1;
            int anchorGridY = globalY + placed.Size.Y - 1;
            
            var anchorIsoPos = IsoUtils.GridToIso(anchorGridX, anchorGridY);
            
            centerX = GridOffsetX + anchorIsoPos.X + IsoUtils.TileWidth / 2f;
            centerY = GridOffsetY + anchorIsoPos.Y + IsoUtils.TileHeight;
        }
        else
        {
            // Для объектов 1x1 (витрины): нижний центр клетки
            centerX = GridOffsetX + isoPos.X + IsoUtils.TileWidth / 2f;
            centerY = GridOffsetY + isoPos.Y + IsoUtils.TileHeight;
        }

        sprite.Position = new Vector2(
            centerX - realWidth / 2f,
            centerY - realHeight
        );

                // === Z-индекс на основе ЦЕНТРА footprint'а ===
        // Это гарантирует правильную сортировку: посетители ниже перекрывают скелет,
        // а посетители выше (позади) остаются позади
        int zAnchorX = globalX + placed.Size.X / 2;
        int zAnchorY = globalY + placed.Size.Y / 2;
        sprite.ZIndex = IsoUtils.GetZOrder(zAnchorX, zAnchorY) + (placed.Size.X + placed.Size.Y) / 2;
        
    
        var museum = GetTree().CurrentScene as Museum;

        // Делаем витрину кликабельной
        if (placed.Furniture is DisplayCase)
        {
            sprite.MouseFilter = Control.MouseFilterEnum.Stop;
            sprite.GuiInput += (@event) =>
            {
                if (@event is InputEventMouseButton mouseEvent && 
                    mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (room.Id != MuseumLayout.Instance.ActiveRoomId)
                    {
                        MuseumLayout.Instance.ActiveRoomId = room.Id;
                        UpdateRoomVisibility();
                        
                        // Перемещаем камеру к центру новой комнаты
                        var camera = museum?.GetNodeOrNull<CameraController>("Camera2D");
                        if (camera != null)
                        {
                            Vector2I center = MuseumLayout.Instance.GetRoomGlobalCenter(room.Id);
                            camera.MoveToRoomCenter(center);
                        }
                    }
                    
                    museum?.OpenDisplayCaseUI(room, placed);
                }
            };

            
                        // === Рисуем экспонаты внутри витрины ===
            if (placed.Items != null && placed.Items.Count > 0)
            {
                bool isLargeCase = (placed.Size.X == 2 && placed.Size.Y == 2);

                for (int i = 0; i < placed.Items.Count; i++)
                {
                    var exhibit = placed.Items[i];
                    var res = GameData.GetResource(exhibit.ResourceId);
                    if (res != null)
                    {
                        // === НОВОЕ: Определяем путь к иконке экспоната ===
                        string exhibitPath = null;
                        
                        // Проверяем, есть ли у ресурса варианты
                        if (res is FossilDefinition fossil && fossil.VariantCount > 1)
                        {
                            // Пробуем загрузить первый вариант из папки
                            string variantPath = $"res://assets/museum/items/common/{exhibit.ResourceId}/1.png";
                            if (ResourceLoader.Exists(variantPath))
                            {
                                exhibitPath = variantPath;
                            }
                        }
                        
                        // Fallback на обычный путь
                        if (exhibitPath == null)
                        {
                            string commonPath = $"res://assets/museum/items/common/{exhibit.ResourceId}.png";
                            if (ResourceLoader.Exists(commonPath))
                            {
                                exhibitPath = commonPath;
                            }
                        }
                        
                        if (exhibitPath != null)
                        {
                            var exhibitTexture = GD.Load<Texture2D>(exhibitPath);
                            var exhibitSprite = new TextureRect();
                            exhibitSprite.Texture = exhibitTexture;
                            exhibitSprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                            exhibitSprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
                            exhibitSprite.MouseFilter = Control.MouseFilterEnum.Ignore;
                            
                            // === НОВОЕ: Автоматическое масштабирование ===
                            // Большие спрайты (256x256) масштабируем до размера витрины
                            float targetSize = 24f; // Целевой размер для маленьких витрин
                            float scale = 0.9f; // Базовый масштаб
                            
                            if (exhibitTexture.GetWidth() > 64 || exhibitTexture.GetHeight() > 64)
                            {
                                // Для больших спрайтов вычисляем масштаб автоматически
                                float scaleX = targetSize / exhibitTexture.GetWidth();
                                float scaleY = targetSize / exhibitTexture.GetHeight();
                                scale = Math.Min(scaleX, scaleY) * 0.9f;
                            }
                            
                            exhibitSprite.Size = new Vector2(
                                exhibitTexture.GetWidth() * scale, 
                                exhibitTexture.GetHeight() * scale
                            );
                            
                            // Базовая позиция: центр витрины со смещением вверх на 22px
                            float basePosX = sprite.Position.X + (realWidth - exhibitSprite.Size.X) / 2f;
                            float basePosY = sprite.Position.Y + (realHeight - exhibitSprite.Size.Y) / 2f - 22f;

                            // Смещения для больших витрин с несколькими экспонатами
                            if (isLargeCase && placed.Items.Count > 1)
                            {
                                if (i == 0)
                                {
                                    basePosX -= 18f; 
                                    basePosY -= 12f; 
                                }
                                else if (i == 1)
                                {
                                    basePosX += 18f; 
                                    basePosY += 12f; 
                                }
                            }

                            exhibitSprite.Position = new Vector2(basePosX, basePosY);
                            exhibitSprite.ZIndex = sprite.ZIndex + 1 + i; 
                            exhibitSprite.Modulate = sprite.Modulate;
                            
                            container.AddChild(exhibitSprite);
                        }
                        else
                        {
                            GD.PrintErr($"[GlobalRoomView] Иконка экспоната не найдена: {exhibit.ResourceId}");
                        }
                    }
                }
            }
        }
        if (placed.Furniture is CollectionExhibit)
        {
            sprite.MouseFilter = Control.MouseFilterEnum.Stop;
            sprite.GuiInput += (@event) =>
            {
                if (@event is InputEventMouseButton mouseEvent && 
                    mouseEvent.Pressed && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (room.Id != MuseumLayout.Instance.ActiveRoomId)
                    {
                        MuseumLayout.Instance.ActiveRoomId = room.Id;
                        UpdateRoomVisibility();
                        
                        // Перемещаем камеру к центру новой комнаты
                        var camera = museum?.GetNodeOrNull<CameraController>("Camera2D");
                        if (camera != null)
                        {
                            Vector2I center = MuseumLayout.Instance.GetRoomGlobalCenter(room.Id);
                            camera.MoveToRoomCenter(center);
                        }
                    }
                    museum?.OpenCollectionExhibitUI(room, placed);
                }
            };
        }
GD.Print($"[RenderFurnitureItem] Добавляем {placed.FurnitureTypeId} в комнату {room.Id}, container={container?.Name ?? "NULL"}");
        container.AddChild(sprite);
    }

    private string GetFurnitureTexturePath(string typeId)
    {
        if (typeId == "display_case_1x1") return "res://assets/museum/furniture/display_case_small.png";
        if (typeId == "display_case_2x2") return "res://assets/museum/furniture/display_case_large.png";
        return $"res://assets/museum/furniture/{typeId}.png";
    }

     /// <summary>
    /// Рисует низкие стены вдоль южной и восточной граней комнаты —
    /// там, где пол заканчивается и нет стен.
    /// </summary>
    private void RenderLowWalls(Room room, Node2D container)
    {
        if (!ResourceLoader.Exists(LowWallPath))
        {
            GD.PrintErr($"[GlobalRoomView] Низкая стена не найдена: {LowWallPath}");
            return;
        }

        var lowWallTexture = GD.Load<Texture2D>(LowWallPath);
        float wallWidth = lowWallTexture.GetWidth();
        float wallHeight = lowWallTexture.GetHeight();

        int ox = room.GlobalOffset.X;
        int oy = room.GlobalOffset.Y;

        // === ЮЖНАЯ грань (y = oy + Height) ===
        int southY = oy + room.Height;
        if (southY >= 0 && southY < MuseumLayout.GridHeight)
        {
            for (int x = ox - 1; x <= ox + room.Width; x++)
            {
                if (x < 0 || x >= MuseumLayout.GridWidth) continue;
                
                // Не рисуем в проходах (дверях)
                if (MuseumLayout.Instance.Grid[x, southY] == TileType.Door) continue;
                
                // Не рисуем, если соседняя открытая комната рисует здесь высокую стену
                if (IsTallWallDrawnByNeighbor(x, southY)) continue;

                AddLowWallTile(x, southY, FlipSouthLowWall, lowWallTexture, wallWidth, wallHeight, container);
            }
        }

        // === ВОСТОЧНАЯ грань (x = ox + Width) ===
        int eastX = ox + room.Width;
        if (eastX >= 0 && eastX < MuseumLayout.GridWidth)
        {
            for (int y = oy - 1; y <= oy + room.Height; y++)
            {
                if (y < 0 || y >= MuseumLayout.GridHeight) continue;
                
                if (MuseumLayout.Instance.Grid[eastX, y] == TileType.Door) continue;
                if (IsTallWallDrawnByNeighbor(eastX, y)) continue;

                AddLowWallTile(eastX, y, FlipEastLowWall, lowWallTexture, wallWidth, wallHeight, container);
            }
        }
    }

    private void AddLowWallTile(int x, int y, bool flipH, Texture2D texture, float w, float h, Node2D container)
    {
        var isoPos = IsoUtils.GridToIso(x, y);
        var wallRect = new TextureRect();
        wallRect.Position = new Vector2(
            GridOffsetX + isoPos.X + LowWallOffset.X,
            GridOffsetY + isoPos.Y - h + LowWallAdjustY + LowWallOffset.Y
        );
        wallRect.Size = new Vector2(w, h);
        wallRect.Texture = texture;
        wallRect.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
        wallRect.MouseFilter = Control.MouseFilterEnum.Ignore;
        wallRect.FlipH = flipH;
        wallRect.ZIndex = IsoUtils.GetZOrder(x, y) + 5;
        container.AddChild(wallRect);
    }

    /// <summary>
    /// Проверяет, рисует ли соседняя ОТКРЫТАЯ комнату высокую стену в этой клетке.
    /// Если да — низкая стена здесь не нужна (иначе они наложатся друг на друга).
    /// </summary>
    private bool IsTallWallDrawnByNeighbor(int cx, int cy)
    {
        foreach (var other in MuseumLayout.Instance.Rooms)
        {
            if (!other.IsUnlocked) continue;

            int oox = other.GlobalOffset.X;
            int ooy = other.GlobalOffset.Y;

            // Сосед рисует ВЕРХНЮЮ стену вдоль ряда ooy - 1
            if (cy == ooy - 1 && cx >= oox - 1 && cx <= oox + other.Width) return true;

            // Сосед рисует ЛЕВУЮ стену вдоль колонки oox - 1
            if (cx == oox - 1 && cy >= ooy - 1 && cy <= ooy + other.Height) return true;
        }
        return false;
    }
}