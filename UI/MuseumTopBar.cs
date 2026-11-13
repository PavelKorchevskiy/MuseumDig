using Godot;

public partial class MuseumTopBar : Control
{
    // Размеры
    private const float PanelHeight = 68f;
    private const float SettingsBtnSize = 56f;
    private const float StatIconSize = 40f;
    private const float PanelPadding = 16f;
    
    private Button _settingsBtn;
    private Label _visitorsLabel;
    private Label _coinsLabel;

    public override void _Ready()
    {
        Name = "MuseumTopBar";
        MouseFilter = MouseFilterEnum.Ignore;
        
        AnchorLeft = 0;
        AnchorRight = 1;
        AnchorTop = 0;
        AnchorBottom = 0;
        OffsetBottom = PanelHeight;
        
        BuildPanel();
        UpdateStats();
    }

    private void BuildPanel()
    {
        // Полупрозрачный фон
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.75f);
        panelStyle.CornerRadiusBottomLeft = 12;
        panelStyle.CornerRadiusBottomRight = 12;
        
        var panelContainer = new PanelContainer();
        panelContainer.AnchorLeft = 0;
        panelContainer.AnchorRight = 1;
        panelContainer.AnchorTop = 0;
        panelContainer.AnchorBottom = 1;
        panelContainer.AddThemeStyleboxOverride("panel", panelStyle);
        panelContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(panelContainer);

        var hbox = new HBoxContainer();
        hbox.Alignment = BoxContainer.AlignmentMode.Center;
        hbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        hbox.AddThemeConstantOverride("separation", 16);
        
        // Внутренние отступы через MarginContainer
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", (int)PanelPadding);
        margin.AddThemeConstantOverride("margin_right", (int)PanelPadding);
        margin.AddChild(hbox);
        panelContainer.AddChild(margin);

        // === Слева: кнопка шестерёнки ===
        _settingsBtn = new Button();
        _settingsBtn.CustomMinimumSize = new Vector2(SettingsBtnSize, SettingsBtnSize);
        _settingsBtn.PivotOffset = new Vector2(SettingsBtnSize / 2f, SettingsBtnSize / 2f);
        
        var settingsStyle = new StyleBoxFlat();
        settingsStyle.BgColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
        settingsStyle.CornerRadiusTopLeft = 12;
        settingsStyle.CornerRadiusTopRight = 12;
        settingsStyle.CornerRadiusBottomLeft = 12;
        settingsStyle.CornerRadiusBottomRight = 12;
        
        _settingsBtn.AddThemeStyleboxOverride("normal", settingsStyle);
        _settingsBtn.AddThemeStyleboxOverride("hover", settingsStyle);
        _settingsBtn.AddThemeStyleboxOverride("pressed", settingsStyle);
        _settingsBtn.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        _settingsBtn.Text = "";
        _settingsBtn.AddThemeFontOverride("font", new FontVariation());
        
        // === ИСПРАВЛЕНО: TextureRect должен занимать всю кнопку ===
        var settingsIcon = new TextureRect();
        settingsIcon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        settingsIcon.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
        settingsIcon.AnchorLeft = 0;
        settingsIcon.AnchorRight = 1;
        settingsIcon.AnchorTop = 0;
        settingsIcon.AnchorBottom = 1;
        settingsIcon.OffsetLeft = 8;   // Отступ от краёв
        settingsIcon.OffsetRight = -8;
        settingsIcon.OffsetTop = 8;
        settingsIcon.OffsetBottom = -8;
        settingsIcon.MouseFilter = MouseFilterEnum.Ignore;
        
        var settingsPath = "res://assets/ui/icons/settings.png";
        if (ResourceLoader.Exists(settingsPath))
        {
            settingsIcon.Texture = GD.Load<Texture2D>(settingsPath);
            GD.Print($"[TopBar] ✅ Шестерёнка загружена: {settingsPath}");
        }
        else
        {
            GD.PrintErr($"[TopBar] ❌ Шестерёнка НЕ найдена: {settingsPath}");
        }
        
        _settingsBtn.AddChild(settingsIcon);
        
        _settingsBtn.Pressed += ToggleSettingsMenu;
        TutorialTargets.Register("top.settings", _settingsBtn);
        AttachPressAnimation(_settingsBtn, SettingsBtnSize);
        
