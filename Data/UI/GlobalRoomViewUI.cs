using Godot;
using System.Collections.Generic;

public partial class GlobalRoomViewUI : Node2D
{
    private float GridOffsetX => MuseumConstants.GridOffsetX;
    private float GridOffsetY => MuseumConstants.GridOffsetY;

    private Dictionary<string, Node2D> _roomContainers = new Dictionary<string, Node2D>();

    public override void _Ready()
    {
        // MuseumLayout теперь синглтон, он уже создан в MuseumSystem или создастся при первом обращении
        RenderAllRooms();
    }

    public void RenderAllRooms()
    {
        // 1. Удаляем старые спрайты мебели и экспонатов (прямые дети GlobalRoomViewUI)
        var toRemove = new List<Node>();
        foreach (var child in GetChildren())
        {
            // Пропускаем контейнеры комнат, чтобы не удалить их
            if (child is Node2D container && _roomContainers.ContainsValue(container))
                continue;
            
            // Помечаем на удаление все TextureRect (это старая мебель и экспонаты)
            if (child is TextureRect)
            {
                toRemove.Add(child);
            }
        }
        foreach (var node in toRemove)
        {
            node.QueueFree();
        }

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

            // Применяем затемнение вместо прозрачности
            float darkness = GetRoomDarkness(room.Id);
            container.Modulate = new Color(darkness, darkness, darkness, 1.0f);

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
        
        // Вызываем метод с 2 аргументами (комната и её контейнер)
        RenderDoorFloors(room, container); 
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

        // Проверяем верхнюю и нижнюю границы комнаты
        for (int x = ox - 1; x <= ox + room.Width; x++)
        {
            CheckAndDrawDoorFloor(x, oy - 1, container);          // Верх
            CheckAndDrawDoorFloor(x, oy + room.Height, container); // Низ
        }

        // Проверяем левую и правую границы комнаты
        for (int y = oy - 1; y <= oy + room.Height; y++)
        {
            CheckAndDrawDoorFloor(ox - 1, y, container);          // Лево
            CheckAndDrawDoorFloor(ox + room.Width, y, container); // Право
        }
    }

 private void CheckAndDrawDoorFloor(int x, int y, Node2D container)
    {
        // Проверяем, что координаты в пределах глобальной сетки
        if (x >= 0 && x < MuseumLayout.GridWidth && y >= 0 && y < MuseumLayout.GridHeight)
        {
            // Если в этой клетке находится проход (дверь)
            if (MuseumLayout.Instance.Grid[x, y] == TileType.Door)
            {
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
                    
                    // ВАЖНО: Добавляем тайл в container комнаты, чтобы он наследовал затемнение (Modulate)!
                    container.AddChild(tileRect);
                }
            }
        }
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
            foreach (var placed in room.PlacedFurnitureList)
            {
                RenderFurnitureItem(room, placed);
            }
        }
    }

       private void RenderFurnitureItem(Room room, PlacedFurniture placed)
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
        
        float darkness = GetRoomDarkness(room.Id);
        sprite.Modulate = new Color(darkness, darkness, darkness, 1.0f);
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

            // Рисуем экспонат внутри витрины
                        // === Рисуем экспонаты внутри витрины ===
            if (placed.Items != null && placed.Items.Count > 0)
            {
                // Проверяем, является ли витрина большой
                bool isLargeCase = (placed.Size.X == 2 && placed.Size.Y == 2);

                for (int i = 0; i < placed.Items.Count; i++)
                {
                    var exhibit = placed.Items[i];
                    var res = GameData.GetResource(exhibit.ResourceId);
                    if (res != null)
                    {
                        // Все спрайты экспонатов лежат в папке common
                        string exhibitPath = $"res://assets/museum/items/common/{exhibit.ResourceId}.png";
                        
                        if (ResourceLoader.Exists(exhibitPath))
                        {
                            var exhibitTexture = GD.Load<Texture2D>(exhibitPath);
                            var exhibitSprite = new TextureRect();
                            exhibitSprite.Texture = exhibitTexture;
                            exhibitSprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                            exhibitSprite.StretchMode = TextureRect.StretchModeEnum.KeepAspect;
                            exhibitSprite.MouseFilter = Control.MouseFilterEnum.Ignore;
                            
                            // Ваш настроенный масштаб
                            float exhibitScale = 0.9f;
                            exhibitSprite.Size = new Vector2(exhibitTexture.GetWidth() * exhibitScale, exhibitTexture.GetHeight() * exhibitScale);
                            
                            // Базовая позиция: центр витрины со смещением вверх на 22px (как вы настроили)
                            float basePosX = sprite.Position.X + (realWidth - exhibitSprite.Size.X) / 2f;
                            float basePosY = sprite.Position.Y + (realHeight - exhibitSprite.Size.Y) / 2f - 22f;

                            // Если витрина большая и в ней 2 предмета, применяем смещения
                            if (isLargeCase && placed.Items.Count > 1)
                            {
                                if (i == 0)
                                {
                                    // Первый экспонат: выше и левее
                                    basePosX -= 18f; 
                                    basePosY -= 12f; 
                                }
                                else if (i == 1)
                                {
                                    // Второй экспонат: ниже и правее
                                    basePosX += 18f; 
                                    basePosY += 12f; 
                                }
                            }

                            exhibitSprite.Position = new Vector2(basePosX, basePosY);
                            
                            // Z-индекс: чуть выше витрины. +i гарантирует, что второй предмет будет рисоваться поверх первого при пересечении
                            exhibitSprite.ZIndex = sprite.ZIndex + 1 + i; 
                            exhibitSprite.Modulate = sprite.Modulate; // Наследует затемнение комнаты
                            
                            AddChild(exhibitSprite);
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

        AddChild(sprite);
    }

    private string GetFurnitureTexturePath(string typeId)
    {
        if (typeId == "display_case_1x1") return "res://assets/museum/furniture/display_case_small.png";
        if (typeId == "display_case_2x2") return "res://assets/museum/furniture/display_case_large.png";
        return $"res://assets/museum/furniture/{typeId}.png";
    }
}