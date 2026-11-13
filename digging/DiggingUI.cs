using Godot;
using System.Collections.Generic;

public partial class DiggingUI : Control
{
    public static DiggingUI Instance { get; private set; }
    
    public WorkerType? SelectedTool { get; private set; }

    private Button _diggerButton;
    private Button _extractorButton;
    private Button _excavatorButton;
    
    // Подпись под картинкой экскаватора: цена или замок (счётчика у него нет)
    private Label _excavatorLabel;

    /// <summary>
    /// Индикатор рабочих внизу кнопки: по одной полоске на каждого.
    /// Зелёная — свободен, жёлтая — сейчас работает.
    /// </summary>
    private class WorkerBars
    {
        public HBoxContainer Row;
        public int Total = -1;
        public int Busy = -1;
    }

    private readonly Dictionary<WorkerType, WorkerBars> _workerBars = new();

    private static readonly Color BarFreeColor = new Color(0.30f, 0.85f, 0.35f);
    private static readonly Color BarBusyColor = new Color(1.00f, 0.80f, 0.15f);
    private const float BarHeight = 9f;

    /// <summary>
    /// Картинка на кнопке: кадры той же анимации, что рабочий проигрывает на
    /// плитке. Пока инструмент не выбран — стоит первый кадр, у выбранного
    /// анимация крутится по кругу, так что сразу видно, что сейчас в руках.
    /// </summary>
    private class ToolIcon
    {
        public TextureRect Rect;
        public List<Texture2D> Frames = new();
        public float Fps = 8f;
        public float Time = 0f;
        public int ShownFrame = -1;
    }

    private readonly Dictionary<WorkerType, ToolIcon> _toolIcons = new();

    private const int ToolFrameCount = 8;

    private StyleBoxFlat _normalStyle;
    private StyleBoxFlat _selectedStyle;
    private StyleBoxFlat _disabledStyle;

    public override void _Ready()
    {
        Instance = this;
        
        MouseFilter = MouseFilterEnum.Ignore;
        
        // Цвета — из общей мобильной палитры (MobileUI), чтобы выглядело
        // так же, как остальной интерфейс игры.
        _normalStyle = new StyleBoxFlat();
        _normalStyle.BgColor = MobileUI.NeutralColor;
        _normalStyle.CornerRadiusTopLeft = 12;
        _normalStyle.CornerRadiusTopRight = 12;
        _normalStyle.CornerRadiusBottomLeft = 12;
        _normalStyle.CornerRadiusBottomRight = 12;
        _normalStyle.BorderWidthLeft = 2;
        _normalStyle.BorderWidthTop = 2;
        _normalStyle.BorderWidthRight = 2;
        _normalStyle.BorderWidthBottom = 2;
        _normalStyle.BorderColor = MobileUI.RowBorderColor;

        _selectedStyle = _normalStyle.Duplicate() as StyleBoxFlat;
        _selectedStyle.BgColor = MobileUI.PrimaryColor;
        _selectedStyle.BorderColor = MobileUI.PrimaryColor.Lightened(0.4f);

        _disabledStyle = _normalStyle.Duplicate() as StyleBoxFlat;
        _disabledStyle.BgColor = new Color(0.22f, 0.22f, 0.28f);
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
        panelContainer.OffsetTop = -104; // чуть выше: кнопки стали крупнее под палец
        panelContainer.MouseFilter = MouseFilterEnum.Ignore;
        
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        panelStyle.CornerRadiusTopLeft = 12;
        panelStyle.CornerRadiusTopRight = 12;
        panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panelContainer);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.AddThemeConstantOverride("separation", 14);
        panelContainer.AddChild(hbox);

        // === Кнопка Копателя ===
         // === Кнопка Копателя ===
        CreateToolButton(
            tool: WorkerType.Digger,
            framesFolder: "res://assets/dig/digger",
            fps: 8f,
            iconPath: "res://assets/dig/buttons/shovel.png",
            textKey: "ui.digging.digger",
            out _diggerButton
        );
        _diggerButton.Pressed += () => SelectTool(WorkerType.Digger);
        hbox.AddChild(_diggerButton);

        // === Кнопка Извлекателя ===
        CreateToolButton(
            tool: WorkerType.Extractor,
            framesFolder: "res://assets/dig/extracter",
            fps: 8f,
            iconPath: "res://assets/dig/buttons/pickaxe.png",
            textKey: "ui.digging.extractor",
            out _extractorButton
        );
        _extractorButton.Pressed += () => SelectTool(WorkerType.Extractor);
        hbox.AddChild(_extractorButton);

