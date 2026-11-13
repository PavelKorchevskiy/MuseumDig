using Godot;
using System;

/// <summary>
/// Всё, что обучение рисует на экране: затемнение с "окном" под подсвеченный
/// элемент, золотая рамка, панель с портретом профессора и текстом, кнопки
/// (стрелки и крестик — без надписей).
///
/// Лежит прямо в корне дерева на самом верхнем слое, поэтому переживает
/// смену сцен (музей <-> раскопки) и перекрывает любые окна игры.
///
/// Блокировка нажатий: экран закрыт четырьмя прозрачными панелями вокруг
/// цели. Они перехватывают все нажатия, а в "окно" между ними нажатие
/// проходит к настоящей кнопке. Камера музея тоже считает эти панели
/// интерфейсом (CameraController.IsPointOverBlockingUi) и не двигается.
/// </summary>
public partial class TutorialOverlay : CanvasLayer
{
    public static TutorialOverlay Instance { get; private set; }

    private const string PortraitFolder = "res://assets/tutorial/professor/";
    private const float PortraitSize = 128f;      // 256px-арт выводится ровно вдвое меньше
    private const float ScreenMargin = 12f;
    private const float TopBarHeight = 76f;       // верхняя плашка музея/раскопок
    private const float BottomBarHeight = 190f;   // нижняя панель музея
    private const float HolePadding = 6f;

    private static readonly Color GoldColor = new Color(1.0f, 0.82f, 0.25f);

    // Затемнение: 4 панели вокруг цели + заглушка на саму цель
    private readonly ColorRect[] _shields = new ColorRect[4];
    private ColorRect _holeBlocker;
    private Panel _highlight;
    private StyleBoxFlat _glowStyle;
    private StyleBoxFlat _outlineStyle;

    // Диалог
    private PanelContainer _dialog;
    private TextureRect _portrait;
    private Label _nameLabel;
    private Label _textLabel;
    private Label _counterLabel;
    private Button _prevButton;
    private Button _nextButton;
    private HBoxContainer _navRow;
    private ConfirmOverlay _skipConfirm;

    private int _shownVersion = -1;
    private double _stepShownAt = 0;
    private float _pulseTime = 0f;

    /// <summary>Создаёт оверлей, если его ещё нет.</summary>
    public static void Ensure(SceneTree tree)
    {
        if (Instance != null && GodotObject.IsInstanceValid(Instance)) return;
        if (tree == null) return;

        Instance = new TutorialOverlay { Name = "TutorialOverlay" };
        tree.Root.AddChild(Instance);
    }

    public override void _Ready()
    {
        Layer = 300;
        Visible = false;

        for (int i = 0; i < _shields.Length; i++)
        {
            _shields[i] = CreateShield();
        }

        _holeBlocker = CreateShield();
        _holeBlocker.Color = Colors.Transparent;

        CreateHighlight();
        CreateDialog();

        _skipConfirm = new ConfirmOverlay(this,
            LocalizationManager.Tr("ui.tutorial.skip"), MobileUI.NeutralColor);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    // =================================================================
    //  Построение
    // =================================================================

    private ColorRect CreateShield()
    {
        var shield = new ColorRect();
        shield.MouseFilter = Control.MouseFilterEnum.Stop;
        shield.GuiInput += OnShieldInput;
        AddChild(shield);
        return shield;
    }

    private void CreateHighlight()
    {
        _highlight = new Panel { MouseFilter = Control.MouseFilterEnum.Ignore };

        // Обычная подсветка: золотая рамка с мягким свечением наружу (тень
        // StyleBox) и едва заметной заливкой
        _glowStyle = new StyleBoxFlat
        {
            BgColor = new Color(1f, 0.85f, 0.3f, 0.10f),
            BorderColor = GoldColor,
            ShadowColor = new Color(1f, 0.78f, 0.15f, 0.55f),
            ShadowSize = 14
        };
        _glowStyle.SetBorderWidthAll(4);
        _glowStyle.SetCornerRadiusAll(16);

        // Только контур (TutorialStep.OutlineOnly): без заливки и свечения
        _outlineStyle = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = GoldColor,
            DrawCenter = false
        };
        _outlineStyle.SetBorderWidthAll(4);
        _outlineStyle.SetCornerRadiusAll(16);

        _highlight.AddThemeStyleboxOverride("panel", _glowStyle);
        AddChild(_highlight);
    }

