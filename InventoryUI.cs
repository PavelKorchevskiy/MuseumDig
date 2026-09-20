using Godot;
using System.Collections.Generic;
using System.Linq;
using System;

public partial class InventoryUI : CanvasLayer
{
    public static InventoryUI _instance;

    private enum Tab { Skeletons, Assembling, Findings, Bones, Tickets }
    private Tab _currentTab = Tab.Skeletons;

    private Label _totalValueLabel;
    private VBoxContainer _itemsList;
    private Button _closeButton;

    private Button _tabSkeletonsButton;
    private Button _tabAssemblingButton;
    private Button _tabFindingsButton;
    private Button _tabBonesButton;
    private Button _tabTicketsButton;

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

        _totalValueLabel = GetNodeOrNull<Label>("MainPanel/Content/TotalValueLabel");
        _itemsList = GetNodeOrNull<VBoxContainer>("MainPanel/Content/ScrollContainer/ItemsList");
        _closeButton = GetNodeOrNull<Button>("MainPanel/Content/ButtonsRow/CloseButton");

        if (_itemsList == null)
        {
            GD.PrintErr("[InventoryUI] КРИТИЧЕСКАЯ ОШИБКА: _itemsList не найден!");
            return;
        }

        CreateTabs();
        CreateSellDialog();

        _closeButton.Pressed += OnClosePressed;
    }

    private void CreateTabs()
    {
        var contentContainer = GetNode("MainPanel/Content") as VBoxContainer;
        if (contentContainer == null) return;

        var tabsContainer = new HBoxContainer();
        tabsContainer.Name = "TabsContainer";
        tabsContainer.AddThemeConstantOverride("separation", 5);

        _tabSkeletonsButton = CreateTabButton("ui.inventory.skeletons", Tab.Skeletons);
        _tabAssemblingButton = CreateTabButton("ui.inventory.assembling", Tab.Assembling);
        _tabFindingsButton = CreateTabButton("ui.inventory.findings", Tab.Findings);
        _tabBonesButton = CreateTabButton("ui.inventory.bones", Tab.Bones);
        _tabTicketsButton = CreateTabButton("ui.inventory.tickets", Tab.Tickets);

        tabsContainer.AddChild(_tabSkeletonsButton);
        tabsContainer.AddChild(_tabAssemblingButton);
        tabsContainer.AddChild(_tabFindingsButton);
        tabsContainer.AddChild(_tabBonesButton);
        tabsContainer.AddChild(_tabTicketsButton);

        contentContainer.AddChild(tabsContainer);
        contentContainer.MoveChild(tabsContainer, 1);

        SwitchTab(Tab.Skeletons);
    }

    private Button CreateTabButton(string localizationKey, Tab tab)
    {
        var button = new Button();
        button.Text = LocalizationManager.Tr(localizationKey);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
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
            return res != null &&
                   GameData.GetCollection(item.ResourceId) == null && // Не коллекция
                   !(res is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId)); // Не часть коллекции
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
        _totalValueLabel.Text = LocalizationManager.Tr("ui.inventory.empty_tickets");

        var neededKeys = new HashSet<string>();
        RemoveCachedRows(neededKeys);
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
        var collection = GameData.GetCollection(resourceId);
        if (collection != null) return collection.TexturePath;

        var resource = GameData.GetResource(resourceId);
        if (resource is FossilDefinition fossil && !string.IsNullOrEmpty(fossil.CollectionId))
        {
            string partName = fossil.Id.Replace($"{fossil.CollectionId}_", "");
            return $"res://assets/museum/items/{fossil.CollectionId}/{partName}.png";
        }

        string commonPath = $"res://assets/museum/items/common/{resourceId}.png";
        if (ResourceLoader.Exists(commonPath)) return commonPath;

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

    private void OnClosePressed()
    {
        Visible = false;
    }

    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
    }

    // ===== КОЛЛЕКЦИИ (из старого кода) =====
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