        // === Кнопка Экскаватора ===
        CreateToolButton(
            tool: WorkerType.Excavator,
            framesFolder: "res://assets/dig/bulldozer",
            fps: 12f,
            iconPath: "res://assets/dig/buttons/bulldozer.png",
            textKey: "ui.digging.excavator",
            out _excavatorButton
        );
        _excavatorButton.Pressed += () => SelectTool(WorkerType.Excavator);
        hbox.AddChild(_excavatorButton);

        TutorialTargets.Register("dig.tool.digger", _diggerButton);
        TutorialTargets.Register("dig.tool.extractor", _extractorButton);
        TutorialTargets.Register("dig.tool.excavator", _excavatorButton);

        UpdateButtons();
    }

    /// <summary>
    /// Создаёт кнопку инструмента: картинка рабочего (кадры его анимации из
    /// framesFolder), а под ней — индикатор: полоски рабочих у копателя и
    /// извлекателя, цена у экскаватора. Названия на кнопке нет.
    /// </summary>
    /// <param name="iconPath">Запасная статичная иконка, если кадров анимации нет.</param>
    /// <param name="textKey">Название инструмента — для всплывающей подсказки.</param>
    private void CreateToolButton(
        WorkerType tool,
        string framesFolder,
        float fps,
        string iconPath, 
        string textKey,
        out Button button)
    {
        button = new Button();
        button.CustomMinimumSize = new Vector2(150, 84);
        button.FocusMode = Control.FocusModeEnum.None;
        button.TooltipText = LocalizationManager.Tr(textKey);
        
        // Контейнер растягивается на всю кнопку
        var vbox = new VBoxContainer();
        vbox.SetAnchorsPreset(LayoutPreset.FullRect);
        vbox.OffsetLeft = 10;
        vbox.OffsetRight = -10;
        vbox.OffsetTop = 5;
        vbox.OffsetBottom = -7;
        vbox.AddThemeConstantOverride("separation", 3);
        vbox.MouseFilter = MouseFilterEnum.Ignore; // Пропускаем клики к кнопке
        
        // === Картинка рабочего: занимает всё место над индикатором ===
        var icon = new TextureRect();
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        icon.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        icon.MouseFilter = MouseFilterEnum.Ignore;

        var toolIcon = new ToolIcon { Rect = icon, Fps = fps };
        for (int i = 1; i <= ToolFrameCount; i++)
        {
            string framePath = $"{framesFolder}/1_{i}.png";
            if (ResourceLoader.Exists(framePath))
            {
                toolIcon.Frames.Add(GD.Load<Texture2D>(framePath));
            }
        }

        if (toolIcon.Frames.Count > 0)
        {
            icon.Texture = toolIcon.Frames[0];
            toolIcon.ShownFrame = 0;
        }
        else if (ResourceLoader.Exists(iconPath))
        {
            icon.Texture = GD.Load<Texture2D>(iconPath);
        }
        _toolIcons[tool] = toolIcon;
        vbox.AddChild(icon);

        // === Индикатор внизу кнопки ===
        if (tool == WorkerType.Excavator)
        {
            _excavatorLabel = new Label();
            _excavatorLabel.HorizontalAlignment = HorizontalAlignment.Center;
            _excavatorLabel.AddThemeFontSizeOverride("font_size", 14);
            _excavatorLabel.AddThemeColorOverride("font_color", Colors.Gold);
            _excavatorLabel.MouseFilter = MouseFilterEnum.Ignore;
            vbox.AddChild(_excavatorLabel);
        }
        else
        {
            var row = new HBoxContainer();
            row.CustomMinimumSize = new Vector2(0, BarHeight);
            row.AddThemeConstantOverride("separation", 4);
            row.MouseFilter = MouseFilterEnum.Ignore;
            vbox.AddChild(row);
            _workerBars[tool] = new WorkerBars { Row = row };
        }
        
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
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        // Та же анимация нажатия ("сжатие" кнопки), что у остальных мобильных кнопок игры
        MobileUI.AttachPressAnimation(button);
    }

    /// <summary>
    /// Устанавливает симметричные отступы для стиля кнопки,
    /// чтобы содержимое было идеально по центру.
    /// </summary>
    private void UpdateButtonMargins(StyleBoxFlat style)
    {
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
    }

    public override void _Process(double delta)
    {
        UpdateButtons();
        AnimateToolIcons((float)delta);
    }

    /// <summary>
    /// Крутит анимацию на кнопке выбранного инструмента; остальные кнопки
    /// возвращает на первый кадр.
    /// </summary>
    private void AnimateToolIcons(float delta)
    {
        foreach (var kvp in _toolIcons)
        {
            var icon = kvp.Value;
            if (icon.Frames.Count == 0) continue;

            int frame = 0;
            if (SelectedTool == kvp.Key)
            {
                icon.Time += delta;
                frame = (int)(icon.Time * icon.Fps) % icon.Frames.Count;
            }
            else
            {
                icon.Time = 0f;
            }

            if (frame != icon.ShownFrame)
            {
                icon.ShownFrame = frame;
                icon.Rect.Texture = icon.Frames[frame];
            }
        }
    }

    private void UpdateButtons()
    {
        if (_diggerButton == null || _extractorButton == null || _excavatorButton == null) return;

        // === Копатель ===
        int activeDiggers = DiggingManager.Instance.GetActiveDiggersCount();
        int maxDiggers = DiggingManager.Instance.GetMaxDiggers();
        UpdateWorkerBars(WorkerType.Digger, activeDiggers, maxDiggers);
        _diggerButton.Disabled = (activeDiggers >= maxDiggers) && SelectedTool != WorkerType.Digger;

        // === Извлекатель ===
        int activeExtractors = DiggingManager.Instance.GetActiveExtractorsCount();
        int maxExtractors = DiggingManager.Instance.GetMaxExtractors();
        UpdateWorkerBars(WorkerType.Extractor, activeExtractors, maxExtractors);
        _extractorButton.Disabled = (activeExtractors >= maxExtractors) && SelectedTool != WorkerType.Extractor;

        // === Экскаватор: цена или замок ===
        bool excavatorUnlocked = DiggingManager.Instance.IsExcavatorUnlocked();
        _excavatorLabel.Text = excavatorUnlocked ? "🪙100" : "🔒";
        _excavatorButton.Disabled = !excavatorUnlocked;

        // Затемняем картинку, когда инструмент недоступен: нет свободных
        // рабочих (и он не выбран) либо экскаватор ещё закрыт.
        SetIconDimmed(WorkerType.Digger, _diggerButton.Disabled);
        SetIconDimmed(WorkerType.Extractor, _extractorButton.Disabled);
        SetIconDimmed(WorkerType.Excavator, _excavatorButton.Disabled);

        UpdateButtonStyle(_diggerButton, SelectedTool == WorkerType.Digger);
        UpdateButtonStyle(_extractorButton, SelectedTool == WorkerType.Extractor);
        UpdateButtonStyle(_excavatorButton, SelectedTool == WorkerType.Excavator);
    }

    /// <summary>
    /// Перекрашивает полоски рабочих: слева зелёные (свободные), справа жёлтые
    /// (работают). Если изменилось общее число рабочих (выкуплен навык) —
    /// полоски пересоздаются. Ничего не делает, если числа прежние.
    /// </summary>
    private void UpdateWorkerBars(WorkerType tool, int busy, int total)
    {
        if (!_workerBars.TryGetValue(tool, out var bars)) return;

        total = Mathf.Max(total, 0);
        busy = Mathf.Clamp(busy, 0, total);
        if (bars.Total == total && bars.Busy == busy) return;

        if (bars.Total != total)
        {
            foreach (var child in bars.Row.GetChildren())
            {
                bars.Row.RemoveChild(child);
                child.QueueFree();
            }

            for (int i = 0; i < total; i++)
            {
                var segment = new Panel();
                segment.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                segment.CustomMinimumSize = new Vector2(0, BarHeight);
                segment.MouseFilter = MouseFilterEnum.Ignore;
                bars.Row.AddChild(segment);
            }
        }

        int free = total - busy;
        for (int i = 0; i < total; i++)
        {
            var style = new StyleBoxFlat { BgColor = i < free ? BarFreeColor : BarBusyColor };
            style.SetCornerRadiusAll(4);
            style.SetBorderWidthAll(1);
            style.BorderColor = new Color(0f, 0f, 0f, 0.45f);
            ((Panel)bars.Row.GetChild(i)).AddThemeStyleboxOverride("panel", style);
        }

        bars.Total = total;
        bars.Busy = busy;
    }

    private static readonly Color IconDimColor = new Color(0.38f, 0.38f, 0.42f, 1f);

    private void SetIconDimmed(WorkerType tool, bool dimmed)
    {
        if (!_toolIcons.TryGetValue(tool, out var icon)) return;

        Color target = dimmed ? IconDimColor : Colors.White;
        if (icon.Rect.Modulate != target) icon.Rect.Modulate = target;
    }

    private void UpdateButtonStyle(Button button, bool isSelected)
    {
        // Недоступная кнопка — тёмный фон, чтобы отличалась и без картинки
        var style = isSelected ? _selectedStyle : (button.Disabled ? _disabledStyle : _normalStyle);
        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("disabled", _disabledStyle);
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