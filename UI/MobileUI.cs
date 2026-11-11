using Godot;
using System;

/// <summary>
/// Общие элементы мобильного интерфейса: тот же стиль, что в InventoryUI
/// (тёмная панель со скруглением, красная кнопка закрытия с крестиком),
/// но с увеличенными шрифтами и кнопками под палец.
/// </summary>
public static class MobileUI
{
    // === Размеры (холст 540x960, на телефоне масштаб ~2x) ===
    public const float PanelPadding = 20f;
    public const float CloseButtonSize = 100f;   // как в InventoryUI
    public const float ButtonHeight = 72f;       // главные кнопки
    public const float RowHeight = 72f;          // строка списка
    public const float RowButtonHeight = 56f;    // кнопка внутри строки

    public const int TitleFontSize = 28;
    public const int BodyFontSize = 20;
    public const int ButtonFontSize = 22;
    public const int SmallFontSize = 18;

    // === Цвета (основа взята из InventoryUI) ===
    public static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.18f, 1.0f);
    public static readonly Color RowColor = new Color(0.18f, 0.18f, 0.25f, 0.9f);
    public static readonly Color RowBorderColor = new Color(0.3f, 0.3f, 0.4f, 0.6f);
    public static readonly Color TitleColor = Colors.Gold;
    public static readonly Color MutedColor = new Color(0.7f, 0.7f, 0.8f);

    public static readonly Color PrimaryColor = new Color(0.25f, 0.45f, 0.75f, 1f);  // переместить / добавить
    public static readonly Color WarningColor = new Color(0.80f, 0.50f, 0.15f, 1f);  // продать / на склад
    public static readonly Color DangerColor = new Color(0.8f, 0.25f, 0.25f, 1f);    // закрыть / убрать
    public static readonly Color NeutralColor = new Color(0.30f, 0.30f, 0.38f, 1f);  // отмена

    // === Камера при открытой панели ===
    public const float FocusZoom = 1.4f;

    // ---------------------------------------------------------------
    // Текст
    // ---------------------------------------------------------------

    /// <summary>
    /// Перевод с запасным текстом. Не пишет ошибки в консоль, если ключа нет в CSV.
    /// </summary>
    public static string Tr(string key, string fallback)
    {
        string translated = TranslationServer.Translate(key);
        return (string.IsNullOrEmpty(translated) || translated == key) ? fallback : translated;
    }

    public static Label CreateLabel(string text, int fontSize = BodyFontSize, Color? color = null,
        bool center = false, bool wrap = false)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color ?? Colors.White);
        if (center) label.HorizontalAlignment = HorizontalAlignment.Center;
        if (wrap)
        {
            label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            // Без минимальной ширины переносимый Label может схлопнуться
            label.CustomMinimumSize = new Vector2(100, 0);
        }
        return label;
    }

    // ---------------------------------------------------------------
    // Панель
    // ---------------------------------------------------------------

    public static StyleBoxFlat MakeStyle(Color bg, int radius = 18, int borderWidth = 0, Color? border = null)
    {
        var style = new StyleBoxFlat { BgColor = bg };
        style.CornerRadiusTopLeft = radius;
        style.CornerRadiusTopRight = radius;
        style.CornerRadiusBottomLeft = radius;
        style.CornerRadiusBottomRight = radius;
        if (borderWidth > 0)
        {
            style.BorderWidthLeft = borderWidth;
            style.BorderWidthTop = borderWidth;
            style.BorderWidthRight = borderWidth;
            style.BorderWidthBottom = borderWidth;
            style.BorderColor = border ?? RowBorderColor;
        }
        return style;
    }

    /// <summary>
    /// Нижняя панель того же вида, что и инвентарь (те же цвета, отступы 5%, скругление 20),
    /// но занимает только низ экрана, чтобы витрина/скелет оставались видны сверху.
    /// topAnchor: 0.075 = как у инвентаря (почти весь экран), 0.5 = нижняя половина.
    /// </summary>
    public static PanelContainer CreateBottomSheet(CanvasLayer layer, float topAnchor, out VBoxContainer content)
    {
        var panel = new PanelContainer();
        panel.AnchorLeft = 0.05f;
        panel.AnchorRight = 0.95f;
        panel.AnchorTop = topAnchor;
        panel.AnchorBottom = 0.99f;
        panel.MouseFilter = Control.MouseFilterEnum.Stop; // клики не уходят в мир под панелью

        var style = MakeStyle(PanelColor, 20);
        style.ContentMarginLeft = PanelPadding;
        style.ContentMarginRight = PanelPadding;
        style.ContentMarginTop = PanelPadding;
        style.ContentMarginBottom = PanelPadding;
        panel.AddThemeStyleboxOverride("panel", style);
        layer.AddChild(panel);

        content = new VBoxContainer();
        content.AddThemeConstantOverride("separation", 12);
        panel.AddChild(content);
        return panel;
    }

    /// <summary>Список с прокруткой пальцем, занимает всё свободное место.</summary>
    public static ScrollContainer CreateScroll(out VBoxContainer list)
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(list);
        return scroll;
    }

    /// <summary>Строка списка (как плитки вкладок в инвентаре). Внутри HBox для содержимого.</summary>
    public static PanelContainer CreateRow(out HBoxContainer hbox)
    {
        var row = new PanelContainer { CustomMinimumSize = new Vector2(0, RowHeight) };
        row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.MouseFilter = Control.MouseFilterEnum.Pass; // не мешаем прокрутке списка пальцем

        var style = MakeStyle(RowColor, 14, 2, RowBorderColor);
        style.ContentMarginLeft = 14;
        style.ContentMarginRight = 10;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        row.AddThemeStyleboxOverride("panel", style);

        hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        row.AddChild(hbox);
        return row;
    }

    public static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            parent.RemoveChild(child); // сразу убираем из дерева, иначе старые строки живут до конца кадра
            child.QueueFree();
        }
    }

    // ---------------------------------------------------------------
    // Кнопки
    // ---------------------------------------------------------------

    public static Button CreateButton(string text, Color bg, float minHeight = ButtonHeight,
        float minWidth = 0f, int fontSize = ButtonFontSize)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(minWidth, minHeight),
            FocusMode = Control.FocusModeEnum.None
        };

        var normal = MakeStyle(bg, 16);
        var pressed = MakeStyle(bg.Lightened(0.2f), 16);
        var disabled = MakeStyle(new Color(0.22f, 0.22f, 0.28f, 1f), 16);

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", normal);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("disabled", disabled);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        button.AddThemeFontSizeOverride("font_size", fontSize);
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeColorOverride("font_hover_color", Colors.White);
        button.AddThemeColorOverride("font_pressed_color", Colors.White);
        button.AddThemeColorOverride("font_focus_color", Colors.White);
        button.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.55f, 0.6f));

        AttachPressAnimation(button);
        return button;
    }

    /// <summary>Красная кнопка с крестиком по центру нижней строки, как в инвентаре.</summary>
    public static Control CreateCloseRow(Action onPressed)
    {
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };

        var button = new Button
        {
            CustomMinimumSize = new Vector2(CloseButtonSize, CloseButtonSize),
            FocusMode = Control.FocusModeEnum.None
        };
        var normal = MakeStyle(DangerColor, 20);
        var pressed = MakeStyle(DangerColor.Lightened(0.2f), 20);
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", normal);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        // Крестик рисуем отдельным Label, как в InventoryUI
        var center = new CenterContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var cross = new Label { Text = "✕", MouseFilter = Control.MouseFilterEnum.Ignore };
        cross.AddThemeFontSizeOverride("font_size", 48);
        cross.AddThemeColorOverride("font_color", Colors.White);
        center.AddChild(cross);
        button.AddChild(center);

        button.Pressed += () => onPressed?.Invoke();
        AttachPressAnimation(button);

        row.AddChild(button);
        return row;
    }

    /// <summary>
    /// "Сжатие" кнопки при нажатии. Публичный — используется не только
    /// кнопками, созданными через CreateButton, но и своими стилизованными
    /// кнопками (например, трёхсостоянием DiggingUI: обычная/выбрана/недоступна).
    /// </summary>
    public static void AttachPressAnimation(Button button)
    {
        button.Resized += () => button.PivotOffset = button.Size / 2f;

        Tween tween = null;
        button.ButtonDown += () =>
        {
            tween?.Kill();
            tween = button.CreateTween();
            tween.TweenProperty(button, "scale", new Vector2(0.95f, 0.95f), 0.08f);
        };
        button.ButtonUp += () =>
        {
            tween?.Kill();
            tween = button.CreateTween();
            tween.TweenProperty(button, "scale", Vector2.One, 0.12f)
                 .SetTrans(Tween.TransitionType.Back)
                 .SetEase(Tween.EaseType.Out);
        };
    }

    // ---------------------------------------------------------------
    // Камера (CameraController не меняется, используем его ZoomToObject / ResetZoom)
    // ---------------------------------------------------------------

    /// <summary>
    /// Наводит камеру на мебель и сдвигает кадр так, чтобы она оказалась НАД панелью,
    /// а не под ней. sheetTop - верхний якорь панели (0..1).
    /// </summary>
    public static void FocusCameraOnFurniture(Node context, Room room, PlacedFurniture placed, float sheetTop)
    {
        var museum = context.GetTree().CurrentScene as Museum;
        var camera = museum?.GetNodeOrNull<CameraController>("Camera2D");
        if (camera == null) return;

        // Опорная точка - нижний центр footprint'а (так же, как рисует GlobalRoomViewUI)
        int anchorX = room.GlobalOffset.X + placed.Position.X + placed.Size.X - 1;
        int anchorY = room.GlobalOffset.Y + placed.Position.Y + placed.Size.Y - 1;
        var iso = IsoUtils.GridToIso(anchorX, anchorY);
        Vector2 worldPos = new Vector2(
            MuseumConstants.GridOffsetX + iso.X + IsoUtils.TileWidth / 2f,
            MuseumConstants.GridOffsetY + iso.Y + IsoUtils.TileHeight
        );

        // Хотим, чтобы низ объекта был чуть выше верхнего края панели.
        // Камера смотрит в центр экрана (0.5), значит центр нужно сместить вниз.
        float viewHeight = context.GetViewport().GetVisibleRect().Size.Y;
        float targetFraction = sheetTop - 0.04f;
        worldPos.Y += (0.5f - targetFraction) * viewHeight / FocusZoom;

        camera.ZoomToObject(worldPos, FocusZoom);
    }

    public static void ResetCamera(Node context)
    {
        var museum = context.GetTree().CurrentScene as Museum;
        var camera = museum?.GetNodeOrNull<CameraController>("Camera2D");
        camera?.ResetZoom();
    }
}