    private void CreateDialog()
    {
        _dialog = new PanelContainer();
        _dialog.MouseFilter = Control.MouseFilterEnum.Stop;

        var style = MobileUI.MakeStyle(MobileUI.PanelColor, 18, 2, GoldColor);
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 10;
        style.ContentMarginBottom = 10;
        _dialog.AddThemeStyleboxOverride("panel", style);
        AddChild(_dialog);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        _dialog.AddChild(hbox);

        // Портрет. Пиксель-арт: без сглаживания, размер кратен исходному.
        _portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(PortraitSize, PortraitSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        hbox.AddChild(_portrait);

        var vbox = new VBoxContainer();
        vbox.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddThemeConstantOverride("separation", 6);
        hbox.AddChild(vbox);

        // Шапка: имя + крестик "пропустить"
        var header = new HBoxContainer();
        vbox.AddChild(header);

        _nameLabel = MobileUI.CreateLabel(
            LocalizationManager.Tr("tutorial.professor_name"), MobileUI.SmallFontSize, GoldColor);
        _nameLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _nameLabel.VerticalAlignment = VerticalAlignment.Center;
        header.AddChild(_nameLabel);

        var skipButton = CreateIconButton(TutorialIcon.IconKind.Cross, "res://assets/ui/icons/close.png",
            MobileUI.NeutralColor, 46f);
        skipButton.Pressed += OnSkipPressed;
        header.AddChild(skipButton);

        _textLabel = MobileUI.CreateLabel("", MobileUI.BodyFontSize, Colors.White, wrap: true);
        _textLabel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(_textLabel);

        // Навигация: ◀  3/12  ▶
        _navRow = new HBoxContainer();
        _navRow.Alignment = BoxContainer.AlignmentMode.End;
        _navRow.AddThemeConstantOverride("separation", 10);
        vbox.AddChild(_navRow);

        _prevButton = CreateIconButton(TutorialIcon.IconKind.ArrowLeft, "res://assets/ui/icons/arrow_prev.png",
            MobileUI.NeutralColor, 58f);
        _prevButton.Pressed += () => TutorialSystem.Instance.Back();
        _navRow.AddChild(_prevButton);

        _counterLabel = MobileUI.CreateLabel("", MobileUI.SmallFontSize, MobileUI.MutedColor, center: true);
        _counterLabel.CustomMinimumSize = new Vector2(64, 0);
        _counterLabel.VerticalAlignment = VerticalAlignment.Center;
        _navRow.AddChild(_counterLabel);

        _nextButton = CreateIconButton(TutorialIcon.IconKind.ArrowRight, "res://assets/ui/icons/arrow_next.png",
            MobileUI.PrimaryColor, 58f);
        _nextButton.Pressed += () => TutorialSystem.Instance.Advance();
        _navRow.AddChild(_nextButton);
    }

    /// <summary>Квадратная кнопка без текста: своя картинка, если она есть в проекте, иначе значок рисуется кодом.</summary>
    private Button CreateIconButton(TutorialIcon.IconKind kind, string texturePath, Color color, float size)
    {
        var button = MobileUI.CreateButton("", color, size, size);

        Control icon;
        if (ResourceLoader.Exists(texturePath))
        {
            icon = new TextureRect
            {
                Texture = GD.Load<Texture2D>(texturePath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
        }
        else
        {
            icon = new TutorialIcon { Kind = kind };
        }

        icon.MouseFilter = Control.MouseFilterEnum.Ignore;
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        float inset = size * 0.26f;
        icon.OffsetLeft = inset;
        icon.OffsetTop = inset;
        icon.OffsetRight = -inset;
        icon.OffsetBottom = -inset;
        button.AddChild(icon);
        return button;
    }

    // =================================================================
    //  Кадр
    // =================================================================

    public override void _Process(double delta)
    {
        var tutorial = TutorialSystem.Instance;
        tutorial.Tick();

        var step = tutorial.CurrentStep;
        if (step == null)
        {
            if (Visible)
            {
                Visible = false;
                _skipConfirm.Hide();
            }
            _shownVersion = -1;
            return;
        }

        Visible = true;

        if (_shownVersion != tutorial.StepVersion)
        {
            _shownVersion = tutorial.StepVersion;
            _stepShownAt = Time.GetTicksMsec() / 1000.0;
            ShowStep(tutorial, step);
        }

        _pulseTime += (float)delta;
        Layout(step);
    }

    private void ShowStep(TutorialSystem tutorial, TutorialStep step)
    {
        _textLabel.Text = LocalizationManager.Tr(step.TextKey);
        _highlight.AddThemeStyleboxOverride("panel", step.OutlineOnly ? _outlineStyle : _glowStyle);

        // Портрет: нужная эмоция -> neutral -> без портрета
        string path = PortraitFolder + step.Emotion + ".png";
        if (!ResourceLoader.Exists(path)) path = PortraitFolder + "neutral.png";

        if (ResourceLoader.Exists(path))
        {
            _portrait.Texture = GD.Load<Texture2D>(path);
            _portrait.Visible = true;
        }
        else
        {
            _portrait.Visible = false;
        }

        bool isGuide = tutorial.Mode == TutorialMode.Guide;

        // Стрелки: в справке всегда, в основном обучении — только на шагах-репликах
        _navRow.Visible = isGuide || step.TapToContinue;
        _prevButton.Visible = isGuide;
        _prevButton.Disabled = tutorial.StepIndex <= 0;
        _counterLabel.Visible = isGuide;
        _counterLabel.Text = $"{tutorial.StepIndex + 1}/{tutorial.StepCount}";
    }

    private void Layout(TutorialStep step)
    {
        Vector2 screen = GetViewport().GetVisibleRect().Size;

        // Свободный шаг: игрок действует сам — ничего не блокируем и не
        // подсвечиваем, на экране только панель с подсказкой.
        if (step.FreePlay)
        {
            foreach (var shield in _shields) shield.Visible = false;
            _holeBlocker.Visible = false;
            _highlight.Visible = false;

            float freeWidth = screen.X - ScreenMargin * 2f;
            _dialog.Size = new Vector2(freeWidth, 0);
            _dialog.Position = new Vector2(ScreenMargin, TopBarHeight + ScreenMargin);
            return;
        }

        bool hasTarget = TutorialTargets.TryGetRect(step.TargetId, out Rect2 target);
        if (hasTarget)
        {
            target = target.Grow(HolePadding).Intersection(new Rect2(Vector2.Zero, screen));
            hasTarget = target.Size.X > 1 && target.Size.Y > 1;
        }

        // Действие (нужно нажать) затемняем сильнее, чтобы взгляд шёл к цели
        bool isAction = hasTarget && step.AllowTargetInput;
        var dim = new Color(0f, 0f, 0f, isAction ? 0.5f : 0.3f);

        if (hasTarget)
        {
            SetRect(_shields[0], new Rect2(0, 0, screen.X, target.Position.Y), dim);                                   // сверху
            SetRect(_shields[1], new Rect2(0, target.End.Y, screen.X, screen.Y - target.End.Y), dim);                  // снизу
            SetRect(_shields[2], new Rect2(0, target.Position.Y, target.Position.X, target.Size.Y), dim);              // слева
            SetRect(_shields[3], new Rect2(target.End.X, target.Position.Y, screen.X - target.End.X, target.Size.Y), dim); // справа

            // Цель только показываем — нажатие в "окно" тоже перехватываем
            _holeBlocker.Visible = !step.AllowTargetInput;
            if (_holeBlocker.Visible)
            {
                _holeBlocker.Position = target.Position;
                _holeBlocker.Size = target.Size;
            }

            _highlight.Visible = true;
            _highlight.Position = target.Position;
            _highlight.Size = target.Size;

            // Пульсация, как у свечения находок на раскопках
            float pulse = 0.5f + 0.5f * Mathf.Sin(_pulseTime * 4f);
            _highlight.Modulate = step.OutlineOnly
                ? Colors.White
                : new Color(1f, 1f, 1f, 0.6f + 0.4f * pulse);
        }
        else
        {
            // Цели нет (или её ещё не видно) — закрываем весь экран
            SetRect(_shields[0], new Rect2(Vector2.Zero, screen), dim);
            for (int i = 1; i < _shields.Length; i++) _shields[i].Visible = false;
            _holeBlocker.Visible = false;
            _highlight.Visible = false;
        }

        // Диалог: обычно сверху; если цель в верхней половине — снизу, чтобы её не закрыть
        float width = screen.X - ScreenMargin * 2f;
        _dialog.Size = new Vector2(width, 0);              // высота — по содержимому
        float height = Mathf.Max(_dialog.Size.Y, _dialog.GetCombinedMinimumSize().Y);

        bool placeBottom = hasTarget && target.GetCenter().Y < screen.Y * 0.5f;
        float y = placeBottom
            ? screen.Y - BottomBarHeight - height - ScreenMargin
            : TopBarHeight + ScreenMargin;

        // Если диалог всё же наехал на цель — отодвигаем на другую сторону от неё
        if (hasTarget && new Rect2(ScreenMargin, y, width, height).Intersects(target))
        {
            float above = target.Position.Y - height - ScreenMargin;
            float below = target.End.Y + ScreenMargin;
            y = above >= ScreenMargin ? above : Mathf.Min(below, screen.Y - height - ScreenMargin);
        }

        _dialog.Position = new Vector2(ScreenMargin, y);
    }

    private static void SetRect(ColorRect rect, Rect2 area, Color color)
    {
        bool visible = area.Size.X > 0.5f && area.Size.Y > 0.5f;
        rect.Visible = visible;
        if (!visible) return;

        rect.Position = area.Position;
        rect.Size = area.Size;
        rect.Color = color;
    }

    // =================================================================
    //  Ввод
    // =================================================================

    private void OnShieldInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb) return;
        if (mb.ButtonIndex != MouseButton.Left || mb.Pressed) return;

        var tutorial = TutorialSystem.Instance;
        var step = tutorial.CurrentStep;
        if (step == null) return;

        // Тап по затемнению листает реплику только в основном обучении:
        // в справке шаги листаются стрелками, чтобы не пролистать случайно.
        if (tutorial.Mode != TutorialMode.Main || !step.TapToContinue) return;

        // Защита от "протыкания": тот же тап, что закрыл прошлый шаг
        if (Time.GetTicksMsec() / 1000.0 - _stepShownAt < 0.35) return;

        tutorial.Advance();
    }

    private void OnSkipPressed()
    {
        var tutorial = TutorialSystem.Instance;
        if (tutorial.Mode == TutorialMode.Main)
        {
            // За основное обучение положена награда — переспрашиваем
            _skipConfirm.Show(
                LocalizationManager.TrFormat("ui.tutorial.skip_confirm", TutorialSystem.MainRewardCoins),
                () => TutorialSystem.Instance.Skip());
        }
        else
        {
            tutorial.Skip();
        }
    }
}

/// <summary>
/// Значки кнопок обучения, нарисованные линиями: стрелки, крестик, вопрос.
/// Используются, пока в проекте нет своих картинок.
/// </summary>
public partial class TutorialIcon : Control
{
    public enum IconKind { ArrowLeft, ArrowRight, Cross, Question }

