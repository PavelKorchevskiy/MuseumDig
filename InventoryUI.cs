using Godot;
using System.Collections.Generic;
using System.Linq;
using System;

public partial class InventoryUI : CanvasLayer
{
    public static InventoryUI _instance;

    // === НОВАЯ СТРУКТУРА ВКЛАДОК ===
    private enum Tab { Tickets, Bones, Findings, Concretions, Assembling, Skeletons }
    private enum TabGroup { Main, BonesGroup }
    
    private Tab _currentTab = Tab.Tickets;
    private bool _isBonesGroupVisible = false;  // Видимость второго ряда

    // === Размеры ===
    private float SmallButtonSize = 120f;  // Значение по умолчанию
    private float LargeButtonSize = 156f;  // Значение по умолчанию
    private const float ButtonSpacing = 12f;
    private const float PanelPadding = 20f;
    private const float CloseButtonSize = 100f;
    
    // === Состояния кнопок ===
    private const float PressedScale = 1.12f;
    private const float PressAnimDuration = 0.1f;
    private const float ReleaseAnimDuration = 0.15f;

    private PanelContainer _mainPanel;
    private Label _titleLabel;
    private Label _hintLabel;
    private VBoxContainer _itemsList;
    private Button _closeButton;

    // === Первый ряд (всегда виден) ===
    private Button _btnTickets;
    private Button _btnBones;
    private Button _btnFindings;
    private Button _btnConcretions;

    // === Второй ряд (появляется при нажатии на Кости) ===
    private HBoxContainer _bonesGroupRow;
    private Button _btnAssembling;
    private Button _btnSkeletons;
    private Button _btnBonesParts;  // Части костей (логика как старая вкладка Bones)

    private Dictionary<string, Control> _rowCache = new();
    private Dictionary<string, string> _iconPathCache = new();
    private bool _needsRefresh = false;
    private SellItemDialog _sellDialog;
        private bool _wasVisibleLastFrame = false;

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
        Visible = false;

        CreateUI();
        CreateSellDialog();

