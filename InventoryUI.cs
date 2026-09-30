using Godot;
using System.Collections.Generic;
using System.Linq;
using System;

public partial class InventoryUI : CanvasLayer
{
    public static InventoryUI _instance;

    private enum Tab { Skeletons, Assembling, Findings, Bones, Tickets, Concretions }
    private Tab _currentTab = Tab.Skeletons;

    private PanelContainer _mainPanel;
    private Label _totalValueLabel;
    private VBoxContainer _itemsList;
    private Button _closeButton;

    private Button _tabSkeletonsButton;
    private Button _tabAssemblingButton;
    private Button _tabFindingsButton;
    private Button _tabBonesButton;
    private Button _tabTicketsButton;
    private Button _tabConcretionsButton;

    private Dictionary<string, Control> _rowCache = new();
    private SellItemDialog _sellDialog;

    public static InventoryUI Instance
    {
        get
        {
            if (_instance == null || !GodotObject.IsInstanceValid(_instance))
            {
                var sceneTree = Engine.GetMainLoop() as SceneTree;
                _instance = sceneTree?.Root.GetNodeOrNull<InventoryUI>("/root/InventoryUI");
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    public override void _Ready()
    {
        Instance = this;
        this.Layer = 101;

        CreateUI();
        CreateSellDialog();

        SwitchTab(Tab.Skeletons);
    }

    private void CreateUI()
    {
        // Затемнённый фон
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.5f);
        background.AnchorRight = 1;
        background.AnchorBottom = 1;
        background.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(background);

        // Главная панель
        _mainPanel = new PanelContainer();
        _mainPanel.AnchorLeft = 0.5f;
        _mainPanel.AnchorRight = 0.5f;
        _mainPanel.AnchorTop = 0.5f;
        _mainPanel.AnchorBottom = 0.5f;
        _mainPanel.OffsetLeft = -300;
        _mainPanel.OffsetRight = 300;
        _mainPanel.OffsetTop = -250;
        _mainPanel.OffsetBottom = 250;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        panelStyle.CornerRadiusTopLeft = 12;
        panelStyle.CornerRadiusTopRight = 12;
        panelStyle.CornerRadiusBottomLeft = 12;
        panelStyle.CornerRadiusBottomRight = 12;
        panelStyle.ContentMarginLeft = 20;
        panelStyle.ContentMarginRight = 20;
        panelStyle.ContentMarginTop = 20;
        panelStyle.ContentMarginBottom = 20;
        _mainPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_mainPanel);

        // Контейнер контента
        var contentContainer = new VBoxContainer();
        contentContainer.AddThemeConstantOverride("separation", 15);
        _mainPanel.AddChild(contentContainer);

        // Верхняя строка: заголовок и кнопка закрытия
        var topRow = new HBoxContainer();
        var titleLabel = new Label();
        titleLabel.Text = LocalizationManager.Tr("ui.inventory.title");
        if (titleLabel.Text == "ui.inventory.title") titleLabel.Text = "Инвентарь";
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        titleLabel.AddThemeColorOverride("font_color", Colors.Gold);
        titleLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        topRow.AddChild(titleLabel);

        _closeButton = new Button();
        _closeButton.Text = "✕";
        _closeButton.CustomMinimumSize = new Vector2(40, 40);
        _closeButton.Pressed += OnClosePressed;
        topRow.AddChild(_closeButton);

        contentContainer.AddChild(topRow);

        // Вкладки
        var tabsContainer = new HBoxContainer();
        tabsContainer.AddThemeConstantOverride("separation", 5);

        _tabSkeletonsButton = CreateTabButton("ui.inventory.skeletons", Tab.Skeletons);
        _tabAssemblingButton = CreateTabButton("ui.inventory.assembling", Tab.Assembling);
        _tabFindingsButton = CreateTabButton("ui.inventory.findings", Tab.Findings);
        _tabBonesButton = CreateTabButton("ui.inventory.bones", Tab.Bones);
        _tabTicketsButton = CreateTabButton("ui.inventory.tickets", Tab.Tickets);
        _tabConcretionsButton = CreateTabButton("ui.inventory.concretions", Tab.Concretions);

        tabsContainer.AddChild(_tabSkeletonsButton);
        tabsContainer.AddChild(_tabAssemblingButton);
        tabsContainer.AddChild(_tabFindingsButton);
        tabsContainer.AddChild(_tabBonesButton);
        tabsContainer.AddChild(_tabTicketsButton);
        tabsContainer.AddChild(_tabConcretionsButton);

        contentContainer.AddChild(tabsContainer);

        // Общая стоимость
        _totalValueLabel = new Label();
        _totalValueLabel.AddThemeFontSizeOverride("font_size", 14);
        _totalValueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        contentContainer.AddChild(_totalValueLabel);

        // ScrollContainer со списком предметов
        var scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        contentContainer.AddChild(scrollContainer);

        _itemsList = new VBoxContainer();
        _itemsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _itemsList.AddThemeConstantOverride("separation", 10);
        scrollContainer.AddChild(_itemsList);
    }

    private Button CreateTabButton(string localizationKey, Tab tab)
    {
        var button = new Button();
        button.Text = LocalizationManager.Tr(localizationKey);
        if (button.Text == localizationKey)
        {
            // Fallback для непереведённых ключей
            button.Text = localizationKey switch
            {
                "ui.inventory.skeletons" => "Скелеты",
                "ui.inventory.assembling" => "Сборка",
                "ui.inventory.findings" => "Находки",
                "ui.inventory.bones" => "Кости",
                "ui.inventory.tickets" => "Билеты",
                "ui.inventory.concretions" => "Конкреции",
                _ => localizationKey
            };
        }
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.CustomMinimumSize = new Vector2(80, 35);
        button.Pressed += () => SwitchTab(tab);
        return button;
    }

    private void CreateSellDialog()
    {
        _sellDialog = new SellItemDialog();
        _sellDialog.OnSellConfirmed += OnSellConfirmed;
        AddChild(_sellDialog);
    }

    private void SwitchTab(Tab newTab)
    {
        _currentTab = newTab;
        _rowCache.Clear();

        _tabSkeletonsButton.Disabled = (_currentTab == Tab.Skeletons);
        _tabAssemblingButton.Disabled = (_currentTab == Tab.Assembling);
        _tabFindingsButton.Disabled = (_currentTab == Tab.Findings);
        _tabBonesButton.Disabled = (_currentTab == Tab.Bones);
        _tabTicketsButton.Disabled = (_currentTab == Tab.Tickets);
        _tabConcretionsButton.Disabled = (_currentTab == Tab.Concretions);

        foreach (var child in _itemsList.GetChildren())
        {
            child.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;
        if (InventorySystem.Instance == null) return;
        if (_itemsList == null) return;

        try
        {
            switch (_currentTab)
            {
                case Tab.Skeletons:
                    UpdateSkeletonsDisplay();
                    break;
                case Tab.Assembling:
                    UpdateAssemblingDisplay();
                    break;
                case Tab.Findings:
                    UpdateFindingsDisplay();
                    break;
                case Tab.Bones:
                    UpdateBonesDisplay();
                    break;
                case Tab.Tickets:
                    UpdateTicketsDisplay();
                    break;
                case Tab.Concretions:
                    UpdateConcretionsDisplay();
                    break;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[InventoryUI] Ошибка при отрисовке: {e.Message}");
        }
    }

    // ===== ВКЛАДКА 1: SKELETONS =====
    private void UpdateSkeletonsDisplay()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        var skeletons = allItems.Where(item => GameData.GetCollection(item.ResourceId) != null).ToList();

        int totalValue = skeletons.Sum(item => CalculateItemValue(item) * item.Amount);
        _totalValueLabel.Text = LocalizationManager.TrFormat("ui.inventory.total_value", totalValue);

        var neededKeys = new HashSet<string>(skeletons.Select(item => item.ResourceId));
        RemoveCachedRows(neededKeys);

        foreach (var item in skeletons)
        {
            if (!_rowCache.ContainsKey(item.ResourceId))
            {
                var row = CreateItemRow(item, true);
                _itemsList.AddChild(row);
                _rowCache[item.ResourceId] = row;
            }
            UpdateItemRow(_rowCache[item.ResourceId], item);
        }
    }

    // ===== ВКЛАДКА 2: ASSEMBLING =====
    private void UpdateAssemblingDisplay()
    {
        _totalValueLabel.Text = LocalizationManager.Tr("ui.inventory.assembling_hint");

        var allCollections = GameData.GetAllCollections();
        var eligibleCollections = new List<CollectionDefinition>();

        foreach (var collection in allCollections)
        {
            bool hasAnyPiece = collection.Pieces.Any(piece => InventorySystem.Instance.GetTotalAmount(piece.Id) > 0);
            if (hasAnyPiece)
            {
                eligibleCollections.Add(collection);
            }
        }

        var sortedCollections = eligibleCollections
            .OrderByDescending(c => InventorySystem.Instance.CanAssembleCollection(c))
            .ThenBy(c => c.DisplayName)
            .ToList();

        var neededKeys = new HashSet<string>(sortedCollections.Select(c => c.Id));
        RemoveCachedRows(neededKeys);

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

    // ===== ВКЛАДКА 3: FINDINGS =====
    private void UpdateFindingsDisplay()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        var findings = allItems.Where(item =>
        {
            var res = GameData.GetResource(item.ResourceId);
            
            if (GameData.IsTicket(item.ResourceId)) return false;
            if (GameData.GetCollection(item.ResourceId) != null) return false;
            if (res is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId)) return false;
            if (res is ConcretionDefinition) return false;
            
            return res != null;
        }).ToList();

        int totalValue = findings.Sum(item => CalculateItemValue(item) * item.Amount);
        _totalValueLabel.Text = LocalizationManager.TrFormat("ui.inventory.total_value", totalValue);

        var neededKeys = new HashSet<string>(findings.Select(item => item.ResourceId));
        RemoveCachedRows(neededKeys);

        foreach (var item in findings)
        {
            if (!_rowCache.ContainsKey(item.ResourceId))
            {
                var row = CreateItemRow(item, false);
                _itemsList.AddChild(row);
                _rowCache[item.ResourceId] = row;
            }
            UpdateItemRow(_rowCache[item.ResourceId], item);
        }
    }

    // ===== ВКЛАДКА 4: BONES =====
    private void UpdateBonesDisplay()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        var bones = allItems.Where(item =>
        {
            var res = GameData.GetResource(item.ResourceId);
            return res is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId);
        }).ToList();