        hbox.AddChild(_settingsBtn);

        // === Центр: посетители ===
        var visitorsBox = CreateStatBox("res://assets/ui/icons/visitor_icon.png");
        _visitorsLabel = visitorsBox.Label;
        hbox.AddChild(visitorsBox.Container);

        // Spacer, чтобы монеты были справа
        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(spacer);

        // === Справа: монеты ===
        var coinsBox = CreateStatBox("res://assets/ui/icons/coin.png");
        _coinsLabel = coinsBox.Label;
        hbox.AddChild(coinsBox.Container);
        TutorialTargets.Register("top.coins", coinsBox.Container);

        CreateSettingsMenu();
    }

    // ===== МЕНЮ ШЕСТЕРЁНКИ =====
    // Выпадает под шестерёнкой. Пока в нём один пункт — справка-обучение;
    // сюда же позже лягут язык и звук.

    private PanelContainer _settingsMenu;

    private void CreateSettingsMenu()
    {
        _settingsMenu = new PanelContainer();
        _settingsMenu.Visible = false;
        _settingsMenu.Position = new Vector2(PanelPadding, PanelHeight + 8f);

        var style = MobileUI.MakeStyle(MobileUI.PanelColor, 16, 2, MobileUI.RowBorderColor);
        style.ContentMarginLeft = 10;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 10;
        style.ContentMarginBottom = 10;
        _settingsMenu.AddThemeStyleboxOverride("panel", style);
        AddChild(_settingsMenu);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        _settingsMenu.AddChild(vbox);

        var tutorialBtn = MobileUI.CreateButton(
            "?   " + LocalizationManager.Tr("ui.settings.tutorial"),
            MobileUI.PrimaryColor, MobileUI.RowButtonHeight, 220f);
        tutorialBtn.Pressed += () =>
        {
            _settingsMenu.Visible = false;
            TutorialSystem.Instance.StartGuide(this);
        };
        vbox.AddChild(tutorialBtn);
    }

    private void ToggleSettingsMenu()
    {
        if (_settingsMenu != null) _settingsMenu.Visible = !_settingsMenu.Visible;
    }

    private (HBoxContainer Container, Label Label) CreateStatBox(string iconPath)
    {
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 8);
        hbox.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        hbox.MouseFilter = MouseFilterEnum.Ignore;

        var icon = new TextureRect();
        icon.CustomMinimumSize = new Vector2(StatIconSize, StatIconSize);
        icon.Size = new Vector2(StatIconSize, StatIconSize);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.MouseFilter = MouseFilterEnum.Ignore;

        if (ResourceLoader.Exists(iconPath))
        {
            icon.Texture = GD.Load<Texture2D>(iconPath);
        }
        hbox.AddChild(icon);

        var label = new Label();
        label.Text = "0";
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 2);
        label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        label.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(label);

        return (hbox, label);
    }

    private void AttachPressAnimation(Button button, float size)
    {
        Tween currentTween = null;
        button.ButtonDown += () =>
        {
            currentTween?.Kill();
            currentTween = CreateTween();
            currentTween.TweenProperty(button, "scale", 
                new Vector2(0.9f, 0.9f), 0.1f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        };
        button.ButtonUp += () =>
        {
            currentTween?.Kill();
            currentTween = CreateTween();
            currentTween.TweenProperty(button, "scale", 
                Vector2.One, 0.15f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);
        };
    }

    /// <summary>
    /// Вызывается для обновления счётчиков.
    /// </summary>
    public void UpdateStats()
    {
        // Посетители
        int current = VisitorManager.Instance?.GetActiveVisitorsCount() ?? 0;
        int max = MuseumSystem.Instance?.CalculateMaxVisitors() ?? 0;
        _visitorsLabel.Text = $"{current}/{max}";

        // Монеты через Wallet
        int coins = Wallet.Instance?.GetCoins() ?? 0;
        _coinsLabel.Text = coins.ToString("N0");  // Формат: 536 875
    }

    public override void _Process(double delta)
    {
        // Обновляем статистику каждый кадр (можно оптимизировать, вызывая реже)
        UpdateStats();
    }
}