        SwitchTab(Tab.Tickets);
    }

        public override void _Process(double delta)
    {
        if (Visible && !_wasVisibleLastFrame)
        {
            // Инвентарь только что открылся — обновляем текущую вкладку
            RefreshCurrentTab();
        }
        _wasVisibleLastFrame = Visible;

        if (!Visible) return;
        if (InventorySystem.Instance == null) return;
        
        if (_needsRefresh)
        {
            _needsRefresh = false;
            RefreshCurrentTab();
        }
    }

    private void CreateUI()
    {
        CalculateButtonSizes();
        // Затемнённый фон
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.6f);
        background.AnchorRight = 1;
        background.AnchorBottom = 1;
        background.MouseFilter = Control.MouseFilterEnum.Stop;
        AddChild(background);

        // === Главная панель с отступами ===
        _mainPanel = new PanelContainer();
        
        // 5% отступы слева/справа/снизу, 30% сверху
        _mainPanel.AnchorLeft = 0.05f;
        _mainPanel.AnchorRight = 0.95f;
        _mainPanel.AnchorTop = 0.075f;
        _mainPanel.AnchorBottom = 0.99f;

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.12f, 0.12f, 0.18f, 1.0f);  // Непрозрачный
        panelStyle.CornerRadiusTopLeft = 20;
        panelStyle.CornerRadiusTopRight = 20;
        panelStyle.CornerRadiusBottomLeft = 20;
        panelStyle.CornerRadiusBottomRight = 20;
        panelStyle.ContentMarginLeft = PanelPadding;
        panelStyle.ContentMarginRight = PanelPadding;
        panelStyle.ContentMarginTop = PanelPadding;
        panelStyle.ContentMarginBottom = PanelPadding;
        _mainPanel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_mainPanel);

        var contentContainer = new VBoxContainer();
        contentContainer.AddThemeConstantOverride("separation", 15);
        _mainPanel.AddChild(contentContainer);

        // === Заголовок ===
        _titleLabel = new Label();
        _titleLabel.Text = LocalizationManager.Tr("ui.inventory.title");
        if (_titleLabel.Text == "ui.inventory.title") _titleLabel.Text = "Инвентарь";
        _titleLabel.AddThemeFontSizeOverride("font_size", 24);
        _titleLabel.AddThemeColorOverride("font_color", Colors.Gold);
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        contentContainer.AddChild(_titleLabel);

        // === Подсказка ===
        _hintLabel = new Label();
        _hintLabel.AddThemeFontSizeOverride("font_size", 14);
        _hintLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        _hintLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        contentContainer.AddChild(_hintLabel);

        // === Список предметов ===
        var scrollContainer = new ScrollContainer();
        scrollContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        scrollContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        contentContainer.AddChild(scrollContainer);

        _itemsList = new VBoxContainer();
        _itemsList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _itemsList.AddThemeConstantOverride("separation", 10);
        scrollContainer.AddChild(_itemsList);

        // === Второй ряд (Сборка, Скелеты, Части костей) ===
        _bonesGroupRow = new HBoxContainer();
        _bonesGroupRow.Alignment = BoxContainer.AlignmentMode.Center;
        _bonesGroupRow.AddThemeConstantOverride("separation", (int)ButtonSpacing);
        _bonesGroupRow.Visible = false;  // Изначально скрыт
        contentContainer.AddChild(_bonesGroupRow);

        _btnAssembling = CreateTabButton("ui.inventory.assembling", Tab.Assembling);
        _btnSkeletons = CreateTabButton("ui.inventory.skeletons", Tab.Skeletons);
        _btnBonesParts = CreateTabButton("ui.inventory.bones_parts", Tab.Bones);
        
        _bonesGroupRow.AddChild(_btnAssembling);
        _bonesGroupRow.AddChild(_btnSkeletons);
        _bonesGroupRow.AddChild(_btnBonesParts);

        // === Первый ряд (всегда виден) ===
        var mainRow = new HBoxContainer();
        mainRow.Alignment = BoxContainer.AlignmentMode.Center;
        mainRow.AddThemeConstantOverride("separation", (int)ButtonSpacing);
        contentContainer.AddChild(mainRow);

        _btnTickets = CreateTabButton("ui.inventory.tickets", Tab.Tickets);
        _btnBones = CreateTabButton("ui.inventory.bones", Tab.Bones, isParent: true);
        _btnFindings = CreateTabButton("ui.inventory.findings", Tab.Findings);
        _btnConcretions = CreateTabButton("ui.inventory.concretions", Tab.Concretions);
        
        mainRow.AddChild(_btnTickets);
        mainRow.AddChild(_btnBones);
        mainRow.AddChild(_btnFindings);
        mainRow.AddChild(_btnConcretions);

        // === Красная кнопка закрытия ===
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

        // Иконка крестика внутри
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
        AttachPressAnimation(_closeButton);
        closeRow.AddChild(_closeButton);
    }

    private Button CreateTabButton(string localizationKey, Tab tab, bool isParent = false)
    {
        var button = new Button();
        button.CustomMinimumSize = new Vector2(SmallButtonSize, SmallButtonSize);
        button.PivotOffset = new Vector2(SmallButtonSize / 2f, SmallButtonSize / 2f);
        button.MouseFilter = Control.MouseFilterEnum.Stop;
        button.SetMeta("tab", (int)tab);
        button.SetMeta("is_parent", isParent);

        // Стиль
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.18f, 0.18f, 0.25f, 0.9f);
        style.CornerRadiusTopLeft = 18;
        style.CornerRadiusTopRight = 18;
        style.CornerRadiusBottomLeft = 18;
        style.CornerRadiusBottomRight = 18;
        style.BorderWidthLeft = 2;
        style.BorderWidthTop = 2;
        style.BorderWidthRight = 2;
        style.BorderWidthBottom = 2;
        style.BorderColor = new Color(0.3f, 0.3f, 0.4f, 0.6f);

        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        
        // === ИСПРАВЛЕНО: Полностью скрываем текст ===
        button.Text = "";
        button.AddThemeFontOverride("font", new FontVariation());
        button.AddThemeColorOverride("font_color", Colors.Transparent);
        button.AddThemeColorOverride("font_hover_color", Colors.Transparent);
        button.AddThemeColorOverride("font_pressed_color", Colors.Transparent);
        button.AddThemeColorOverride("font_focus_color", Colors.Transparent);
        button.AddThemeColorOverride("font_disabled_color", Colors.Transparent);

        // Иконка внутри
        var center = new CenterContainer();
        center.MouseFilter = Control.MouseFilterEnum.Ignore;
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var icon = new TextureRect();
        icon.Name = "TabIcon";
        float iconSize = SmallButtonSize * 0.55f;
        icon.CustomMinimumSize = new Vector2(iconSize, iconSize);
        icon.Size = new Vector2(iconSize, iconSize);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        
        // Загружаем иконку
        string iconPath = GetTabIconPath(tab);
        
        // === ОТЛАДКА ===
        GD.Print($"[InventoryUI] Пытаюсь загрузить иконку: {iconPath}");
        
        if (ResourceLoader.Exists(iconPath))
        {
            icon.Texture = GD.Load<Texture2D>(iconPath);
            GD.Print($"[InventoryUI] ✅ Иконка загружена: {iconPath}");
        }
        else
        {
            GD.PrintErr($"[InventoryUI] ❌ Иконка НЕ найдена: {iconPath}");
        }

        center.AddChild(icon);
        button.AddChild(center);

        // Подключаем нажатие
        button.Pressed += () => OnTabButtonPressed(tab, isParent);
        AttachPressAnimation(button);

        return button;
    }

    private string GetTabIconPath(Tab tab)
    {
        return tab switch
        {
            Tab.Tickets => "res://assets/ui/icons/tab_tickets.png",
            Tab.Bones => "res://assets/ui/icons/tab_bones.png",
            Tab.Findings => "res://assets/ui/icons/tab_findings.png",
            Tab.Concretions => "res://assets/ui/icons/tab_concretions.png",
            Tab.Assembling => "res://assets/ui/icons/tab_assembling.png",
            Tab.Skeletons => "res://assets/ui/icons/tab_skeletons.png",
            _ => "res://assets/ui/icons/tab_findings.png"
        };
    }

        private void OnTabButtonPressed(Tab tab, bool isParent)
    {
        if (isParent && tab == Tab.Bones)
        {
            // Кнопка "Кости" — раскрываем/сворачиваем второй ряд
            _isBonesGroupVisible = !_isBonesGroupVisible;
            _bonesGroupRow.Visible = _isBonesGroupVisible;

            if (_isBonesGroupVisible)
            {
                // Автоматически выбираем первую вкладку второго ряда
                SwitchTab(Tab.Assembling);
            }
            UpdateButtonsState();
            return;
        }

        // Обычная вкладка — сворачиваем второй ряд, если активна не группа "Кости"
        if (tab != Tab.Assembling && tab != Tab.Skeletons && tab != Tab.Bones)
        {
            _isBonesGroupVisible = false;
            _bonesGroupRow.Visible = false;
        }

        SwitchTab(tab);
    }

    private void SwitchTab(Tab newTab)
    {
        _currentTab = newTab;
        _rowCache.Clear();
        _iconPathCache.Clear();

        foreach (var child in _itemsList.GetChildren())
        {
            child.QueueFree();
        }

        UpdateButtonsState();
        UpdateHint();
        RefreshCurrentTab();
    }

    private void UpdateButtonsState()
    {
        // Обновляем размеры кнопок
        UpdateButtonSize(_btnTickets, _currentTab == Tab.Tickets);
        UpdateButtonSize(_btnFindings, _currentTab == Tab.Findings);
        UpdateButtonSize(_btnConcretions, _currentTab == Tab.Concretions);
        
        // Кнопка "Кости" активна, если активна любая вкладка группы
        bool isBonesGroupActive = _currentTab == Tab.Assembling || _currentTab == Tab.Skeletons || _currentTab == Tab.Bones;
        UpdateButtonSize(_btnBones, isBonesGroupActive);

        // Кнопки второго ряда
        UpdateButtonSize(_btnAssembling, _currentTab == Tab.Assembling);
        UpdateButtonSize(_btnSkeletons, _currentTab == Tab.Skeletons);
        UpdateButtonSize(_btnBonesParts, _currentTab == Tab.Bones);
    }

    private void UpdateButtonSize(Button button, bool isActive)
    {
        float targetSize = isActive ? LargeButtonSize : SmallButtonSize;
        button.CustomMinimumSize = new Vector2(targetSize, targetSize);
        button.PivotOffset = new Vector2(targetSize / 2f, targetSize / 2f);
        
        // Обновляем размер иконки внутри
        var icon = button.GetNodeOrNull<TextureRect>("TabIcon");
        if (icon != null)
        {
            float iconSize = targetSize * 0.55f;
            icon.CustomMinimumSize = new Vector2(iconSize, iconSize);
            icon.Size = new Vector2(iconSize, iconSize);
        }
    }

    private void UpdateHint()
    {
        string hintKey = _currentTab switch
        {
            Tab.Tickets => "ui.inventory.hint_tickets",
            Tab.Assembling => "ui.inventory.hint_assembling",
            Tab.Findings => "ui.inventory.hint_findings",
            Tab.Bones => "ui.inventory.hint_bones",
            Tab.Skeletons => "ui.inventory.hint_skeletons",
            Tab.Concretions => "ui.inventory.hint_concretions",
            _ => ""
        };

        string hint = LocalizationManager.Tr(hintKey);
        
        // Fallback для непереведённых ключей
        if (hint == hintKey)
        {
            hint = _currentTab switch
            {
                Tab.Tickets => "Используйте билет для начала раскопок",
                Tab.Assembling => "Соберите коллекции из найденных костей",
                Tab.Findings => "Продайте или оставьте находки для коллекции",
                Tab.Bones => "Части скелетов для сборки коллекций",
                Tab.Skeletons => "Готовые скелеты можно разместить в музее",
                Tab.Concretions => "Раскройте конкреции молотком",
                _ => ""
            };
        }

        _hintLabel.Text = hint;
    }

    private void AttachPressAnimation(Button button)
    {
        Tween currentTween = null;
        button.ButtonDown += () =>
        {
            currentTween?.Kill();
            currentTween = CreateTween();
            currentTween.TweenProperty(button, "scale", 
                new Vector2(PressedScale, PressedScale), PressAnimDuration)
                .SetTrans(Tween.TransitionType.Back)
                .SetEase(Tween.EaseType.Out);
        };
        button.ButtonUp += () =>
        {
            currentTween?.Kill();
            currentTween = CreateTween();
            currentTween.TweenProperty(button, "scale", 
                Vector2.One, ReleaseAnimDuration)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        };
    }

    private void CreateSellDialog()
    {
        _sellDialog = new SellItemDialog();
        _sellDialog.OnSellConfirmed += OnSellConfirmed;
        AddChild(_sellDialog);
    }

    private void RefreshCurrentTab()
    {
        if (_itemsList == null) return;

        try
        {
            switch (_currentTab)
            {
                case Tab.Tickets:     RenderTicketsTab(); break;
                case Tab.Assembling:  RenderAssemblingTab(); break;
                case Tab.Findings:    RenderFindingsTab(); break;
                case Tab.Bones:       RenderBonesTab(); break;
                case Tab.Skeletons:   RenderSkeletonsTab(); break;
                case Tab.Concretions: RenderConcretionsTab(); break;
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[InventoryUI] Ошибка при отрисовке: {e.Message}");
        }
    }

    // ===== ВКЛАДКА 1: SKELETONS =====
    private void RenderSkeletonsTab()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        var skeletons = allItems.Where(item => GameData.GetCollection(item.ResourceId) != null).ToList();

        int totalValue = skeletons.Sum(item => CalculateItemValue(item) * item.Amount);

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
       private void RenderAssemblingTab()
    {
        GD.Print("[AssemblingTab] 🔄 RenderAssemblingTab вызван");
        
        _hintLabel.Text = LocalizationManager.Tr("ui.inventory.hint_assembling");
        if (_hintLabel.Text == "ui.inventory.hint_assembling")
        {
            _hintLabel.Text = "Соберите коллекции из найденных костей";
        }

        var allCollections = GameData.GetAllCollections();
        GD.Print($"[AssemblingTab] Всего коллекций: {allCollections.Count}");
        
        var eligibleCollections = new List<CollectionDefinition>();

        foreach (var collection in allCollections)
        {
            bool hasAnyPiece = collection.Pieces.Any(piece => InventorySystem.Instance.GetTotalAmount(piece.Id) > 0);
            if (hasAnyPiece)
            {
                eligibleCollections.Add(collection);
            }
        }

        GD.Print($"[AssemblingTab] Коллекций с костями: {eligibleCollections.Count}");

        var sortedCollections = eligibleCollections
            .OrderByDescending(c => InventorySystem.Instance.CanAssembleCollection(c))
            .ThenBy(c => c.DisplayName)
            .ToList();

        var neededKeys = new HashSet<string>(sortedCollections.Select(c => c.Id));
        RemoveCachedRows(neededKeys);

        foreach (var collection in sortedCollections)
        {
            GD.Print($"[AssemblingTab] Обрабатываю коллекцию: {collection.Id}");
            
            if (!_rowCache.ContainsKey(collection.Id))
            {
                GD.Print($"[AssemblingTab] Создаю новую строку для: {collection.Id}");
                var row = CreateCollectionRow(collection);
                _itemsList.AddChild(row);
                _rowCache[collection.Id] = row;
            }
            
            GD.Print($"[AssemblingTab] Вызываю UpdateCollectionRow для: {collection.Id}");
            UpdateCollectionRow(_rowCache[collection.Id], collection);
        }
    }

    // ===== ВКЛАДКА 3: FINDINGS =====
    private void RenderFindingsTab()
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
    private void RenderBonesTab()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        var bones = allItems.Where(item =>
        {
            var res = GameData.GetResource(item.ResourceId);
            return res is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId);
        }).ToList();

        int totalValue = bones.Sum(item => CalculateItemValue(item) * item.Amount);

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
    private void RenderTicketsTab()
    {
        
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
    private void RenderConcretionsTab()
    {
        var allItems = InventorySystem.Instance.GetAllItems();
        var concretions = allItems.Where(item =>
        {
            var res = GameData.GetResource(item.ResourceId);
            return res is ConcretionDefinition;
        }).ToList();

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

        // === ИСПРАВЛЕНО: Фиксированный размер иконки, не растягивается ===
        var icon = new TextureRect();
        icon.Name = "IconRect";
        icon.CustomMinimumSize = new Vector2(48, 48);
        icon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        icon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        row.AddChild(icon);

        // === ИСПРАВЛЕНО: NameLabel с ограничением ширины и обрезкой текста ===
        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        nameLabel.SizeFlagsStretchRatio = 2f;
        nameLabel.ClipText = true;  // Обрезать длинный текст
        nameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimChar;
        nameLabel.AddThemeConstantOverride("outline_size", 0);
        row.AddChild(nameLabel);

        // === ИСПРАВЛЕНО: AmountLabel с фиксированной шириной ===
        var amountLabel = new Label();
        amountLabel.Name = "AmountLabel";
        amountLabel.AddThemeFontSizeOverride("font_size", 12);
        amountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        amountLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        row.AddChild(amountLabel);

        // === ИСПРАВЛЕНО: Кнопка с фиксированной шириной, не сжимается ===
        var openButton = new Button();
        openButton.Name = "OpenButton";
        openButton.Text = "🔨 " + LocalizationManager.Tr("ui.inventory.open");
        if (openButton.Text == "🔨 ui.inventory.open") openButton.Text = "🔨 Раскрыть";
        openButton.CustomMinimumSize = new Vector2(110, 0);
        openButton.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;  // Не растягивается
        openButton.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
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
            string iconPath = GetCachedIconPath(item.ResourceId);
            if (!string.IsNullOrEmpty(iconPath) && ResourceLoader.Exists(iconPath))
            {
                icon.Texture = GD.Load<Texture2D>(iconPath);
                icon.Visible = true;
                
                // === ИСПРАВЛЕНО: Принудительно ограничиваем размер текстуры ===
                icon.CustomMinimumSize = new Vector2(48, 48);
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

        // Иконка предмета
        var iconRect = new TextureRect();
        iconRect.Name = "IconRect";
        iconRect.CustomMinimumSize = new Vector2(40, 40);
        iconRect.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
        iconRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        row.AddChild(iconRect);

        // === ИСПРАВЛЕНО: Название растягивается и обрезается ===
        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;  // ← Растягивается
        nameLabel.ClipText = true;  // ← Обрезает длинный текст
        nameLabel.TextOverrunBehavior = TextServer.OverrunBehavior.TrimChar;
        row.AddChild(nameLabel);

        // Количество
        var amountLabel = new Label();
        amountLabel.Name = "AmountLabel";
        amountLabel.AddThemeFontSizeOverride("font_size", 12);
        amountLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        amountLabel.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        row.AddChild(amountLabel);

        // === НОВОЕ: Кнопка "Разместить" ПЕРЕД ценой (только для коллекций) ===
        if (isCollection)
        {
            var placeBtn = new Button();
            placeBtn.Name = "PlaceButton";
            placeBtn.Text = LocalizationManager.Tr("ui.inventory.place");
            placeBtn.CustomMinimumSize = new Vector2(120, 0);
            placeBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
            placeBtn.Pressed += () => OnPlaceCollectionPressed((string)row.GetMeta("resource_id"));
            row.AddChild(placeBtn);
        }

        // === ИСПРАВЛЕНО: Цена с иконкой монеты вместо смайлика ===
        var priceContainer = new HBoxContainer();
        priceContainer.Name = "PriceContainer";
        priceContainer.AddThemeConstantOverride("separation", 4);
        priceContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        row.AddChild(priceContainer);

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
        priceLabel.AddThemeFontSizeOverride("font_size", 12);
        priceLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        priceLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        priceContainer.AddChild(priceLabel);

        // Кнопка "Продать" (всегда справа)
        var sellBtn = new Button();
        sellBtn.Name = "SellButton";
        sellBtn.Text = LocalizationManager.Tr("ui.inventory.sell");
        sellBtn.CustomMinimumSize = new Vector2(90, 0);
        sellBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
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
            string iconPath = GetCachedIconPath(item.ResourceId);
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

        // === ИСПРАВЛЕНО: Цена без смайлика (иконка монеты уже в PriceContainer) ===
        var priceLabel = row.GetNodeOrNull<Label>("PriceContainer/PriceLabel");
        if (priceLabel != null)
        {
            int price = CalculateItemValue(item) * item.Amount;
            priceLabel.Text = price.ToString("N0");  // Формат: 1 234
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

        if (resource is FossilDefinition standaloneFossil && standaloneFossil.VariantCount > 1)
    {
        // Для инвентаря всегда показываем первый вариант
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
            _needsRefresh = true;
            GD.Print($"[InventoryUI] Продано {amount}x {resourceId} за {earned} монет");
        }
    }

    public void RequestRefresh()
    {
        _needsRefresh = true;
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
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        // === ВАЖНО: Устанавливаем имя для поиска ===
        var leftPanel = new VBoxContainer();
        leftPanel.Name = "LeftPanel";  // ← Имя для поиска
        leftPanel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        leftPanel.AddThemeConstantOverride("separation", 5);
        row.AddChild(leftPanel);

        var nameLabel = new Label();
        nameLabel.Name = "NameLabel";  // ← Имя для поиска
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        nameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        leftPanel.AddChild(nameLabel);

        var iconsRow = new HBoxContainer();
        iconsRow.Name = "IconsRow";  // ← Имя для поиска
        iconsRow.AddThemeConstantOverride("separation", 15);
        leftPanel.AddChild(iconsRow);

        var assembledIcon = new TextureRect();
        assembledIcon.Name = "AssembledIcon";  // ← Имя для поиска
        assembledIcon.CustomMinimumSize = new Vector2(128, 128);
        assembledIcon.CustomMaximumSize = new Vector2(128, 128);
        assembledIcon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        assembledIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        iconsRow.AddChild(assembledIcon);

        var arrowLabel = new Label();
        arrowLabel.Name = "ArrowLabel";  // ← Имя для поиска
        arrowLabel.Text = "←";
        arrowLabel.AddThemeFontSizeOverride("font_size", 24);
        arrowLabel.VerticalAlignment = VerticalAlignment.Center;
        iconsRow.AddChild(arrowLabel);

        var piecesContainer = new HBoxContainer();
        piecesContainer.Name = "PiecesContainer";  // ← Имя для поиска
        piecesContainer.AddThemeConstantOverride("separation", 10);
        iconsRow.AddChild(piecesContainer);

        for (int i = 0; i < collection.Pieces.Count; i++)
        {
            var pieceSlot = new VBoxContainer();
            pieceSlot.Name = $"PieceSlot_{i}";  // ← Имя для поиска
            pieceSlot.Alignment = BoxContainer.AlignmentMode.Center;

            var iconRect = new TextureRect();
            iconRect.Name = $"PieceIcon_{i}";  // ← Имя для поиска
            iconRect.CustomMinimumSize = new Vector2(32, 32);
            iconRect.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            iconRect.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            iconRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
            pieceSlot.AddChild(iconRect);

            var countLabel = new Label();
            countLabel.Name = $"PieceCount_{i}";  // ← Имя для поиска
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

        // === Кнопка "Собрать" справа ===
        var assembleBtn = new Button();
        assembleBtn.Name = "AssembleButton";  // ← Имя для поиска
        assembleBtn.Text = LocalizationManager.Tr("ui.inventory.assemble");
        assembleBtn.CustomMinimumSize = new Vector2(80, 0);
        assembleBtn.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        assembleBtn.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        assembleBtn.Pressed += () => OnAssemblePressed(collection);
        row.AddChild(assembleBtn);

        return row;
    }

       private void UpdateCollectionRow(Control row, CollectionDefinition collection)
    {
        var nameLabel = row.GetNodeOrNull<Label>("LeftPanel/NameLabel");
        if (nameLabel != null) nameLabel.Text = collection.DisplayName;

        var arrowLabel = row.GetNodeOrNull<Label>("LeftPanel/IconsRow/ArrowLabel");
        bool canAssemble = InventorySystem.Instance.CanAssembleCollection(collection);
        if (arrowLabel != null)
        {
            arrowLabel.Modulate = canAssemble ? new Color(0.4f, 1f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
        }

        var assembledIcon = row.GetNodeOrNull<TextureRect>("LeftPanel/IconsRow/AssembledIcon");
        if (assembledIcon != null)
        {
            string collectionIconPath = GetCachedIconPath(collection.Id);
            GD.Print($"[CollectionRow] Иконка коллекции '{collection.Id}': {collectionIconPath}");
            
            if (!string.IsNullOrEmpty(collectionIconPath) && ResourceLoader.Exists(collectionIconPath))
            {
                assembledIcon.Texture = GD.Load<Texture2D>(collectionIconPath);
                assembledIcon.Visible = true;
            }
            else
            {
                assembledIcon.Visible = false;
                GD.PrintErr($"[CollectionRow] ❌ Иконка коллекции не найдена: {collectionIconPath}");
            }
        }

        for (int i = 0; i < collection.Pieces.Count; i++)
        {
            var piece = collection.Pieces[i];

            var iconRect = row.GetNodeOrNull<TextureRect>($"LeftPanel/IconsRow/PiecesContainer/PieceSlot_{i}/PieceIcon_{i}");
            if (iconRect != null)
            {
                string partIconPath = GetCachedIconPath(piece.Id);
                if (!string.IsNullOrEmpty(partIconPath) && ResourceLoader.Exists(partIconPath))
                {
                    iconRect.Texture = GD.Load<Texture2D>(partIconPath);
                    iconRect.Visible = true;
                }
                else
                {
                    iconRect.Visible = false;
                    GD.PrintErr($"[CollectionRow] ❌ Иконка кости '{piece.Id}' не найдена: {partIconPath}");
                }
            }

            var countLabel = row.GetNodeOrNull<Label>($"LeftPanel/IconsRow/PiecesContainer/PieceSlot_{i}/PieceCount_{i}");
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
             _needsRefresh = true;
        }
        else
        {
            GD.PrintErr("[UI] Не удалось собрать коллекцию.");
        }
    }

     private string GetCachedIconPath(string resourceId)
    {
        if (!_iconPathCache.TryGetValue(resourceId, out var path))
        {
            path = GetResourceIconPath(resourceId);
            _iconPathCache[resourceId] = path;
        }
        return path;
    }

        /// <summary>
    /// Вычисляет размеры кнопок в зависимости от доступной ширины экрана.
    /// </summary>
    private void CalculateButtonSizes()
    {
        float screenWidth = GetViewport().GetVisibleRect().Size.X;
        
        // Отступы панели: 5% слева и справа
        float panelWidth = screenWidth * 0.90f;
        
        // Внутренние отступы панели
        float availableWidth = panelWidth - PanelPadding * 2;
        
        // Промежутки между кнопками
        float spacing = ButtonSpacing;
        
        // === Первый ряд: 4 кнопки (3 неактивных + 1 активная = коэффициент 1.3) ===
        float mainRowWidth = availableWidth - 3 * spacing;
        float mainSmallSize = mainRowWidth / 4.3f;  // 3*1 + 1*1.3
        
        // === Второй ряд: 3 кнопки (2 неактивных + 1 активная = коэффициент 1.3) ===
        float subRowWidth = availableWidth - 2 * spacing;
        float subSmallSize = subRowWidth / 3.3f;  // 2*1 + 1*1.3
        
        // Используем минимальный размер, чтобы оба ряда помещались
        SmallButtonSize = Mathf.Min(mainSmallSize, subSmallSize);
        LargeButtonSize = SmallButtonSize * 1.3f;
        
        GD.Print($"[InventoryUI] Экран: {screenWidth}, кнопки: small={SmallButtonSize:F0}, large={LargeButtonSize:F0}");
    }
}