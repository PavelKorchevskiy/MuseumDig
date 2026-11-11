using Godot;

public partial class ConcretionOpeningUI : CanvasLayer
{
    private string _concretionId;
    private ConcretionDefinition _definition;
    private int _currentHits = 0;
    private int _requiredHits = 5;

    private PanelContainer _panel;
    private TextureRect _concretionSprite;
    private Label _titleLabel;
    private Label _progressLabel;
    private ProgressBar _progressBar;
    private Button _hitButton;
    private Button _closeButton;
    private Button _nextButton;  // === НОВОЕ: Кнопка "Следующая" ===

    // === Размеры кнопки закрытия (как в инвентаре) ===
    private const float CloseButtonSize = 100f;

    public void Setup(string concretionId)
    {
        _concretionId = concretionId;
        _definition = GameData.GetResource(concretionId) as ConcretionDefinition;
        
        if (_definition == null)
        {
            GD.PrintErr($"[ConcretionOpeningUI] Не найдено определение для {concretionId}");
            QueueFree();
            return;
        }

        _requiredHits = _definition.HitsToOpen;
        _currentHits = 0;

        CreateUI();
    }

    public override void _Ready()
    {
        Layer = 200;
    }

    private void CreateUI()
    {
        // Затемнённый фон
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.7f);
        background.AnchorRight = 1;
        background.AnchorBottom = 1;
        AddChild(background);