        int totalValue = bones.Sum(item => CalculateItemValue(item) * item.Amount);
        _totalValueLabel.Text = LocalizationManager.TrFormat("ui.inventory.total_value", totalValue);

        var neededKeys = new HashSet<string>(bones.Select(item => item.ResourceId));
        RemoveCachedRows(neededKeys);

        foreach (var item in bones)
        {
            if (!_rowCache.ContainsKey(item.ResourceId))
            {
                var row = CreateItemRow(item, false);
                _itemsList.AddChild(row);
                _rowCache[item.ResourceId] = row;
            }
            UpdateItemRow(_rowCache[item.ResourceId], item);
        }
    }

    // ===== ВКЛАДКА 5: TICKETS =====
    private void UpdateTicketsDisplay()
    {
        string ticketsInfoText = LocalizationManager.Tr("ui.inventory.tickets_info");
        if (ticketsInfoText == "ui.inventory.tickets_info") 
        {
            ticketsInfoText = "Используйте билет для начала раскопок";
        }
        _totalValueLabel.Text = ticketsInfoText;
        
        var allItems = InventorySystem.Instance.GetAllItems();
        var tickets = allItems.Where(item => GameData.IsTicket(item.ResourceId)).ToList();

        var neededKeys = new HashSet<string>(tickets.Select(item => item.ResourceId));
        
        string emptyStateKey = "__empty_tickets__";
        if (tickets.Count == 0)
        {
            neededKeys.Add(emptyStateKey);
        }
        
        RemoveCachedRows(neededKeys);

        if (tickets.Count == 0)
        {
            if (!_rowCache.ContainsKey(emptyStateKey))
            {
                var emptyLabel = new Label();
                
                string noTicketsText = LocalizationManager.Tr("ui.inventory.no_tickets");
                if (noTicketsText == "ui.inventory.no_tickets")
                {
                    noTicketsText = "Нет доступных билетов";
                }
                
                emptyLabel.Text = noTicketsText;
                emptyLabel.AddThemeFontSizeOverride("font_size", 16);
                emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
                emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
                _itemsList.AddChild(emptyLabel);
                _rowCache[emptyStateKey] = emptyLabel;
            }
            return;
        }

        foreach (var item in tickets)
        {
            if (!_rowCache.ContainsKey(item.ResourceId))
            {
                var row = CreateTicketRow(item);
                _itemsList.AddChild(row);
                _rowCache[item.ResourceId] = row;
            }
            UpdateTicketRow(_rowCache[item.ResourceId], item);
        }
    }

    // ===== ВКЛАДКА 6: CONCRETIONS =====
    private void UpdateConcretionsDisplay()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        var concretions = allItems.Where(item =>
        {
            var res = GameData.GetResource(item.ResourceId);
            return res is ConcretionDefinition;
        }).ToList();

        string infoText = LocalizationManager.Tr("ui.inventory.concretions_hint");
        _totalValueLabel.Text = infoText;

        var neededKeys = new HashSet<string>(concretions.Select(item => item.ResourceId));
        RemoveCachedRows(neededKeys);

        foreach (var item in concretions)
        {
            if (!_rowCache.ContainsKey(item.ResourceId))
            {
                var row = CreateConcretionRow(item);
                _itemsList.AddChild(row);
                _rowCache[item.ResourceId] = row;
            }
            UpdateConcretionRow(_rowCache[item.ResourceId], item);
        }
    }

    private Control CreateConcretionRow(FoundItem item)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.SetMeta("resource_id", item.ResourceId);

        var icon = new TextureRect();
        icon.Name = "IconRect";
        icon.CustomMinimumSize = new Vector2(40, 40);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        row.AddChild(icon);

        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        var amountLabel = new Label();
        amountLabel.Name = "AmountLabel";
        amountLabel.AddThemeFontSizeOverride("font_size", 12);
        amountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        row.AddChild(amountLabel);

        var openButton = new Button();
        openButton.Name = "OpenButton";
        openButton.Text = "🔨 " + LocalizationManager.Tr("ui.inventory.open");
        if (openButton.Text == "🔨 ui.inventory.open") openButton.Text = "🔨 Раскрыть";
        openButton.CustomMinimumSize = new Vector2(120, 0);
        openButton.AddThemeColorOverride("font_color", new Color(0.2f, 0.8f, 0.2f));
        openButton.Pressed += () => OnOpenConcretionPressed((string)row.GetMeta("resource_id"));
        row.AddChild(openButton);

        return row;
    }

    private void UpdateConcretionRow(Control row, FoundItem item)
    {
        var res = GameData.GetResource(item.ResourceId);

        var icon = row.GetNodeOrNull<TextureRect>("IconRect");
        if (icon != null)
        {
            string iconPath = GetResourceIconPath(item.ResourceId);
            if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
            {
                icon.Texture = GD.Load<Texture2D>(iconPath);
                icon.Visible = true;
            }
            else
            {
                icon.Visible = false;
            }
        }

        var nameLabel = row.GetNodeOrNull<Label>("NameLabel");
        if (nameLabel != null) nameLabel.Text = res?.DisplayName ?? item.ResourceId;

        var amountLabel = row.GetNodeOrNull<Label>("AmountLabel");
        if (amountLabel != null) amountLabel.Text = $"x{item.Amount}";
    }

    private void OnOpenConcretionPressed(string concretionId)
    {
        GD.Print($"[InventoryUI] Открытие конкреции: {concretionId}");
        
        var openingUI = new ConcretionOpeningUI();
        openingUI.Setup(concretionId);
        AddChild(openingUI);
    }

    private Control CreateTicketRow(FoundItem item)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.SetMeta("resource_id", item.ResourceId);

        var iconRect = new TextureRect();
        iconRect.Name = "IconRect";
        iconRect.CustomMinimumSize = new Vector2(40, 40);
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        row.AddChild(iconRect);

        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(nameLabel);

        var amountLabel = new Label();
        amountLabel.Name = "AmountLabel";
        amountLabel.AddThemeFontSizeOverride("font_size", 14);
        amountLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        row.AddChild(amountLabel);

        var useBtn = new Button();
        useBtn.Name = "UseButton";
        useBtn.Text = LocalizationManager.Tr("ui.inventory.use_ticket");
        useBtn.CustomMinimumSize = new Vector2(120, 0);
        useBtn.AddThemeColorOverride("font_color", new Color(0.2f, 0.8f, 0.2f));
        useBtn.Pressed += () => OnUseTicketPressed((string)row.GetMeta("resource_id"));
        row.AddChild(useBtn);

        return row;
    }

    private void UpdateTicketRow(Control row, FoundItem item)
    {
        var ticket = GameData.GetTicket(item.ResourceId);
        if (ticket == null) return;

        var iconRect = row.GetNodeOrNull<TextureRect>("IconRect");
        if (iconRect != null)
        {
            string iconPath = $"res://assets/museum/tickets/{item.ResourceId}.png";
            if (ResourceLoader.Exists(iconPath))
                iconRect.Texture = GD.Load<Texture2D>(iconPath);
            else
                iconRect.Texture = GD.Load<Texture2D>("res://assets/museum/tickets/ticket.png");
        }

        row.GetNodeOrNull<Label>("NameLabel").Text = ticket.DisplayName;
        row.GetNodeOrNull<Label>("AmountLabel").Text = $"x{item.Amount}";
    }

        private void OnUseTicketPressed(string ticketId)
    {
        var ticket = GameData.GetTicket(ticketId);
        if (ticket == null || string.IsNullOrEmpty(ticket.TargetLocationId))
        {
            GD.PrintErr($"[InventoryUI] Ошибка: билет '{ticketId}' не имеет целевой локации!");
            return;
        }

        // === ИСПРАВЛЕНИЕ: Всегда сбрасываем прогресс целевой локации ===
        // Когда игрок использует билет, он хочет начать заново
        DiggingManager.Instance.ResetLocationProgress(ticket.TargetLocationId);
        GD.Print($"[InventoryUI] 🔄 Сброшен прогресс локации: {ticket.TargetLocationId}");

        // Списываем 1 билет из инвентаря
        InventorySystem.Instance.RemoveItem(ticketId, 1);
        
        // Активируем локацию
        LocationSystem.Instance.StartDiggingSession(ticket.TargetLocationId);
        
        // Переходим на сцену раскопок
        GetTree().ChangeSceneToFile("res://DigSite.tscn");
    }

    // ===== УТИЛИТЫ =====
    private void RemoveCachedRows(HashSet<string> neededKeys)
    {
        var toRemove = _rowCache.Keys.Where(k => !neededKeys.Contains(k)).ToList();
        foreach (var key in toRemove)
        {
            _rowCache[key].QueueFree();
            _rowCache.Remove(key);
        }
    }

    private Control CreateItemRow(FoundItem item, bool isCollection)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        row.SetMeta("resource_id", item.ResourceId);

        var iconRect = new TextureRect();
        iconRect.Name = "IconRect";
        iconRect.CustomMinimumSize = new Vector2(40, 40);
        iconRect.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        iconRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        row.AddChild(iconRect);

        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        row.AddChild(nameLabel);

        var amountLabel = new Label();
        amountLabel.Name = "AmountLabel";
        amountLabel.AddThemeFontSizeOverride("font_size", 12);
        amountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        row.AddChild(amountLabel);

        var priceLabel = new Label();
        priceLabel.Name = "PriceLabel";
        priceLabel.AddThemeFontSizeOverride("font_size", 12);
        priceLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        row.AddChild(priceLabel);

        if (isCollection)
        {
            var placeBtn = new Button();
            placeBtn.Name = "PlaceButton";
            placeBtn.Text = LocalizationManager.Tr("ui.inventory.place");
            placeBtn.CustomMinimumSize = new Vector2(120, 0);
            placeBtn.Pressed += () => OnPlaceCollectionPressed((string)row.GetMeta("resource_id"));
            row.AddChild(placeBtn);
        }

        var sellBtn = new Button();
        sellBtn.Name = "SellButton";
        sellBtn.Text = LocalizationManager.Tr("ui.inventory.sell");
        sellBtn.CustomMinimumSize = new Vector2(90, 0);
        sellBtn.Pressed += () => OnSellPressed((string)row.GetMeta("resource_id"));
        row.AddChild(sellBtn);

        return row;
    }

    private void UpdateItemRow(Control row, FoundItem item)
    {
        var resource = GameData.GetResource(item.ResourceId);
        if (resource == null) return;

        row.SetMeta("resource_id", item.ResourceId);

        var iconRect = row.GetNodeOrNull<TextureRect>("IconRect");
        if (iconRect != null)
        {
            string iconPath = GetResourceIconPath(item.ResourceId);
            if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
            {
                iconRect.Texture = GD.Load<Texture2D>(iconPath);
                iconRect.Visible = true;
            }
        }

        var nameLabel = row.GetNodeOrNull<Label>("NameLabel");
        if (nameLabel != null) nameLabel.Text = resource.DisplayName;

        var amountLabel = row.GetNodeOrNull<Label>("AmountLabel");
        if (amountLabel != null) amountLabel.Text = $"x{item.Amount}";

        var priceLabel = row.GetNodeOrNull<Label>("PriceLabel");
        if (priceLabel != null)
        {
            int price = CalculateItemValue(item) * item.Amount;
            priceLabel.Text = $"{price} 🪙";
        }
    }

    private int CalculateItemValue(FoundItem item)
    {
        var resource = GameData.GetResource(item.ResourceId);
        if (resource == null) return 0;
        float multiplier = resource.GetRarityMultiplier();
        return (int)(resource.BaseSellPrice * multiplier);
    }

        private string GetResourceIconPath(string resourceId)
    {
        // Конкреции лежат в отдельной папке
        var resource = GameData.GetResource(resourceId);
        if (resource is ConcretionDefinition)
        {
            // common_concretion -> concretion/common.png (для иконки в инвентаре)
            string shortName = resourceId.Replace("_concretion", "");
            string concretionIconPath = $"res://assets/museum/items/common/concretion/{shortName}_1.png";
            if (ResourceLoader.Exists(concretionIconPath)) return concretionIconPath;
        }

        // Коллекции
        var collection = GameData.GetCollection(resourceId);
        if (collection != null) return collection.TexturePath;

        // Кости из коллекций (части скелетов)
        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            string partName = fossil.Id.Replace($"{fossil.CollectionId}_", "");
            return $"res://assets/museum/items/{fossil.CollectionId}/{partName}.png";
        }

        // === НОВОЕ: Экспонаты с вариантами ===
        if (resource is FossilDefinition standaloneFossil && standaloneFossil.VariantCount > 1)
        {
            // Пробуем загрузить случайный вариант
            int randomVariant = GD.RandRange(1, standaloneFossil.VariantCount);
            string variantPath = $"res://assets/museum/items/common/{resourceId}/{randomVariant}.png";
            if (ResourceLoader.Exists(variantPath)) return variantPath;
            
            // Fallback на первый вариант
            string firstVariantPath = $"res://assets/museum/items/common/{resourceId}/1.png";
            if (ResourceLoader.Exists(firstVariantPath)) return firstVariantPath;
        }

        // Обычные ресурсы (без вариантов)
        string commonPath = $"res://assets/museum/items/common/{resourceId}.png";
        if (ResourceLoader.Exists(commonPath)) return commonPath;

        // Fallback на папку с вариантом 1
        string variantFolder = $"res://assets/museum/items/common/{resourceId}/1.png";
        if (ResourceLoader.Exists(variantFolder)) return variantFolder;

        return "res://icon.svg";
    }

    // ===== ОБРАБОТЧИКИ =====
    private void OnPlaceCollectionPressed(string collectionId)
    {
        this.Visible = false;
        MuseumSystem.Instance?.StartPlacementFromInventory(collectionId);
    }

    private void OnSellPressed(string resourceId)
    {
        var item = InventorySystem.Instance.GetItem(resourceId);
        if (item == null || item.Amount <= 0) return;

        int pricePerUnit = CalculateItemValue(item);
        _sellDialog.Open(resourceId, item.Amount, pricePerUnit);
    }

    private void OnSellConfirmed(string resourceId, int amount)
    {
        int earned = InventorySystem.Instance.SellItem(resourceId, amount);
        if (earned > 0)
        {
            GD.Print($"[InventoryUI] Продано {amount}x {resourceId} за {earned} монет");
        }
    }

    public void SwitchToTicketsTab()
    {
        SwitchTab(Tab.Tickets);
        Visible = true;
    }

    private void OnClosePressed()
    {
        Visible = false;
    }

    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
    }

    // ===== КОЛЛЕКЦИИ =====
    private Control CreateCollectionRow(CollectionDefinition collection)
    {
        var row = new VBoxContainer();
        row.AddThemeConstantOverride("separation", 5);

        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        row.AddChild(nameLabel);

        var iconsRow = new HBoxContainer();
        iconsRow.Name = "IconsRow";
        iconsRow.AddThemeConstantOverride("separation", 15);
        iconsRow.Alignment = BoxContainer.AlignmentMode.Center;
        row.AddChild(iconsRow);

        var assembledIcon = new TextureRect();
        assembledIcon.Name = "AssembledIcon";
        assembledIcon.CustomMinimumSize = new Vector2(40, 40);
        assembledIcon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        assembledIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconsRow.AddChild(assembledIcon);

        var arrowLabel = new Label();
        arrowLabel.Name = "ArrowLabel";
        arrowLabel.Text = "<=";
        arrowLabel.AddThemeFontSizeOverride("font_size", 24);
        arrowLabel.VerticalAlignment = VerticalAlignment.Center;
        iconsRow.AddChild(arrowLabel);

        var piecesContainer = new HBoxContainer();
        piecesContainer.Name = "PiecesContainer";
        piecesContainer.AddThemeConstantOverride("separation", 10);
        iconsRow.AddChild(piecesContainer);

        for (int i = 0; i < collection.Pieces.Count; i++)
        {
            var pieceSlot = new VBoxContainer();
            pieceSlot.Name = $"PieceSlot_{i}";
            pieceSlot.Alignment = BoxContainer.AlignmentMode.Center;

            var iconRect = new TextureRect();
            iconRect.Name = $"PieceIcon_{i}";
            iconRect.CustomMinimumSize = new Vector2(32, 32);
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

        var assembleBtn = new Button();
        assembleBtn.Name = "AssembleButton";
        assembleBtn.Text = LocalizationManager.Tr("ui.inventory.assemble");
        assembleBtn.CustomMinimumSize = new Vector2(120, 0);
        assembleBtn.Pressed += () => OnAssemblePressed(collection);
        row.AddChild(assembleBtn);

        return row;
    }

    private void UpdateCollectionRow(Control row, CollectionDefinition collection)
    {
        var nameLabel = row.GetNodeOrNull<Label>("NameLabel");
        if (nameLabel != null) nameLabel.Text = collection.DisplayName;

        var arrowLabel = row.GetNodeOrNull<Label>("IconsRow/ArrowLabel");
        bool canAssemble = InventorySystem.Instance.CanAssembleCollection(collection);
        if (arrowLabel != null)
        {
            arrowLabel.Modulate = canAssemble ? new Color(0.4f, 1f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
        }

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

        for (int i = 0; i < collection.Pieces.Count; i++)
        {
            var piece = collection.Pieces[i];

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
            _rowCache.Clear();
            foreach (var child in _itemsList.GetChildren())
            {
                child.QueueFree();
            }
        }
        else
        {
            GD.PrintErr("[UI] Не удалось собрать коллекцию.");
        }
    }
}