/// <summary>
/// Окно подтверждения под палец (вместо ConfirmationDialog, у которого мелкие кнопки).
/// Обычный C#-класс, не нод: создаётся один раз и показывается через Show().
/// </summary>
public class ConfirmOverlay
{
    private readonly ColorRect _root;
    private readonly Label _message;
    private Action _onConfirm;

    public ConfirmOverlay(CanvasLayer parent, string confirmText, Color confirmColor)
    {
        // Затемнение на весь экран, перехватывает клики
        _root = new ColorRect { Color = new Color(0, 0, 0, 0.6f), Visible = false };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Stop;
        parent.AddChild(_root);

        // Окно по центру: ширина от якорей, высота по содержимому
        var panel = new PanelContainer();
        panel.AnchorLeft = 0.08f;
        panel.AnchorRight = 0.92f;
        panel.AnchorTop = 0.5f;
        panel.AnchorBottom = 0.5f;
        panel.GrowVertical = Control.GrowDirection.Both;

        var style = MobileUI.MakeStyle(MobileUI.PanelColor, 20);
        style.ContentMarginLeft = MobileUI.PanelPadding;
        style.ContentMarginRight = MobileUI.PanelPadding;
        style.ContentMarginTop = MobileUI.PanelPadding;
        style.ContentMarginBottom = MobileUI.PanelPadding;
        panel.AddThemeStyleboxOverride("panel", style);
        _root.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        panel.AddChild(vbox);

        _message = MobileUI.CreateLabel("", MobileUI.BodyFontSize + 2, Colors.White, center: true, wrap: true);
        vbox.AddChild(_message);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 12);
        vbox.AddChild(buttons);

        var cancelBtn = MobileUI.CreateButton(MobileUI.Tr("ui.common.cancel", "Отмена"), MobileUI.NeutralColor);
        cancelBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        cancelBtn.Pressed += Hide;
        buttons.AddChild(cancelBtn);

        var confirmBtn = MobileUI.CreateButton(confirmText, confirmColor);
        confirmBtn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        confirmBtn.Pressed += () =>
        {
            var action = _onConfirm;
            Hide();
            action?.Invoke();
        };
        buttons.AddChild(confirmBtn);
    }

    public void Show(string message, Action onConfirm)
    {
        _message.Text = message;
        _onConfirm = onConfirm;
        _root.Visible = true;
    }

    public void Hide()
    {
        _root.Visible = false;
        _onConfirm = null;
    }
}