        // Панель с anchor-подходом
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0.05f;
        _panel.AnchorRight = 0.95f;
        _panel.AnchorTop = 0.05f;
        _panel.AnchorBottom = 0.99f;
        
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.12f, 0.12f, 0.18f, 1.0f);
        panelStyle.CornerRadiusTopLeft = 20;
        panelStyle.CornerRadiusTopRight = 20;
        panelStyle.CornerRadiusBottomLeft = 20;
        panelStyle.CornerRadiusBottomRight = 20;
        panelStyle.ContentMarginLeft = 20;
        panelStyle.ContentMarginRight = 20;
        panelStyle.ContentMarginTop = 20;
        panelStyle.ContentMarginBottom = 20;
        _panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 20);
        _panel.AddChild(vbox);

        // Заголовок
        _titleLabel = new Label();
        _titleLabel.Text = _definition.DisplayName;
        _titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _titleLabel.AddThemeFontSizeOverride("font_size", 24);
        _titleLabel.AddThemeColorOverride("font_color", Colors.Gold);
        vbox.AddChild(_titleLabel);

        // CenterContainer для центрирования изображения
        var spriteContainer = new CenterContainer();
        spriteContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        spriteContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        vbox.AddChild(spriteContainer);

        _concretionSprite = new TextureRect();
        _concretionSprite.CustomMinimumSize = new Vector2(256, 256);
        _concretionSprite.Size = new Vector2(256, 256);
        _concretionSprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _concretionSprite.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _concretionSprite.PivotOffset = new Vector2(128, 128);
        
        // Загружаем первый кадр конкреции
        string shortName = _concretionId.Replace("_concretion", "");
        string firstFramePath = $"res://assets/museum/items/common/concretion/{shortName}_1.png";
        
        if (ResourceLoader.Exists(firstFramePath))
        {
            _concretionSprite.Texture = GD.Load<Texture2D>(firstFramePath);
        }
        else
        {
            string fallbackPath = $"res://assets/museum/items/common/{_concretionId}.png";
            if (ResourceLoader.Exists(fallbackPath))
            {
                _concretionSprite.Texture = GD.Load<Texture2D>(fallbackPath);
            }
            else
            {
                _concretionSprite.Texture = GD.Load<Texture2D>("res://icon.svg");
            }
        }
        
        spriteContainer.AddChild(_concretionSprite);

        // === Прогресс с локализацией ===
        _progressLabel = new Label();
        UpdateProgressLabel();  // Вызываем метод для установки текста
        _progressLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _progressLabel.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(_progressLabel);

        _progressBar = new ProgressBar();
        _progressBar.MaxValue = _requiredHits;
        _progressBar.Value = _currentHits;
        _progressBar.CustomMinimumSize = new Vector2(0, 20);
        vbox.AddChild(_progressBar);

        // Кнопка "Ударить молотком" с локализацией
        _hitButton = new Button();
        string hitButtonText = LocalizationManager.Tr("ui.concretion.hit_button");
        _hitButton.Text = hitButtonText;
        _hitButton.CustomMinimumSize = new Vector2(0, 100);
        _hitButton.AddThemeFontSizeOverride("font_size", 20);
        _hitButton.Pressed += OnHitButtonPressed;
        vbox.AddChild(_hitButton);

        // === НОВОЕ: Кнопка "Следующая" ПЕРЕД кнопкой закрытия (изначально скрыта) ===
        _nextButton = new Button();
        string nextButtonText = LocalizationManager.Tr("ui.concretion.next_button");
        _nextButton.Text = nextButtonText;
        _nextButton.CustomMinimumSize = new Vector2(0, 120);
        _nextButton.AddThemeFontSizeOverride("font_size", 18);
        _nextButton.AddThemeColorOverride("font_color", new Color(0.3f, 0.9f, 0.3f));
        _nextButton.Pressed += OnNextButtonPressed;
        _nextButton.Visible = false;
        vbox.AddChild(_nextButton);

        // Красная кнопка закрытия с крестиком (в самом низу)
        var closeRow = new HBoxContainer();
        closeRow.Alignment = BoxContainer.AlignmentMode.Center;
        vbox.AddChild(closeRow);

        _closeButton = new Button();
        _closeButton.CustomMinimumSize = new Vector2(CloseButtonSize, CloseButtonSize);
        _closeButton.PivotOffset = new Vector2(CloseButtonSize / 2f, CloseButtonSize / 2f);
        
        var closeStyle = new StyleBoxFlat();
        closeStyle.BgColor = new Color(0.8f, 0.25f, 0.25f, 1.0f);
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

        var closeCenter = new CenterContainer();
        closeCenter.MouseFilter = Control.MouseFilterEnum.Ignore;
        closeCenter.SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var closeIcon = new Label();
        closeIcon.Text = "✕";
        closeIcon.AddThemeFontSizeOverride("font_size", 48);
        closeIcon.AddThemeColorOverride("font_color", Colors.White);
        closeCenter.AddChild(closeIcon);
        _closeButton.AddChild(closeCenter);

        _closeButton.Pressed += CloseUI;
        closeRow.AddChild(_closeButton);
    }

    /// <summary>
    /// Обновляет текст прогресса с учётом локализации.
    /// </summary>
    private void UpdateProgressLabel()
    {
        string format = LocalizationManager.Tr("ui.concretion.hits_format");
        _progressLabel.Text = string.Format(format, _currentHits, _requiredHits);
    }

    // Конкреция уже расколота и ждёт раскрытия: лишние удары за эти полсекунды
    // раньше запускали раскрытие повторно и выдавали несколько находок за одну.
    private bool _isOpening = false;

    private void OnHitButtonPressed()
    {
        if (_isOpening) return;

        _currentHits++;
        
        AnimateHit();
        UpdateConcretionFrame();

         UpdateProgressLabel();
        _progressBar.Value = _currentHits;

        if (_currentHits >= _requiredHits)
        {
            _isOpening = true;
            _hitButton.Disabled = true;
            GetTree().CreateTimer(0.5).Timeout += () => OpenConcretion();
        }
    }

    private void AnimateHit()
    {
        var tween = CreateTween();
        tween.SetParallel();
        
        tween.TweenProperty(_concretionSprite, "rotation", 0.15f, 0.05f);
        tween.TweenProperty(_concretionSprite, "rotation", -0.15f, 0.05f).SetDelay(0.05f);
        tween.TweenProperty(_concretionSprite, "rotation", 0.0f, 0.05f).SetDelay(0.1f);
        
        tween.TweenProperty(_concretionSprite, "modulate", Colors.White * 1.5f, 0.05f);
        tween.TweenProperty(_concretionSprite, "modulate", Colors.White, 0.1f).SetDelay(0.05f);
    }

    private void UpdateConcretionFrame()
    {
        int frameIndex = Mathf.Min(_currentHits, 5);
        
        string shortName = _concretionId.Replace("_concretion", "");
        string framePath = $"res://assets/museum/items/common/concretion/{shortName}_{frameIndex}.png";
        
        if (ResourceLoader.Exists(framePath))
        {
            _concretionSprite.Texture = GD.Load<Texture2D>(framePath);
        }
    }

    private void OpenConcretion()
    {
        GD.Print($"[ConcretionOpeningUI] Конкреция раскрыта: {_concretionId}");

        // Находку выдаём только взамен реально лежащей в инвентаре конкреции.
        // Без этой проверки RemoveItem молча не срабатывал, а находка всё равно
        // добавлялась — так получались бесконечные находки.
        if (!HasMoreConcretions())
        {
            GD.PrintErr($"[ConcretionOpeningUI] Конкреции '{_concretionId}' нет в инвентаре, раскрытие отменено");
            InventoryUI.Instance?.RequestRefresh();
            CloseUI();
            return;
        }

        string innerItemId = RollInnerItem();
        
        if (string.IsNullOrEmpty(innerItemId))
        {
            GD.PrintErr("[ConcretionOpeningUI] Не удалось определить содержимое конкреции!");
            CloseUI();
            return;
        }

        InventorySystem.Instance.RemoveItem(_concretionId, 1);
        InventorySystem.Instance.AddItem(innerItemId, 1);
        QuestSystem.Instance.OnConcretionOpened();

        // Список в инвентаре под этим окном обновляем сразу: иначе строка
        // с уже потраченной конкрецией и кнопкой "Раскрыть" оставалась на месте.
        InventoryUI.Instance?.RequestRefresh();

        AnimateReveal(innerItemId);
    }

        private void AnimateReveal(string innerItemId)
    {
        var innerResource = GameData.GetResource(innerItemId);
        
        // === Используем локализацию для заголовка ===
        string revealedText = LocalizationManager.Tr("ui.concretion.revealed");
        _titleLabel.Text = revealedText;
        
        // === Используем локализацию для текста находки ===
        string foundFormat = LocalizationManager.Tr("ui.concretion.found_item");
        string resultText = string.Format(foundFormat, innerResource?.DisplayName ?? innerItemId);
        _progressLabel.Text = resultText;
        _progressLabel.AddThemeColorOverride("font_color", Colors.LimeGreen);
        
        // Скрываем ненужные элементы
        _hitButton.Visible = false;
        _progressBar.Visible = false;

        string innerIconPath = GetRandomVariantIconPath(innerItemId);
        var newTexture = GD.Load<Texture2D>(innerIconPath);

        // Сбрасываем трансформации перед анимацией
        _concretionSprite.Scale = Vector2.One;
        _concretionSprite.Position = Vector2.Zero;

        var tween = CreateTween();
        
        // 1. Fade out конкреции + лёгкое уменьшение
        tween.TweenProperty(_concretionSprite, "modulate:a", 0f, 0.25f);
        tween.Parallel().TweenProperty(_concretionSprite, "scale", new Vector2(0.8f, 0.8f), 0.25f)
               .SetTrans(Tween.TransitionType.Quad)
               .SetEase(Tween.EaseType.In);
        
        // 2. Меняем текстуру (невидимо для пользователя)
        tween.TweenCallback(Callable.From(() => {
            _concretionSprite.Texture = newTexture;
            _concretionSprite.Modulate = new Color(1, 1, 1, 0);
            _concretionSprite.Scale = new Vector2(0.8f, 0.8f);
        }));
        
        // 3. Fade in + появление с эффектом "pop"
        tween.TweenProperty(_concretionSprite, "modulate:a", 1f, 0.25f);
        tween.Parallel().TweenProperty(_concretionSprite, "scale", new Vector2(1.15f, 1.15f), 0.2f)
               .SetTrans(Tween.TransitionType.Back)
               .SetEase(Tween.EaseType.Out);
        
        // 4. Плавное возвращение к нормальному размеру
        tween.TweenProperty(_concretionSprite, "scale", Vector2.One, 0.15f)
               .SetTrans(Tween.TransitionType.Quad)
               .SetEase(Tween.EaseType.Out);

        // 5. Показываем кнопку "Следующая", если есть ещё конкреции
        tween.TweenCallback(Callable.From(() => {
            UpdateNextButtonState();
        }));

        GD.Print($"[ConcretionOpeningUI] Получен предмет: {innerItemId}");
    }

    // === НОВОЕ: Проверяем наличие других конкреций такого же типа ===
    private bool HasMoreConcretions()
    {
        if (InventorySystem.Instance == null) return false;
        
        var item = InventorySystem.Instance.GetItem(_concretionId);
        return item != null && item.Amount > 0;
    }

    // === НОВОЕ: Обновляем видимость кнопки "Следующая" ===
       private void UpdateNextButtonState()
    {
        bool hasMore = HasMoreConcretions();
        _nextButton.Visible = hasMore;
        
        if (hasMore)
        {
            var item = InventorySystem.Instance.GetItem(_concretionId);
            string format = LocalizationManager.Tr("ui.concretion.next_button_with_count");
            _nextButton.Text = string.Format(format, item.Amount);
        }
    }

    // === НОВОЕ: Обработчик кнопки "Следующая" ===
    private void OnNextButtonPressed()
    {
        if (!HasMoreConcretions())
        {
            GD.PrintErr("[ConcretionOpeningUI] Нет больше конкреций!");
            return;
        }

        // Сбрасываем состояние
        _isOpening = false;
        _hitButton.Disabled = false;
        _currentHits = 0;
        _progressBar.Value = 0;
         UpdateProgressLabel();
        _progressLabel.AddThemeColorOverride("font_color", Colors.White);
        
        // Показываем элементы снова
        _hitButton.Visible = true;
        _progressBar.Visible = true;
        _nextButton.Visible = false;
        
        // Обновляем заголовок
        _titleLabel.Text = _definition.DisplayName;
        _titleLabel.AddThemeColorOverride("font_color", Colors.Gold);

        // Загружаем первый кадр
        string shortName = _concretionId.Replace("_concretion", "");
        string firstFramePath = $"res://assets/museum/items/common/concretion/{shortName}_1.png";
        
        if (ResourceLoader.Exists(firstFramePath))
        {
            _concretionSprite.Texture = GD.Load<Texture2D>(firstFramePath);
        }

        // Сбрасываем трансформации
        _concretionSprite.Position = Vector2.Zero;
        _concretionSprite.Scale = Vector2.One;
        _concretionSprite.Modulate = Colors.White;
        _concretionSprite.Rotation = 0f;

        GD.Print($"[ConcretionOpeningUI] Открытие следующей конкреции");
    }

    private string GetRandomVariantIconPath(string resourceId)
    {
        var resource = GameData.GetResource(resourceId);
        
        if (resource is FossilDefinition fossil && fossil.VariantCount > 1)
        {
            int randomVariant = GD.RandRange(1, fossil.VariantCount);
            string variantPath = $"res://assets/museum/items/common/{resourceId}/{randomVariant}.png";
            if (ResourceLoader.Exists(variantPath)) return variantPath;
        }
        
        string commonPath = $"res://assets/museum/items/common/{resourceId}.png";
        if (ResourceLoader.Exists(commonPath)) return commonPath;
        
        return "res://icon.svg";
    }

    private string RollInnerItem()
    {
        if (_definition.PossibleContents == null || _definition.PossibleContents.Count == 0)
        {
            return null;
        }

        foreach (var entry in _definition.PossibleContents)
        {
            if (GD.Randf() < entry.DropChance)
            {
                return entry.Resource.Id;
            }
        }

        return _definition.PossibleContents[0].Resource.Id;
    }

    private void CloseUI()
    {
        InventoryUI.Instance?.RequestRefresh();
        QueueFree();
    }
}