using Godot;

public partial class MuseumBottomBar : Control
{
    // Размеры
    private const float SmallButtonSize = 116f;
    private const float LargeButtonSize = 152f;
    private const float SmallIconSize = 64f;
    private const float LargeIconSize = 84f;
    private const float ButtonSpacing = 10f;
    private const float PanelPadding = 16f;
    
    // Состояния кнопок
    private const float PressedScale = 1.12f;
    private const float PressAnimDuration = 0.1f;
    private const float ReleaseAnimDuration = 0.15f;

    private Button _questBtn, _progressBtn, _digBtn, _shopBtn, _inventoryBtn;
    private HBoxContainer _container;

    public override void _Ready()
    {
        Name = "MuseumBottomBar";
        MouseFilter = MouseFilterEnum.Ignore;
        
        // Анкоры: прижато к низу, на всю ширину
        AnchorLeft = 0;
        AnchorRight = 1;
        AnchorTop = 1;
        AnchorBottom = 1;
        OffsetTop = -180;
        
        BuildPanel();
    }

    private void BuildPanel()
    {
var (smallSize, largeSize, spacing) = CalculateButtonSizes();
        
        _container = new HBoxContainer();
        _container.Alignment = BoxContainer.AlignmentMode.Center;
        _container.AddThemeConstantOverride("separation", (int)spacing);
        _container.AnchorLeft = 0;
        _container.AnchorRight = 1;
        _container.AnchorTop = 0;
        _container.AnchorBottom = 1;
        
        AddChild(_container);

        // Иконки пропорциональны размеру кнопки (60% от кнопки)
        float smallIcon = smallSize * 0.55f;
        float largeIcon = largeSize * 0.55f;

        _questBtn = CreateButton("res://assets/ui/icons/quest.png", smallSize, smallIcon);
        _progressBtn = CreateButton("res://assets/ui/icons/progress.png", smallSize, smallIcon);
        _digBtn = CreateButton("res://assets/ui/icons/dig.png", largeSize, largeIcon);
        _shopBtn = CreateButton("res://assets/ui/icons/shop.png", smallSize, smallIcon);
        _inventoryBtn = CreateButton("res://assets/ui/icons/inventory.png", smallSize, smallIcon);

        _container.AddChild(_questBtn);
        _container.AddChild(_progressBtn);
        _container.AddChild(_digBtn);
        _container.AddChild(_shopBtn);
        _container.AddChild(_inventoryBtn);

        // === Подключаем действия ===
        _questBtn.Pressed += () => GD.Print("[BottomBar] Задания (заглушка)");
        _progressBtn.Pressed += () => {
            var progressUI = ProgressUI.Instance;
            if (progressUI != null && GodotObject.IsInstanceValid(progressUI))
                progressUI.Open();
        };
        _digBtn.Pressed += OnDigPressed;
       _shopBtn.Pressed += () => {
            var museum = GetTree().CurrentScene as Museum;
            if (museum != null)
            {
                // Ищем через CanvasLayer UI
                var uiLayer = museum.GetNodeOrNull<CanvasLayer>("UI");
                var shopUI = uiLayer?.GetNodeOrNull<MuseumShopUI>("MuseumShop");
                if (shopUI != null) 
                    shopUI.Visible = true;
                else
                    GD.PrintErr("[BottomBar] MuseumShopUI не найден!");
            }
        };
        _inventoryBtn.Pressed += () => {
            var invUI = InventoryUI.Instance;
            if (invUI != null && GodotObject.IsInstanceValid(invUI))
                invUI.Visible = true;
        };

        // === Подключаем анимации нажатия ===
        AttachPressAnimation(_questBtn);
        AttachPressAnimation(_progressBtn);
        AttachPressAnimation(_digBtn);
        AttachPressAnimation(_shopBtn);
        AttachPressAnimation(_inventoryBtn);
    }

        /// <summary>
    /// Вычисляет размеры кнопок в зависимости от ширины экрана.
    /// </summary>
    private (float smallSize, float largeSize, float spacing) CalculateButtonSizes()
    {
        float screenWidth = GetViewport().GetVisibleRect().Size.X;
        float availableWidth = screenWidth - PanelPadding * 2;  // отступы по бокам
        
        // 5 кнопок: 4 маленькие + 1 большая (коэффициент 1.3)
        // Формула: 4*s + 1.3*s + 4*spacing = available
        float spacing = 8f;
        float smallSize = (availableWidth - 4 * spacing) / 5.3f;
        float largeSize = smallSize * 1.3f;
        
        // Ограничиваем максимальный размер
        smallSize = Mathf.Min(smallSize, 116f);
        largeSize = Mathf.Min(largeSize, 152f);
        
        GD.Print($"[BottomBar] Экран: {screenWidth}, размеры: small={smallSize:F0}, large={largeSize:F0}");
        
        return (smallSize, largeSize, spacing);
    }

     private Button CreateButton(string iconPath, float buttonSize, float iconSize)
    {
        var button = new Button();
        button.CustomMinimumSize = new Vector2(buttonSize, buttonSize);
        button.PivotOffset = new Vector2(buttonSize / 2f, buttonSize / 2f);
        button.MouseFilter = MouseFilterEnum.Stop;

         button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        button.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        
        // Отключаем весь встроенный рендер
        button.Text = "";
        button.AddThemeFontOverride("font", new FontVariation());
        button.AddThemeColorOverride("font_color", Colors.Transparent);

        // Стиль фона
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.15f, 0.15f, 0.2f, 0.85f);
        style.CornerRadiusTopLeft = 18;
        style.CornerRadiusTopRight = 18;
        style.CornerRadiusBottomLeft = 18;
        style.CornerRadiusBottomRight = 18;
        style.BorderWidthLeft = 2;
        style.BorderWidthTop = 2;
        style.BorderWidthRight = 2;
        style.BorderWidthBottom = 2;
        style.BorderColor = new Color(0.4f, 0.4f, 0.5f, 0.6f);

        button.AddThemeStyleboxOverride("normal", style);
        button.AddThemeStyleboxOverride("hover", style);
        button.AddThemeStyleboxOverride("pressed", style);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());

        // === ВАЖНО: CenterContainer гарантирует центрирование ===
        var center = new CenterContainer();
        center.MouseFilter = MouseFilterEnum.Ignore;
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var icon = new TextureRect();
        icon.CustomMinimumSize = new Vector2(iconSize, iconSize);
        icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        icon.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        icon.MouseFilter = MouseFilterEnum.Ignore;
        icon.Size = new Vector2(iconSize, iconSize);

        if (ResourceLoader.Exists(iconPath))
        {
            icon.Texture = GD.Load<Texture2D>(iconPath);
        }

        center.AddChild(icon);
        button.AddChild(center);

        return button;
    }

    /// <summary>
    /// Подключает анимацию увеличения при нажатии.
    /// </summary>
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

    private void OnDigPressed()
    {
        if (LocationSystem.Instance.HasActiveDiggingSession())
        {
            GetTree().ChangeSceneToFile("res://DigSite.tscn");
        }
        else
        {
            var invUI = InventoryUI.Instance;
            if (invUI != null && GodotObject.IsInstanceValid(invUI))
            {
                invUI.SwitchToTicketsTab();
                invUI.Visible = true;
            }
        }
    }
}