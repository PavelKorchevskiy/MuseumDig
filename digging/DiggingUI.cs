using Godot;
using System.Collections.Generic;

public partial class DiggingUI : Control
{
    public static DiggingUI Instance { get; private set; }
    
    public WorkerType? SelectedTool { get; private set; }

    private Button _diggerButton;
    private Button _extractorButton;
    private Button _excavatorButton;
    
    // === НОВОЕ: Ссылки на Labels внутри кнопок для обновления текста ===
    private Label _diggerLabel;
    private Label _extractorLabel;
    private Label _excavatorLabel;

    private StyleBoxFlat _normalStyle;
    private StyleBoxFlat _selectedStyle;
    private StyleBoxFlat _disabledStyle;

    public override void _Ready()
    {
        Instance = this;
        
        MouseFilter = MouseFilterEnum.Ignore;
        
        _normalStyle = new StyleBoxFlat();
        _normalStyle.BgColor = new Color(0.2f, 0.2f, 0.25f);
        _normalStyle.CornerRadiusTopLeft = 8;
        _normalStyle.CornerRadiusTopRight = 8;
        _normalStyle.CornerRadiusBottomLeft = 8;
        _normalStyle.CornerRadiusBottomRight = 8;
        _normalStyle.BorderWidthLeft = 2;
        _normalStyle.BorderWidthTop = 2;
        _normalStyle.BorderWidthRight = 2;
        _normalStyle.BorderWidthBottom = 2;
        _normalStyle.BorderColor = new Color(0.4f, 0.4f, 0.4f);

        _selectedStyle = _normalStyle.Duplicate() as StyleBoxFlat;
        _selectedStyle.BgColor = new Color(0.3f, 0.5f, 0.3f);
        _selectedStyle.BorderColor = new Color(0.5f, 1.0f, 0.5f);

        _disabledStyle = _normalStyle.Duplicate() as StyleBoxFlat;
        _disabledStyle.BgColor = new Color(0.15f, 0.15f, 0.15f);
        _disabledStyle.BorderColor = new Color(0.3f, 0.3f, 0.3f);

        CreatePanel();
    }

    private void CreatePanel()
    {
        var panelContainer = new PanelContainer();
        panelContainer.AnchorLeft = 0;
        panelContainer.AnchorRight = 1;
        panelContainer.AnchorBottom = 1;
        panelContainer.AnchorTop = 1;
        panelContainer.OffsetTop = -90;
        panelContainer.MouseFilter = MouseFilterEnum.Ignore;
        
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        panelStyle.CornerRadiusTopLeft = 12;
        panelStyle.CornerRadiusTopRight = 12;
        panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panelContainer);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", 20);
        panelContainer.AddChild(hbox);

        // === Кнопка Копателя ===
         // === Кнопка Копателя ===
        CreateToolButton(
            iconPath: "res://assets/dig/buttons/shovel.png",
            textKey: "ui.digging.digger",
            price: null,
            out _diggerButton,
            out _diggerLabel
        );
        _diggerButton.Pressed += () => SelectTool(WorkerType.Digger);
        hbox.AddChild(_diggerButton);

        // === Кнопка Извлекателя ===
        CreateToolButton(
            iconPath: "res://assets/dig/buttons/pickaxe.png",
            textKey: "ui.digging.extractor",
            price: null,
            out _extractorButton,
            out _extractorLabel
        );
        _extractorButton.Pressed += () => SelectTool(WorkerType.Extractor);
        hbox.AddChild(_extractorButton);

        // === Кнопка Экскаватора ===
        CreateToolButton(
            iconPath: "res://assets/dig/buttons/bulldozer.png",
            textKey: "ui.digging.excavator",
            price: 100,
            out _excavatorButton,
            out _excavatorLabel
        );
        _excavatorButton.Pressed += () => SelectTool(WorkerType.Excavator);
        hbox.AddChild(_excavatorButton);

