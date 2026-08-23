using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class InventoryUI : CanvasLayer
{

    public static InventoryUI _instance;

    public static event System.Action<string> OnStartPlacement;

    private enum Tab { Items, Collections }
    private Tab _currentTab = Tab.Items;

    private Label _totalValueLabel;
    private VBoxContainer _itemsList;
    private Button _sellAllButton;
    private Button _closeButton;

    // Новые кнопки для вкладок
    private Button _tabItemsButton;
    private Button _tabCollectionsButton;

    private Dictionary<string, Control> _rowCache = new();

    public static InventoryUI Instance
    {
        get
        {
            // Если ссылка пустая или объект уничтожен — ищем заново
            if (_instance == null || !GodotObject.IsInstanceValid(_instance))
            {
                // Ищем по АБСОЛЮТНОМУ пути в корне дерева сцены
                var sceneTree = Engine.GetMainLoop() as SceneTree;
                _instance = sceneTree?.Root.GetNodeOrNull<InventoryUI>("/root/InventoryUI");

                if (_instance == null)
                {
                    GD.PrintErr("[InventoryUI] КРИТИЧЕСКАЯ ОШИБКА: Autoload не найден в /root/InventoryUI!");
                }
            }
            return _instance;
        }
        private set
        {
            _instance = value;
        }
    }
    public override void _Ready()
    {
        Instance = this;
        this.Layer = 101;

        // Безопасное получение узлов
        _totalValueLabel = GetNodeOrNull<Label>("MainPanel/Content/TotalValueLabel");
        _itemsList = GetNodeOrNull<VBoxContainer>("MainPanel/Content/ScrollContainer/ItemsList");
        _sellAllButton = GetNodeOrNull<Button>("MainPanel/Content/ButtonsRow/SellAllButton");
        _closeButton = GetNodeOrNull<Button>("MainPanel/Content/ButtonsRow/CloseButton");

        if (_itemsList == null)
        {
            GD.PrintErr("[InventoryUI] КРИТИЧЕСКАЯ ОШИБКА: _itemsList не найден!");
            return;
        }

        // === БЕЗОПАСНОЕ СОЗДАНИЕ ВКЛАДОК ===
        var contentContainer = GetNode("MainPanel/Content") as VBoxContainer;
        if (contentContainer != null)
        {
            var tabsContainer = new HBoxContainer();
            tabsContainer.Name = "TabsContainer";
            tabsContainer.AddThemeConstantOverride("separation", 10);

            _tabItemsButton = new Button { Text = "Инвентарь", Name = "TabItems" };
            _tabItemsButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _tabItemsButton.Pressed += () => SwitchTab(Tab.Items);

            _tabCollectionsButton = new Button { Text = "Сбор коллекций", Name = "TabCollections" };
            _tabCollectionsButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            _tabCollectionsButton.Pressed += () => SwitchTab(Tab.Collections);

            tabsContainer.AddChild(_tabItemsButton);
            tabsContainer.AddChild(_tabCollectionsButton);

            // Добавляем в MainPanel/Content и перемещаем на позицию 1 (сразу после TotalValueLabel)
            contentContainer.AddChild(tabsContainer);
            contentContainer.MoveChild(tabsContainer, 1);
        }
        // ====================================

        _sellAllButton.Pressed += OnSellAllPressed;
        _closeButton.Pressed += OnClosePressed;

        _sellAllButton.Visible = (_currentTab == Tab.Items);
    }

    private void SwitchTab(Tab newTab)
    {
        _currentTab = newTab;
        _rowCache.Clear(); // Очищаем кэш при смене вкладки

        // Обновляем визуальное состояние кнопок
        _tabItemsButton.Disabled = (_currentTab == Tab.Items);
        _tabCollectionsButton.Disabled = (_currentTab == Tab.Collections);
        _sellAllButton.Visible = (_currentTab == Tab.Items);

        // Очищаем список
        foreach (var child in _itemsList.GetChildren())
        {
            child.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        if (InventorySystem.Instance == null) return;
        if (_itemsList == null) return; // Защита от null

        try
        {
            if (_currentTab == Tab.Items)
            {
                UpdateItemsDisplay();
            }
            else
            {
                UpdateCollectionsDisplay();
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[InventoryUI] Ошибка при отрисовке: {e.Message}");
            GD.PrintErr($"[InventoryUI] StackTrace: {e.StackTrace}");
        }
    }

    // ==========================================
    // ВКЛАДКА 1: ОБЫЧНЫЕ ПРЕДМЕТЫ (Ваш старый код)
    // ==========================================
    private void UpdateItemsDisplay()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        int totalValue = allItems.Sum(item => CalculateItemValue(item) * item.Amount);
        _totalValueLabel.Text = $"Общая стоимость: {totalValue} монет";

        var neededKeys = new HashSet<string>(allItems.Select(item => $"{item.ResourceId}_{(int)item.Quality}"));

        // Очистка кэша
        var toRemove = _rowCache.Keys.Where(k => !neededKeys.Contains(k)).ToList();
        foreach (var key in toRemove)
        {
            _rowCache[key].QueueFree();
            _rowCache.Remove(key);
        }

        foreach (var item in allItems)
        {
            string key = $"{item.ResourceId}_{(int)item.Quality}";
            
            if (!_rowCache.ContainsKey(key))
            {
                // === ВАЖНО: Вычисляем isCollection ПЕРЕД созданием строки ===
                bool isCollection = GameData.GetCollection(item.ResourceId) != null;
                
                // Передаём оба параметра
                var row = CreateItemRow(item, isCollection);
                _itemsList.AddChild(row);
                _rowCache[key] = row;
            }
            
            // === ВАЖНО: Вычисляем isCollection ПЕРЕД обновлением строки ===
            bool isCollectionForUpdate = GameData.GetCollection(item.ResourceId) != null;
            UpdateItemRow(_rowCache[key], item, isCollectionForUpdate);
        }
    }

    // ==========================================
    // ВКЛАДКА 2: СБОР КОЛЛЕКЦИЙ (Новая логика)
    // ==========================================
    private void UpdateCollectionsDisplay()
    {
        _totalValueLabel.Text = "Соберите полные коллекции для выставки";

        // GetAllCollections возвращает List<CollectionDefinition>
        var allCollections = GameData.GetAllCollections();

        var eligibleCollections = new List<CollectionDefinition>();

        // Перебираем напрямую коллекции, а не kvp
        foreach (var collection in allCollections)
        {
            // Проверяем, есть ли ХОТЯ БЫ ОДИН фрагмент от этой коллекции в инвентаре
            bool hasAnyPiece = collection.Pieces.Any(piece => InventorySystem.Instance.GetTotalAmount(piece.Id) > 0);

            if (hasAnyPiece)
            {
                eligibleCollections.Add(collection);
            }
        }

        // Сортировка: 1) Можно собрать (true сначала), 2) По алфавиту (DisplayName)
        var sortedCollections = eligibleCollections
            .OrderByDescending(c => InventorySystem.Instance.CanAssembleCollection(c))
            .ThenBy(c => c.DisplayName)
            .ToList();

        var neededKeys = new HashSet<string>(sortedCollections.Select(c => c.Id));

        // Очистка кэша (удаляем строки, которых больше нет в отфильтрованном списке)
        var toRemove = _rowCache.Keys.Where(k => !neededKeys.Contains(k)).ToList();
        foreach (var key in toRemove)
        {
            _rowCache[key].QueueFree();
            _rowCache.Remove(key);
        }

        // Создаем или обновляем строки
        foreach (var collection in sortedCollections)
        {
            if (!_rowCache.ContainsKey(collection.Id))
            {
                var row = CreateCollectionRow(collection);
                _itemsList.AddChild(row);
                _rowCache[collection.Id] = row;
            }
            UpdateCollectionRow(_rowCache[collection.Id], collection);
        }
    }

    private Control CreateCollectionRow(CollectionDefinition collection)
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 5);

        // 1. Название (прямой ребенок row)
        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        row.AddChild(nameLabel);

        // 2. Контейнер иконок (прямой ребенок row)
        var iconsRow = new HBoxContainer();
        iconsRow.Name = "IconsRow"; // ВАЖНО: задаем имя для поиска
        iconsRow.AddThemeConstantOverride("separation", 15);
        iconsRow.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddChild(iconsRow);

        // Иконка собранной коллекции
        var assembledIcon = new TextureRect();
    assembledIcon.Name = "AssembledIcon";
    assembledIcon.CustomMinimumSize = new Vector2(40, 40); // Жесткий минимум!
    assembledIcon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
    assembledIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
    iconsRow.AddChild(assembledIcon);

        // Стрелка <=
        var arrowLabel = new Label();
        arrowLabel.Name = "ArrowLabel";
        arrowLabel.Text = "<=";
        arrowLabel.AddThemeFontSizeOverride("font_size", 24);
        arrowLabel.VerticalAlignment = VerticalAlignment.Center;
        iconsRow.AddChild(arrowLabel);

        // Контейнер для частей
        var piecesContainer = new HBoxContainer();
        piecesContainer.Name = "PiecesContainer"; // ВАЖНО: задаем имя
        piecesContainer.AddThemeConstantOverride("separation", 10);
        iconsRow.AddChild(piecesContainer);

        // Создаем слоты для частей
        for (int i = 0; i < collection.Pieces.Count; i++)
        {
            var pieceSlot = new VBoxContainer();
            pieceSlot.Name = $"PieceSlot_{i}"; // ВАЖНО: задаем имя слоту
            pieceSlot.Alignment = BoxContainer.AlignmentMode.Center;

            var iconRect = new TextureRect();
        iconRect.Name = $"PieceIcon_{i}";
        iconRect.CustomMinimumSize = new Vector2(32, 32); // Жесткий минимум!
        iconRect.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        iconRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        pieceSlot.AddChild(iconRect);

            var countLabel = new Label();
            countLabel.Name = $"PieceCount_{i}";
            countLabel.AddThemeFontSizeOverride("font_size", 12);
            countLabel.HorizontalAlignment = HorizontalAlignment.Center;
            pieceSlot.AddChild(countLabel);

            piecesContainer.AddChild(pieceSlot);

            if (i < collection.Pieces.Count - 1)
            {
                var plus = new Label();
                plus.Text = "+";
                plus.AddThemeFontSizeOverride("font_size", 16);
                plus.VerticalAlignment = VerticalAlignment.Center;
                piecesContainer.AddChild(plus);
            }
        }

        // 3. Кнопка сборки (прямой ребенок row)
        var assembleBtn = new Button();
        assembleBtn.Name = "AssembleButton";
        assembleBtn.Text = "Собрать";
        assembleBtn.CustomMinimumSize = new Vector2(120, 0);

        // Подписываемся на нажатие ОДИН раз при создании
        assembleBtn.Pressed += () => OnAssemblePressed(collection);

        row.AddChild(assembleBtn);

        return row;
    }

    private void UpdateCollectionRow(Control row, CollectionDefinition collection)
    {
        // Название
        var nameLabel = row.GetNodeOrNull<Label>("NameLabel");
        if (nameLabel != null) nameLabel.Text = collection.DisplayName;

        // Стрелка
        var arrowLabel = row.GetNodeOrNull<Label>("IconsRow/ArrowLabel");
        bool canAssemble = InventorySystem.Instance.CanAssembleCollection(collection);
        if (arrowLabel != null)
        {
            arrowLabel.Modulate = canAssemble ? new Color(0.4f, 1f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
        }

        // === ИКОНКА СОБРАННОЙ КОЛЛЕКЦИИ (TextureRect) ===
        var assembledIcon = row.GetNodeOrNull<TextureRect>("IconsRow/AssembledIcon");
        if (assembledIcon != null)
        {
            string collectionIconPath = GetResourceIconPath(collection.Id);
            if (!string.IsNullOrEmpty(collectionIconPath) && ResourceLoader.Exists(collectionIconPath))
            {
                assembledIcon.Texture = GD.Load<Texture2D>(collectionIconPath);
                assembledIcon.Visible = true;
            }
            else
            {
                assembledIcon.Visible = false;
            }
        }

        // === ЧАСТИ КОЛЛЕКЦИИ (TextureRect вместо Label) ===
        for (int i = 0; i < collection.Pieces.Count; i++)
        {
            var piece = collection.Pieces[i];

            // Иконка части
            var iconRect = row.GetNodeOrNull<TextureRect>($"IconsRow/PiecesContainer/PieceSlot_{i}/PieceIcon_{i}");
            if (iconRect != null)
            {
                string partIconPath = GetResourceIconPath(piece.Id);
                if (!string.IsNullOrEmpty(partIconPath) && ResourceLoader.Exists(partIconPath))
                {
                    iconRect.Texture = GD.Load<Texture2D>(partIconPath);
                    iconRect.Visible = true;
                }
                else
                {
                    iconRect.Visible = false;
                }
            }

            // Количество
            var countLabel = row.GetNodeOrNull<Label>($"IconsRow/PiecesContainer/PieceSlot_{i}/PieceCount_{i}");
            int amount = InventorySystem.Instance.GetTotalAmount(piece.Id);

            if (countLabel != null)
            {
                if (amount > 0)
                {
                    countLabel.Text = $"x{amount}";
                    countLabel.Modulate = Colors.White;
                }
                else
                {
                    countLabel.Text = "?";
                    countLabel.Modulate = new Color(0.5f, 0.5f, 0.5f);
                }
            }
        }

        // Кнопка сборки
        var assembleBtn = row.GetNodeOrNull<Button>("AssembleButton");
        if (assembleBtn != null)
        {
            assembleBtn.Disabled = !canAssemble;
            assembleBtn.Modulate = canAssemble ? Colors.White : new Color(0.5f, 0.5f, 0.5f);
        }
    }

    private void OnAssemblePressed(CollectionDefinition collection)
    {
        if (InventorySystem.Instance.AssembleCollection(collection))
        {
            GD.Print($"[UI] Коллекция '{collection.DisplayName}' успешно собрана!");

            // Очищаем кэш и перерисовываем список, чтобы обновить количества фрагментов
            _rowCache.Clear();
            foreach (var child in _itemsList.GetChildren())
            {
                child.QueueFree();
            }
        }
        else
        {
            GD.PrintErr("[UI] Не удалось собрать коллекцию (не хватает фрагментов).");
        }
    }

    // ===== СОЗДАНИЕ СТРОКИ =====

        private Control CreateItemRow(FoundItem item, bool isCollection)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        
        // Сохраняем данные предмета в метаданных строки
        row.SetMeta("resource_id", item.ResourceId);
        row.SetMeta("quality", (int)item.Quality);
        row.SetMeta("is_collection", isCollection);

        // Иконка
        var iconRect = new TextureRect();
    iconRect.Name = "IconRect";
    iconRect.CustomMinimumSize = new Vector2(40, 40); // Жесткий минимум!
    iconRect.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
    iconRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
    iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        row.AddChild(iconRect);

        // Название
        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        row.AddChild(nameLabel);

        // Количество
        var amountLabel = new Label();
        amountLabel.Name = "AmountLabel";
        amountLabel.AddThemeFontSizeOverride("font_size", 12);
        amountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        row.AddChild(amountLabel);

        // Цена
        var priceLabel = new Label();
        priceLabel.Name = "PriceLabel";
        priceLabel.AddThemeFontSizeOverride("font_size", 12);
        priceLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        row.AddChild(priceLabel);

        if (isCollection)
        {
            // Кнопка "Разместить"
            var placeBtn = new Button();
            placeBtn.Name = "PlaceButton";
            placeBtn.Text = "📍 Разместить";
            placeBtn.CustomMinimumSize = new Vector2(120, 0);
            
            // === ПОДПИСЫВАЕМ ОДИН РАЗ, используя метаданные ===
            placeBtn.Pressed += () => {
                string resId = (string)row.GetMeta("resource_id");
                Quality q = (Quality)(int)row.GetMeta("quality");
                OnPlaceCollectionPressed(resId, q);
            };
            row.AddChild(placeBtn);
        }
        else
        {
            // Кнопка "Продать 1"
            var sellOneButton = new Button();
            sellOneButton.Name = "SellOneButton";
            sellOneButton.Text = "Продать 1";
            sellOneButton.CustomMinimumSize = new Vector2(90, 0);
            
            // === ПОДПИСЫВАЕМ ОДИН РАЗ ===
            sellOneButton.Pressed += () => {
                string resId = (string)row.GetMeta("resource_id");
                Quality q = (Quality)(int)row.GetMeta("quality");
                OnSellOnePressed(resId, q);
            };
            row.AddChild(sellOneButton);
            
            // Кнопка "Продать всё"
            var sellAllButton = new Button();
            sellAllButton.Name = "SellAllOfItemButton";
            sellAllButton.Text = "Продать всё";
            sellAllButton.CustomMinimumSize = new Vector2(100, 0);
            
            // === ПОДПИСЫВАЕМ ОДИН РАЗ ===
            sellAllButton.Pressed += () => {
                string resId = (string)row.GetMeta("resource_id");
                Quality q = (Quality)(int)row.GetMeta("quality");
                OnSellAllOfItemPressed(resId, q);
            };
            row.AddChild(sellAllButton);
        }
        
        return row;
    }

    // ===== ОБНОВЛЕНИЕ СТРОКИ =====

        private void UpdateItemRow(Control row, FoundItem item, bool isCollection)
    {
        var resource = GameData.GetResource(item.ResourceId);
        if (resource == null) return;

        // === ОБНОВЛЯЕМ МЕТАДАННЫЕ ===
        row.SetMeta("resource_id", item.ResourceId);
        row.SetMeta("quality", (int)item.Quality);
        row.SetMeta("is_collection", isCollection);

        // === ИКОНКА ===
        var iconRect = row.GetNodeOrNull<TextureRect>("IconRect");
        if (iconRect != null)
        {
            string iconPath = GetResourceIconPath(item.ResourceId);
            
            // Пытаемся загрузить специфичный спрайт
            if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
            {
                iconRect.Texture = GD.Load<Texture2D>(iconPath);
                iconRect.Visible = true;
            }
            else
            {
                // ЗАПАСНОЙ ВАРИАНТ: Если спрайта нет, показываем стандартную иконку Godot 
                // (или вы можете заменить "res://icon.svg" на путь к вашей универсальной иконке "res://assets/museum/items/unknown.png")
                iconRect.Texture = GD.Load<Texture2D>("res://icon.svg"); 
                iconRect.Visible = true;
                
                // Опционально: можно сделать её полупрозрачной, чтобы было видно, что это заглушка
                iconRect.Modulate = new Color(1f, 1f, 1f, 0.5f);
            }
        }

        // === ТЕКСТОВЫЕ ПОЛЯ ===
        var nameLabel = row.GetNodeOrNull<Label>("NameLabel");
        if (nameLabel != null) nameLabel.Text = resource.DisplayName;

        var amountLabel = row.GetNodeOrNull<Label>("AmountLabel");
        if (amountLabel != null) amountLabel.Text = $"x{item.Amount}";

        var priceLabel = row.GetNodeOrNull<Label>("PriceLabel");
        if (priceLabel != null)
        {
            int price = CalculateItemValue(item) * item.Amount;
            priceLabel.Text = $"{price} монет";
        }

        // === КНОПКИ: ТОЛЬКО ПЕРЕКЛЮЧАЕМ ВИДИМОСТЬ ===
        // ВАЖНО: Здесь НЕТ никаких -= или += для Pressed!
        // Подписки на кнопки делаются ТОЛЬКО в CreateItemRow.
        
        var sellOneButton = row.GetNodeOrNull<Button>("SellOneButton");
        if (sellOneButton != null) 
        {
            sellOneButton.Visible = !isCollection;
        }

        var sellAllButton = row.GetNodeOrNull<Button>("SellAllOfItemButton");
        if (sellAllButton != null) 
        {
            sellAllButton.Visible = !isCollection;
        }

        var placeButton = row.GetNodeOrNull<Button>("PlaceButton");
        if (placeButton != null) 
        {
            placeButton.Visible = isCollection;
        }
    }

    // ===== РАСЧЁТ СТОИМОСТИ =====

    private int CalculateItemValue(FoundItem item)
    {
        var resource = GameData.GetResource(item.ResourceId);
        if (resource == null) return 0;

        float multiplier = resource.GetRarityMultiplier() * resource.GetQualityMultiplier(item.Quality);
        return (int)(resource.BaseSellPrice * multiplier);
    }

    // ===== УТИЛИТЫ =====

    private string GetResourceIcon(ResourceType type)
    {
        return type switch
        {
            ResourceType.Bone => "🦴",
            ResourceType.Tooth => "🦷",
            ResourceType.Gold => "💰",
            ResourceType.Gem => "💎",
            _ => "❓"
        };
    }

    private Color GetQualityColor(Quality quality, bool hasQuality)
    {
        if (!hasQuality) return Colors.White;

        return quality switch
        {
            Quality.Damaged => new Color(1f, 0.4f, 0.4f),  // Красноватый
            Quality.Good => new Color(0.4f, 1f, 0.4f),     // Зеленоватый
            _ => Colors.White
        };
    }

    /// <summary>
    /// Возвращает путь к спрайту ресурса (части коллекции или целой коллекции)
    /// </summary>
            private string GetResourceIconPath(string resourceId)
    {
        // 1. Проверяем, является ли это целой коллекцией
        var collection = GameData.GetCollection(resourceId);
        if (collection != null)
        {
            return collection.TexturePath;
        }

        // 2. Проверяем, является ли это частью коллекции
        var resource = GameData.GetResource(resourceId);
        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            string partName = fossil.Id.Replace($"{fossil.CollectionId}_", "");
            return $"res://assets/museum/items/{fossil.CollectionId}/{partName}.png";
        }

        // 3. НОВОЕ: Проверяем папку common для обычных находок
        string commonPath = $"res://assets/museum/items/common/{resourceId}.png";
        if (ResourceLoader.Exists(commonPath))
        {
            return commonPath;
        }

        // 4. Запасной вариант (если спрайт вдруг не найден)
        return "res://icon.svg"; 
    }

    // ===== ОБРАБОТЧИКИ =====

    private void OnPlaceCollectionPressed(string collectionId, Quality quality)
    {

        // Скрываем инвентарь, чтобы игрок видел комнату
        this.Visible = false;

        // Уведомляем RoomViewUI о начале размещения
        OnStartPlacement?.Invoke(collectionId);
    }

    private void OnSellOnePressed(string resourceId, Quality quality)
    {
        int earned = InventorySystem.Instance.SellItem(resourceId, quality, 1);
        if (earned > 0)
        {
            GD.Print($"[InventoryUI] Sold 1 for {earned} coins");
        }
    }

    private void OnSellAllOfItemPressed(string resourceId, Quality quality)
    {
        var item = InventorySystem.Instance.GetItem(resourceId, quality);
        if (item != null && item.Amount > 0)
        {
            int earned = InventorySystem.Instance.SellItem(resourceId, quality, item.Amount);
            GD.Print($"[InventoryUI] Sold all for {earned} coins");
        }
    }

    private void OnSellAllPressed()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        int totalEarned = 0;

        // Копируем список, чтобы не модифицировать во время итерации
        var itemsCopy = new List<FoundItem>(allItems);

        foreach (var item in itemsCopy)
        {
            int earned = InventorySystem.Instance.SellItem(item.ResourceId, item.Quality, item.Amount);
            totalEarned += earned;
        }

        GD.Print($"[InventoryUI] Sold everything for {totalEarned} coins");
    }

    private void OnClosePressed()
    {
        Visible = false;
    }

    public override void _ExitTree()
    {
        GD.Print($"[InventoryUI] === УНИЧТОЖАЕТСЯ === Путь: {GetPath()}, ID объекта: {GetInstanceId()}");

        // Когда узел уничтожается — очищаем ссылку, чтобы Instance пересоздался
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
