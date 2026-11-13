using Godot;
using System;

public partial class DigSiteUI : CanvasLayer
{
    private Label _coinsLabel;
    private Label _locationLabel;
    private HBoxContainer _mainHBox; // Верхняя строка (локация + находки + монеты)
    private Control _spacer;
    private int _lastCoins = -1;

    public override void _Ready()
    {
        this.Layer = 50;
        CreateTopBar();
        CreateReturnButton();
    }

    public override void _Process(double delta)
    {
        if (Wallet.Instance != null && _coinsLabel != null)
        {
            int currentCoins = Wallet.Instance.GetCoins();

            // Обновляем текст ТОЛЬКО при изменении
            if (currentCoins != _lastCoins)
            {
                _lastCoins = currentCoins;
                _coinsLabel.Text = $"{currentCoins} 🪙";
            }
        }
    }

    private void CreateTopBar()
    {
        var topPanel = new PanelContainer();
        topPanel.AnchorLeft = 0;
        topPanel.AnchorRight = 1;
        topPanel.AnchorTop = 0;
        // Высота — по содержимому (одна строка): кнопка возврата переехала вниз экрана
        topPanel.OffsetBottom = 0;

        var style = MobileUI.MakeStyle(MobileUI.PanelColor);
        style.CornerRadiusTopLeft = 0;
        style.CornerRadiusTopRight = 0;
        style.CornerRadiusBottomLeft = 0;
        style.CornerRadiusBottomRight = 0;
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 10;
        style.ContentMarginBottom = 10;
        topPanel.AddThemeStyleboxOverride("panel", style);
        AddChild(topPanel);

        var rows = new VBoxContainer();
        rows.AddThemeConstantOverride("separation", 8);
        topPanel.AddChild(rows);

        // === Строка 1: локация + находки + монеты ===
        _mainHBox = new HBoxContainer();
        _mainHBox.AddThemeConstantOverride("separation", 10);
        rows.AddChild(_mainHBox);

        var location = LocationSystem.Instance.GetActiveLocation();
        _locationLabel = MobileUI.CreateLabel(
            location != null ? $"📍 {location.DisplayName}" : LocalizationManager.Tr("ui.digging.title"),
            MobileUI.BodyFontSize, MobileUI.TitleColor);
        _mainHBox.AddChild(_locationLabel);

        // Основная распорка (LootSummaryBar встаёт перед ней, см. AddLootSummaryBar)
        _spacer = new Control();
        _spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _mainHBox.AddChild(_spacer);

        _coinsLabel = MobileUI.CreateLabel("0 🪙", MobileUI.BodyFontSize, Colors.Gold);
        _mainHBox.AddChild(_coinsLabel);
    }

    // ===== КНОПКА ВОЗВРАТА В МУЗЕЙ =====
    // Отдельная квадратная кнопка со стрелкой в правом нижнем углу, над
    // панелью инструментов (DiggingUI, слой ниже) — под большой палец.

    private const float ReturnButtonSize = 76f;
    private const float ReturnButtonMargin = 14f;
    private const float ToolsPanelHeight = 114f; // см. bottomContainer в DigSite._Ready
    private const string ReturnIconPath = "res://assets/ui/icons/return.png";

