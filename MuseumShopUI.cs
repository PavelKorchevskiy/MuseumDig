using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class MuseumShopUI : CanvasLayer
{
    private enum ShopTab { Furniture, Tickets }

    private PanelContainer _panel;
    private Label _hintLabel;

    private ScrollContainer _furnitureScroll;
    private VBoxContainer _furnitureSection;

    private ScrollContainer _ticketsScroll;
    private VBoxContainer _ticketsSection;

    private Button _tabFurnitureBtn;
    private Button _tabTicketsBtn;
    private ShopTab _currentTab = ShopTab.Furniture;

    private Button _closeButton;

    // === Кэш кнопок для быстрого обновления (общий для мебели и билетов,
    // т.к. их Id не пересекаются: "display_case_..." и "ticket_...") ===
    private Dictionary<string, Button> _buyButtons = new();
    private Dictionary<string, int> _buyPrices = new();

    // === Размеры кнопки закрытия (как в инвентаре) ===
    private const float CloseButtonSize = 100f;

    public override void _Ready()
    {
        Layer = 150;

        // Затемнённый фон
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.6f);
        background.AnchorRight = 1;
        background.AnchorBottom = 1;
        background.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(background);

        // === Панель магазина (как в инвентаре) ===
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0.05f;
        _panel.AnchorRight = 0.95f;
        _panel.AnchorTop = 0.07f;
        _panel.AnchorBottom = 0.99f;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.12f, 0.12f, 0.18f, 1.0f);  // Непрозрачный
        panelStyle.CornerRadiusTopLeft = 20;
        panelStyle.CornerRadiusTopRight = 20;
        panelStyle.CornerRadiusBottomLeft = 20;
        panelStyle.CornerRadiusBottomRight = 20;
        panelStyle.ContentMarginLeft = 20;
        panelStyle.ContentMarginRight = 20;
        panelStyle.ContentMarginTop = 20;
        panelStyle.ContentMarginBottom = 20;
        _panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_panel);

        var contentContainer = new VBoxContainer();
        contentContainer.AddThemeConstantOverride("separation", 15);
        _panel.AddChild(contentContainer);

        // === Заголовок ===
        var titleLabel = new Label();
        string titleText = LocalizationManager.Tr("ui.shop.title");
        titleLabel.Text = titleText;
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", Colors.Gold);
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentContainer.AddChild(titleLabel);

        // === Подсказка (текст меняется вместе с вкладкой) ===
        _hintLabel = new Label();
        _hintLabel.AddThemeFontSizeOverride("font_size", 14);
        _hintLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        _hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        contentContainer.AddChild(_hintLabel);

        // === Страница со списком + вкладки под ней (над крестиком) ===
        contentContainer.AddChild(MobileUI.CreateTabbedPage(out var page, out var tabsRow));

        // Обе вкладки лежат в одной "стопке": PanelContainer растянул бы их
        // на всю страницу, но видна всегда только одна (см. SwitchTab).
        var pageStack = new Control();
        pageStack.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        pageStack.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        page.AddChild(pageStack);

        _tabFurnitureBtn = MobileUI.CreateTab(LocalizationManager.Tr("ui.shop.tab_furniture"));
        _tabFurnitureBtn.Pressed += () => SwitchTab(ShopTab.Furniture);
        tabsRow.AddChild(_tabFurnitureBtn);

        _tabTicketsBtn = MobileUI.CreateTab(LocalizationManager.Tr("ui.shop.tab_tickets"));
        _tabTicketsBtn.Pressed += () => SwitchTab(ShopTab.Tickets);
        tabsRow.AddChild(_tabTicketsBtn);
        TutorialTargets.Register("shop.tab.tickets", _tabTicketsBtn);

        // === Вкладка "Мебель" ===
        _furnitureScroll = new ScrollContainer();
        _furnitureScroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _furnitureScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        pageStack.AddChild(_furnitureScroll);

        _furnitureSection = new VBoxContainer();
        _furnitureSection.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _furnitureSection.AddThemeConstantOverride("separation", 10);
        _furnitureScroll.AddChild(_furnitureSection);

        // === Вкладка "Билеты" ===
        _ticketsScroll = new ScrollContainer();
        _ticketsScroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _ticketsScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        pageStack.AddChild(_ticketsScroll);

        _ticketsSection = new VBoxContainer();
        _ticketsSection.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _ticketsSection.AddThemeConstantOverride("separation", 10);
        _ticketsScroll.AddChild(_ticketsSection);

        // Заполняем оба списка один раз; переключение вкладок только показывает/скрывает
        PopulateFurnitureList();
        PopulateTicketsList();
        SwitchTab(ShopTab.Furniture);

        // === Красная кнопка закрытия с крестиком (в самом низу) ===
        var closeRow = new HBoxContainer();
        closeRow.Alignment = BoxContainer.AlignmentMode.Center;
        contentContainer.AddChild(closeRow);

        _closeButton = new Button();
        _closeButton.CustomMinimumSize = new Vector2(CloseButtonSize, CloseButtonSize);
        _closeButton.PivotOffset = new Vector2(CloseButtonSize / 2f, CloseButtonSize / 2f);

        var closeStyle = new StyleBoxFlat();
        closeStyle.BgColor = new Color(0.8f, 0.25f, 0.25f, 1.0f);  // Бледно-красный
        closeStyle.CornerRadiusTopLeft = 20;
        closeStyle.CornerRadiusTopRight = 20;
        closeStyle.CornerRadiusBottomLeft = 20;
        closeStyle.CornerRadiusBottomRight = 20;

        _closeButton.AddThemeStyleboxOverride("normal", closeStyle);
        _closeButton.AddThemeStyleboxOverride("hover", closeStyle);
        _closeButton.AddThemeStyleboxOverride("pressed", closeStyle);
        _closeButton.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        _closeButton.Text = "";
        _closeButton.AddThemeFontOverride("font", new FontVariation());
        _closeButton.AddThemeColorOverride("font_color", Colors.Transparent);

        var closeCenter = new CenterContainer();
        closeCenter.MouseFilter = Control.MouseFilterEnum.Ignore;
        closeCenter.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var closeIcon = new Label();
        closeIcon.Text = "✕";
        closeIcon.AddThemeFontSizeOverride("font_size", 48);
        closeIcon.AddThemeColorOverride("font_color", Colors.White);
        closeCenter.AddChild(closeIcon);
        _closeButton.AddChild(closeCenter);

        _closeButton.Pressed += OnClosePressed;
        closeRow.AddChild(_closeButton);

        Visible = false;
    }

    // ===== ВКЛАДКИ =====

    /// <summary>Текущий магазин в сцене музея (для обучения).</summary>
    public static MuseumShopUI Current { get; private set; }

    public override void _EnterTree() => Current = this;

    public override void _ExitTree()
    {
        if (Current == this) Current = null;
    }

    public void ShowFurnitureTab() => SwitchTab(ShopTab.Furniture);
    public void ShowTicketsTab() => SwitchTab(ShopTab.Tickets);
    public bool IsTicketsTabOpen => Visible && _currentTab == ShopTab.Tickets;

    private void SwitchTab(ShopTab tab)
    {
        _currentTab = tab;

        _furnitureScroll.Visible = tab == ShopTab.Furniture;
        _ticketsScroll.Visible = tab == ShopTab.Tickets;

        MobileUI.SetTabActive(_tabFurnitureBtn, tab == ShopTab.Furniture);
        MobileUI.SetTabActive(_tabTicketsBtn, tab == ShopTab.Tickets);

        if (tab == ShopTab.Furniture)
        {
            string hintText = LocalizationManager.Tr("ui.shop.hint");
            _hintLabel.Text = hintText;
        }
        else
        {
            string hintText = LocalizationManager.Tr("ui.shop.hint_tickets");
            _hintLabel.Text = hintText;
        }
    }

    // ===== ВКЛАДКА "МЕБЕЛЬ" =====

    /// <summary>
    /// Заполняет список мебели.
    /// </summary>
    private void PopulateFurnitureList()
    {
        if (MuseumSystem.Instance == null) return;

        var templates = MuseumSystem.Instance.GetAvailableFurnitureTemplates();

        foreach (var template in templates)
        {
            var row = CreateFurnitureRow(template);
            _furnitureSection.AddChild(row);
        }
    }

    /// <summary>
    /// Создаёт строку мебели в стиле инвентаря.
    /// </summary>
    private Control CreateFurnitureRow(FurnitureTemplate template)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        // Иконка мебели (если есть)
        var iconRect = new TextureRect();
        iconRect.Name = "IconRect";
        iconRect.CustomMinimumSize = new Vector2(48, 48);
        iconRect.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        iconRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;

        string iconPath = FurnitureTemplate.GetTexturePathForType(template.TypeId);
        if (ResourceLoader.Exists(iconPath))
        {
            iconRect.Texture = GD.Load<Texture2D>(iconPath);
        }
        else
        {
            // Fallback на иконку по умолчанию
            string fallbackPath = "res://assets/ui/icons/tab_skeletons.png";
            if (ResourceLoader.Exists(fallbackPath))
            {
                iconRect.Texture = GD.Load<Texture2D>(fallbackPath);
            }
        }
        row.AddChild(iconRect);

        // Название и размер (растягивается)
        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.Text = $"{template.DisplayName} ({template.Size.X}×{template.Size.Y})";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        nameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimChar;
        row.AddChild(nameLabel);

        // Цена с иконкой монеты
        var priceContainer = CreatePriceTag(template.BuyPrice);
        row.AddChild(priceContainer);

        // Кнопка "Купить"
        var buyBtn = new Button();
        buyBtn.Name = "BuyButton";
        string buyText = LocalizationManager.Tr("ui.shop.buy");
        buyBtn.Text = buyText;
        buyBtn.CustomMinimumSize = new Vector2(90, 0);
        buyBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        buyBtn.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));

        string typeId = template.TypeId;
        buyBtn.Pressed += () => OnBuyFurniturePressed(typeId);
        TutorialTargets.Register($"shop.buy.{typeId}", buyBtn);

        row.AddChild(buyBtn);

        // Кэшируем для быстрого обновления
        _buyButtons[typeId] = buyBtn;
        _buyPrices[typeId] = template.BuyPrice;

        return row;
    }

    private void OnBuyFurniturePressed(string typeId)
    {
        if (MuseumSystem.Instance.TryBuyFurniture(typeId))
        {
            GD.Print($"[MuseumShop] Bought furniture: {typeId}");
            Visible = false;

            var pendingFurniture = MuseumSystem.Instance.GetPendingFurniture();
            if (pendingFurniture != null && pendingFurniture.Count > 0)
            {
                var furniture = pendingFurniture[pendingFurniture.Count - 1];
                MuseumSystem.Instance.StartPlacementMode(furniture);

                var currentScene = GetTree().CurrentScene;
                var placementUI = currentScene.GetNodeOrNull<PlacementModeUI>("PlacementModeUI");

                if (placementUI == null)
                {
                    placementUI = new PlacementModeUI();
                    placementUI.Name = "PlacementModeUI";
                    currentScene.AddChild(placementUI);
                    GD.Print("[MuseumShop] ✅ PlacementModeUI создан и добавлен в сцену");
                }
                else
                {
                    GD.Print("[MuseumShop] ✅ PlacementModeUI уже существует");
                }

                var currentRoom = MuseumSystem.Instance.GetCurrentRoom();
                if (currentRoom != null)
                {
                    placementUI.StartPlacement(currentRoom, furniture);
                }
                else
                {
                    GD.PrintErr("[MuseumShop] ❌ Не удалось получить текущую комнату!");
                }
            }
        }
    }

    // ===== ВКЛАДКА "БИЛЕТЫ" =====

    /// <summary>
    /// Заполняет список билетов, доступных для покупки в магазине.
    /// Показываются только билеты с IsSellable == true.
    /// </summary>
    private void PopulateTicketsList()
    {
        var tickets = GameData.GetAllTickets()
            .Where(t => t.IsSellable)
            .OrderBy(t => t.ShopPrice)
            .ToList();

        if (tickets.Count == 0)
        {
            var emptyLabel = new Label();
            string emptyText = LocalizationManager.Tr("ui.shop.no_tickets");
            emptyLabel.Text = emptyText;
            emptyLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.6f, 0.6f));
            emptyLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _ticketsSection.AddChild(emptyLabel);
            return;
        }

        foreach (var ticket in tickets)
        {
            var row = CreateTicketShopRow(ticket);
            _ticketsSection.AddChild(row);
        }
    }

    private Control CreateTicketShopRow(TicketDefinition ticket)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);

        // Иконка билета — те же спрайты, что в инвентаре и у выпавшего билета
        var iconRect = new TextureRect();
        iconRect.Name = "IconRect";
        iconRect.CustomMinimumSize = new Vector2(48, 48);
        iconRect.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        iconRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;

        string iconPath = $"res://assets/museum/tickets/{ticket.Id}.png";
        if (!ResourceLoader.Exists(iconPath))
            iconPath = "res://assets/museum/tickets/ticket.png";

        if (ResourceLoader.Exists(iconPath))
            iconRect.Texture = GD.Load<Texture2D>(iconPath);

        row.AddChild(iconRect);

        // Название
        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.Text = ticket.DisplayName;
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameLabel.ClipText = true;
        nameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimChar;
        row.AddChild(nameLabel);

        // Цена с иконкой монеты
        var priceContainer = CreatePriceTag(ticket.ShopPrice);
        row.AddChild(priceContainer);

        // Кнопка "Купить"
        var buyBtn = new Button();
        buyBtn.Name = "BuyButton";
        string buyText = LocalizationManager.Tr("ui.shop.buy");
        buyBtn.Text = buyText;
        buyBtn.CustomMinimumSize = new Vector2(90, 0);
        buyBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        buyBtn.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));

        string ticketId = ticket.Id;
        buyBtn.Pressed += () => OnBuyTicketPressed(ticketId);

        row.AddChild(buyBtn);

        _buyButtons[ticketId] = buyBtn;
        _buyPrices[ticketId] = ticket.ShopPrice;

        return row;
    }

    private void OnBuyTicketPressed(string ticketId)
    {
        var ticket = GameData.GetTicket(ticketId);
        if (ticket == null) return;

        if (!Wallet.Instance.SpendCoins(ticket.ShopPrice))
        {
            GD.Print($"[MuseumShop] Недостаточно монет для билета: {ticketId}");
            return;
        }

        InventorySystem.Instance.AddItem(ticketId, 1);
        QuestSystem.Instance.OnTicketBought();
        SaveSystem.Instance?.MarkDirty();

        GD.Print($"[MuseumShop] 🎫 Куплен билет: {ticketId}");
    }

    // ===== ОБЩЕЕ =====

    private Control CreatePriceTag(int price)
    {
        var priceContainer = new HBoxContainer();
        priceContainer.Name = "PriceContainer";
        priceContainer.AddThemeConstantOverride("separation", 4);
        priceContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;

        var coinIcon = new TextureRect();
        coinIcon.Name = "CoinIcon";
        coinIcon.CustomMinimumSize = new Vector2(20, 20);
        coinIcon.Size = new Vector2(20, 20);
        coinIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        coinIcon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        coinIcon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        string coinPath = "res://assets/ui/icons/coin.png";
        if (ResourceLoader.Exists(coinPath))
        {
            coinIcon.Texture = GD.Load<Texture2D>(coinPath);
        }
        priceContainer.AddChild(coinIcon);

        var priceLabel = new Label();
        priceLabel.Name = "PriceLabel";
        priceLabel.Text = price.ToString("N0");
        priceLabel.AddThemeFontSizeOverride("font_size", 12);
        priceLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        priceLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        priceContainer.AddChild(priceLabel);

        return priceContainer;
    }

    public override void _Process(double delta)
    {
        if (Visible) UpdateButtons();
    }

    private void UpdateButtons()
    {
        int coins = Wallet.Instance?.GetCoins() ?? 0;

        foreach (var kvp in _buyButtons)
        {
            string id = kvp.Key;
            Button btn = kvp.Value;

            if (_buyPrices.TryGetValue(id, out int price))
            {
                btn.Disabled = coins < price;
            }
        }
    }

    private void OnClosePressed() => Visible = false;
}