        UpdateButtons();
    }

    /// <summary>
    /// Создаёт кнопку инструмента с центрированным изображением и текстом.
    /// </summary>
    private void CreateToolButton(
        string iconPath, 
        string textKey,
        int? price,
        out Button button,
        out Label label)
    {
        button = new Button();
        button.CustomMinimumSize = new Vector2(140, 70);
        
        // === ВАЖНО: Контейнер растягивается на ВСЮ кнопку ===
        var vbox = new VBoxContainer();
        vbox.Alignment = BoxContainer.AlignmentMode.Center;  // Контент по центру вертикально
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; // Заполняет всю ширину
        vbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;   // Заполняет всю высоту
        vbox.AddThemeConstantOverride("separation", 2);
        vbox.MouseFilter = MouseFilterEnum.Ignore; // Пропускаем клики к кнопке
        
        // === Иконка ===
        var icon = new TextureRect();
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize; // Фиксированный размер
        icon.CustomMinimumSize = new Vector2(40, 32);
        icon.Size = new Vector2(40, 32);
        icon.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter; // Центрируем по горизонтали
        icon.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        icon.MouseFilter = MouseFilterEnum.Ignore;
        
        if (ResourceLoader.Exists(iconPath))
        {
            icon.Texture = GD.Load<Texture2D>(iconPath);
        }
        vbox.AddChild(icon);
        
        // === Текст ===
        label = new Label();
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; // Заполняет ширину
        label.AddThemeFontSizeOverride("font_size", 12);
        label.MouseFilter = MouseFilterEnum.Ignore;
        
        string baseText = LocalizationManager.Tr(textKey);
        label.Text = price.HasValue ? $"{baseText}\n🪙{price.Value}" : baseText;
        
        vbox.AddChild(label);
        
        button.AddChild(vbox);
        
        // === ВАЖНО: очищаем встроенный текст и убираем content margin ===
        button.Text = "";
        
        // Делаем все стили с одинаковыми симметричными отступами
        UpdateButtonMargins(_normalStyle);
        UpdateButtonMargins(_selectedStyle);
        UpdateButtonMargins(_disabledStyle);
        
        button.AddThemeStyleboxOverride("normal", _normalStyle);
        button.AddThemeStyleboxOverride("hover", _normalStyle);
        button.AddThemeStyleboxOverride("pressed", _normalStyle);
        button.AddThemeStyleboxOverride("focus", _normalStyle);
    }

    /// <summary>
    /// Устанавливает симметричные отступы для стиля кнопки,
    /// чтобы содержимое было идеально по центру.
    /// </summary>
    private void UpdateButtonMargins(StyleBoxFlat style)
    {
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 6;
        style.ContentMarginBottom = 6;
    }

    public override void _Process(double delta)
    {
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (_diggerButton == null || _extractorButton == null || _excavatorButton == null) return;

        // === Копатель ===
        int activeDiggers = DiggingManager.Instance.GetActiveDiggersCount();
        int maxDiggers = DiggingManager.Instance.GetMaxDiggers();
        _diggerLabel.Text = $"{LocalizationManager.Tr("ui.digging.digger")}\n({activeDiggers}/{maxDiggers})";
        _diggerButton.Disabled = (activeDiggers >= maxDiggers) && SelectedTool != WorkerType.Digger;

        // === Извлекатель ===
        int activeExtractors = DiggingManager.Instance.GetActiveExtractorsCount();
        int maxExtractors = DiggingManager.Instance.GetMaxExtractors();
        _extractorLabel.Text = $"{LocalizationManager.Tr("ui.digging.extractor")}\n({activeExtractors}/{maxExtractors})";
        _extractorButton.Disabled = (activeExtractors >= maxExtractors) && SelectedTool != WorkerType.Extractor;

        // === Экскаватор ===
        bool excavatorUnlocked = DiggingManager.Instance.IsExcavatorUnlocked();
        if (excavatorUnlocked)
        {
            _excavatorLabel.Text = $"{LocalizationManager.Tr("ui.digging.excavator")}\n🪙100";
        }
        else
        {
            _excavatorLabel.Text = $"🔒 {LocalizationManager.Tr("ui.digging.excavator")}";
        }
        _excavatorButton.Disabled = !excavatorUnlocked;

        UpdateButtonStyle(_diggerButton, SelectedTool == WorkerType.Digger);
        UpdateButtonStyle(_extractorButton, SelectedTool == WorkerType.Extractor);
        UpdateButtonStyle(_excavatorButton, SelectedTool == WorkerType.Excavator);
    }

    private void UpdateButtonStyle(Button button, bool isSelected)
    {
        var style = isSelected ? _selectedStyle : _normalStyle;
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
    }

    public void SelectTool(WorkerType tool)
    {
        if (SelectedTool == tool)
        {
            SelectedTool = null;
            GD.Print($"[DiggingUI] Инструмент {tool} снят с выбора");
        }
        else
        {
            SelectedTool = tool;
            GD.Print($"[DiggingUI] Выбран инструмент: {tool}");
        }
        UpdateButtons();
    }

    public void ClearSelection()
    {
        SelectedTool = null;
        UpdateButtons();
        var digSite = GetParent();
        if (digSite != null)
        {
            foreach (var child in digSite.GetChildren())
            {
                if (child is Tile tile)
                {
                    tile.Modulate = Colors.White;
                }
            }
        }
    }
}