    public IconKind Kind = IconKind.ArrowRight;
    public Color IconColor = Colors.White;

    public override void _Draw()
    {
        float s = Mathf.Min(Size.X, Size.Y);
        Vector2 o = (Size - new Vector2(s, s)) / 2f;
        Vector2 P(float x, float y) => o + new Vector2(x * s, y * s);
        float w = Mathf.Max(3f, s * 0.18f);

        switch (Kind)
        {
            case IconKind.ArrowRight:
                DrawColoredPolygon(new[] { P(0.18f, 0.02f), P(0.92f, 0.5f), P(0.18f, 0.98f) }, IconColor);
                break;

            case IconKind.ArrowLeft:
                DrawColoredPolygon(new[] { P(0.82f, 0.02f), P(0.08f, 0.5f), P(0.82f, 0.98f) }, IconColor);
                break;

            case IconKind.Cross:
                DrawLine(P(0.08f, 0.08f), P(0.92f, 0.92f), IconColor, w, true);
                DrawLine(P(0.92f, 0.08f), P(0.08f, 0.92f), IconColor, w, true);
                break;

            case IconKind.Question:
                DrawArc(P(0.5f, 0.32f), s * 0.24f, -Mathf.Pi, Mathf.Pi * 0.5f, 20, IconColor, w, true);
                DrawLine(P(0.5f, 0.56f), P(0.5f, 0.70f), IconColor, w, true);
                DrawCircle(P(0.5f, 0.90f), w * 0.6f, IconColor);
                break;
        }
    }
}