    private void CreateReturnButton()
    {
        var button = new Button();
        button.FocusMode = Control.FocusModeEnum.None;
        button.TooltipText = LocalizationManager.Tr("ui.digging.return_to_museum");

        button.AnchorLeft = 1;
        button.AnchorRight = 1;
        button.AnchorTop = 1;
        button.AnchorBottom = 1;
        button.OffsetRight = -ReturnButtonMargin;
        button.OffsetLeft = -ReturnButtonMargin - ReturnButtonSize;
        button.OffsetBottom = -ToolsPanelHeight - ReturnButtonMargin;
        button.OffsetTop = -ToolsPanelHeight - ReturnButtonMargin - ReturnButtonSize;
        button.PivotOffset = new Vector2(ReturnButtonSize / 2f, ReturnButtonSize / 2f);

        var normal = MobileUI.MakeStyle(MobileUI.PrimaryColor, 20, 2, MobileUI.PrimaryColor.Lightened(0.35f));
        var pressed = MobileUI.MakeStyle(MobileUI.PrimaryColor.Lightened(0.2f), 20, 2, MobileUI.PrimaryColor.Lightened(0.5f));
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", normal);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        // Иконка: своя картинка, если она есть в проекте, иначе стрелка рисуется кодом
        Control icon;
        if (ResourceLoader.Exists(ReturnIconPath))
        {
            icon = new TextureRect
            {
                Texture = GD.Load<Texture2D>(ReturnIconPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
        }
        else
        {
            icon = new ReturnArrowIcon();
        }
        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        icon.OffsetLeft = 14;
        icon.OffsetTop = 14;
        icon.OffsetRight = -14;
        icon.OffsetBottom = -14;
        button.AddChild(icon);

        button.Pressed += OnReturnPressed;
        TutorialTargets.Register("dig.return", button);
        MobileUI.AttachPressAnimation(button);
        AddChild(button);
    }

    /// <summary>
    /// Вставляет панель находок справа от названия локации.
    /// </summary>
    public void AddLootSummaryBar(LootSummaryBar lootBar)
    {
        if (_mainHBox == null || _spacer == null) return;

        // Вставляем lootBar перед spacer (то есть справа от локации)
        int spacerIndex = _spacer.GetIndex();
        _mainHBox.AddChild(lootBar);
        _mainHBox.MoveChild(lootBar, spacerIndex); // Перемещаем перед spacer
    }

    /// <summary>
    /// Анимация сбора: картинка находки подпрыгивает на месте и летит в
    /// плашку находок, уменьшаясь до размера ячейки. Рисуется на этом же
    /// CanvasLayer, поэтому координаты — экранные, камера на неё не влияет.
    /// </summary>
    public void FlyToLootBar(Texture2D texture, Vector2 from, float startSize, Vector2 to, Action onArrive)
    {
        const float endSize = 44f;
        startSize = Mathf.Clamp(startSize, 40f, 110f);

        var icon = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(startSize, startSize),
            PivotOffset = new Vector2(startSize, startSize) / 2f,
            ZIndex = 100
        };
        icon.Position = from - icon.Size / 2f;
        AddChild(icon);

        float endScale = endSize / startSize;
        Vector2 endPosition = to - icon.Size / 2f;

        var tween = icon.CreateTween();

        // 1. Подпрыгнуть и чуть увеличиться — "взяли в руку"
        tween.TweenProperty(icon, "scale", new Vector2(1.3f, 1.3f), 0.12f)
             .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(icon, "position", icon.Position + new Vector2(0, -18f), 0.12f);

        // 2. Полёт в плашку
        tween.TweenProperty(icon, "position", endPosition, 0.45f)
             .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        tween.Parallel().TweenProperty(icon, "scale", new Vector2(endScale, endScale), 0.45f)
             .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);

        tween.TweenCallback(Callable.From(() =>
        {
            icon.QueueFree();
            onArrive?.Invoke();
        }));
    }

    private void OnReturnPressed()
    {
        GetTree().ChangeSceneToFile("res://Museum.tscn");
    }
}

/// <summary>
/// Стрелка "назад" (разворот влево), нарисованная линиями — запасной вариант
/// на случай, если в проекте нет картинки res://assets/ui/icons/return.png.
/// </summary>
public partial class ReturnArrowIcon : Control
{
    public override void _Draw()
    {
        float s = Mathf.Min(Size.X, Size.Y);
        Vector2 origin = (Size - new Vector2(s, s)) / 2f;
        Vector2 P(float x, float y) => origin + new Vector2(x * s, y * s);

        Color color = Colors.White;
        float width = Mathf.Max(4f, s * 0.11f);

        // Верхняя линия (со стрелкой слева) -> дуга справа -> нижняя линия
        Vector2 arcCenter = P(0.62f, 0.52f);
        float radius = s * 0.22f;

        DrawLine(P(0.16f, 0.30f), P(0.62f, 0.30f), color, width, true);
        DrawArc(arcCenter, radius, -Mathf.Pi / 2f, Mathf.Pi / 2f, 24, color, width, true);
        DrawLine(P(0.62f, 0.74f), P(0.34f, 0.74f), color, width, true);

        // Скругляем стыки и концы линий
        DrawCircle(P(0.62f, 0.30f), width / 2f, color);
        DrawCircle(P(0.62f, 0.74f), width / 2f, color);
        DrawCircle(P(0.34f, 0.74f), width / 2f, color);

        // Наконечник
        DrawColoredPolygon(new[]
        {
            P(0.02f, 0.30f),
            P(0.26f, 0.10f),
            P(0.26f, 0.50f)
        }, color);
    }
}
