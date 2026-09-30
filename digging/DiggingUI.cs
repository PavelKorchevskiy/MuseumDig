using Godot;
using System.Collections.Generic;

public partial class DiggingUI : Control
{
    public static DiggingUI Instance { get; private set; }
    
    public WorkerType? SelectedTool { get; private set; }

    private Button _diggerButton;
    private Button _extractorButton;
    private Button _excavatorButton;

    private StyleBoxFlat _normalStyle;
    private StyleBoxFlat _selectedStyle;
    private StyleBoxFlat _disabledStyle;

    public override void _Ready()
    {
        Instance = this;
        
        // Делаем панель некликабельной (чтобы не блокировать плитки)
        MouseFilter = MouseFilterEnum.Ignore;
        
        // Создаём стили для кнопок
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
        // Панель инструментов внизу экрана
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
        _diggerButton = new Button();
        _diggerButton.CustomMinimumSize = new Vector2(140, 60);
        _diggerButton.AddThemeStyleboxOverride("normal", _normalStyle);
        _diggerButton.AddThemeStyleboxOverride("hover", _normalStyle);
        _diggerButton.AddThemeStyleboxOverride("pressed", _normalStyle);
        _diggerButton.Pressed += () => SelectTool(WorkerType.Digger);
        hbox.AddChild(_diggerButton);

        // === Кнопка Извлекателя ===
        _extractorButton = new Button();
        _extractorButton.CustomMinimumSize = new Vector2(140, 60);
        _extractorButton.AddThemeStyleboxOverride("normal", _normalStyle);
        _extractorButton.AddThemeStyleboxOverride("hover", _normalStyle);
        _extractorButton.AddThemeStyleboxOverride("pressed", _normalStyle);
        _extractorButton.Pressed += () => SelectTool(WorkerType.Extractor);
        hbox.AddChild(_extractorButton);

        // === Кнопка Экскаватора ===
        _excavatorButton = new Button();
        _excavatorButton.CustomMinimumSize = new Vector2(140, 60);
        _excavatorButton.AddThemeStyleboxOverride("normal", _normalStyle);
        _excavatorButton.AddThemeStyleboxOverride("hover", _normalStyle);
        _excavatorButton.AddThemeStyleboxOverride("pressed", _normalStyle);
        _excavatorButton.Pressed += () => SelectTool(WorkerType.Excavator);
        hbox.AddChild(_excavatorButton);

        UpdateButtons();
    }

    public override void _Process(double delta)
    {
        // Обновляем состояние кнопок (лимиты, разблокировки)
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        if (_diggerButton == null) return;

        int activeDiggers = DiggingManager.Instance.GetActiveDiggersCount();
        int maxDiggers = DiggingManager.Instance.GetMaxDiggers();
        _diggerButton.Text = $"⛏️ Копатель ({activeDiggers}/{maxDiggers})";
        _diggerButton.Disabled = (activeDiggers >= maxDiggers) && SelectedTool != WorkerType.Digger;

        int activeExtractors = DiggingManager.Instance.GetActiveExtractorsCount();
        int maxExtractors = DiggingManager.Instance.GetMaxExtractors();
        _extractorButton.Text = $"🔨 Извлекатель ({activeExtractors}/{maxExtractors})";
        _extractorButton.Disabled = (activeExtractors >= maxExtractors) && SelectedTool != WorkerType.Extractor;

        bool excavatorUnlocked = DiggingManager.Instance.IsExcavatorUnlocked();
        _excavatorButton.Text = excavatorUnlocked ? "🚜 Экскаватор (100 🪙)" : "🚜 🔒 Экскаватор";
        _excavatorButton.Disabled = !excavatorUnlocked;

        // Обновляем стили (выбран/не выбран)
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