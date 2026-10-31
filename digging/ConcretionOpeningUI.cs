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
        Layer = 200; // Поверх инвентаря
    }

        private void CreateUI()
    {
        // Затемнённый фон
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.7f);
        background.AnchorRight = 1;
        background.AnchorBottom = 1;
        AddChild(background);

        // Панель
        _panel = new PanelContainer();
        _panel.AnchorLeft = 0.5f;
        _panel.AnchorRight = 0.5f;
        _panel.AnchorTop = 0.5f;
        _panel.AnchorBottom = 0.5f;
        _panel.OffsetLeft = -250;
        _panel.OffsetRight = 250;
        _panel.OffsetTop = -300;
        _panel.OffsetBottom = 300;
        
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.15f, 0.15f, 0.2f);
        panelStyle.CornerRadiusTopLeft = 12;
        panelStyle.CornerRadiusTopRight = 12;
        panelStyle.CornerRadiusBottomLeft = 12;
        panelStyle.CornerRadiusBottomRight = 12;
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

        // === НОВОЕ: AnimatedSprite2D для анимации разрушения ===
        var spriteContainer = new Control();
        spriteContainer.CustomMinimumSize = new Vector2(256, 256);
        vbox.AddChild(spriteContainer);

        _concretionSprite = new TextureRect();
        _concretionSprite.CustomMinimumSize = new Vector2(256, 256);
        _concretionSprite.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _concretionSprite.Position = new Vector2(0, 0);
        
                // === Загружаем первый кадр конкреции ===
        string shortName = _concretionId.Replace("_concretion", "");
        string firstFramePath = $"res://assets/museum/items/common/concretion/{shortName}_1.png";
        
        if (ResourceLoader.Exists(firstFramePath))
        {
            _concretionSprite.Texture = GD.Load<Texture2D>(firstFramePath);
        }
        else
        {
            // Fallback на обычную иконку
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

        // Прогресс
        _progressLabel = new Label();
        _progressLabel.Text = $"Ударов: {_currentHits} / {_requiredHits}";
        _progressLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _progressLabel.AddThemeFontSizeOverride("font_size", 18);
        vbox.AddChild(_progressLabel);

        _progressBar = new ProgressBar();
        _progressBar.MaxValue = _requiredHits;
        _progressBar.Value = _currentHits;
        _progressBar.CustomMinimumSize = new Vector2(0, 20);
        vbox.AddChild(_progressBar);

        // Кнопка "Ударить молотком"
        _hitButton = new Button();
        _hitButton.Text = "🔨 Ударить молотком!";
        _hitButton.CustomMinimumSize = new Vector2(0, 50);
        _hitButton.AddThemeFontSizeOverride("font_size", 18);
        _hitButton.Pressed += OnHitButtonPressed;
        vbox.AddChild(_hitButton);

        // Кнопка закрытия
        _closeButton = new Button();
        _closeButton.Text = "Закрыть";
        _closeButton.CustomMinimumSize = new Vector2(0, 35);
        _closeButton.Pressed += CloseUI;
        vbox.AddChild(_closeButton);
    }

    private void OnHitButtonPressed()
    {
        _currentHits++;
        
        // === НОВОЕ: Анимация удара ===
        AnimateHit();
        
        // === НОВОЕ: Переключаем кадр разрушения ===
        UpdateConcretionFrame();

        _progressLabel.Text = $"Ударов: {_currentHits} / {_requiredHits}";
        _progressBar.Value = _currentHits;

        if (_currentHits >= _requiredHits)
        {
            // Небольшая задержка перед раскрытием
            GetTree().CreateTimer(0.5).Timeout += () => OpenConcretion();
        }
    }

    private void AnimateHit()
    {
        // Тряска + вспышка
        var tween = CreateTween();
        tween.SetParallel();
        
        // Тряска
        tween.TweenProperty(_concretionSprite, "rotation", 0.15f, 0.05f);
        tween.TweenProperty(_concretionSprite, "rotation", -0.15f, 0.05f).SetDelay(0.05f);
        tween.TweenProperty(_concretionSprite, "rotation", 0.0f, 0.05f).SetDelay(0.1f);
        
        // Вспышка белым
        tween.TweenProperty(_concretionSprite, "modulate", Colors.White * 1.5f, 0.05f);
        tween.TweenProperty(_concretionSprite, "modulate", Colors.White, 0.1f).SetDelay(0.05f);
    }

        private void UpdateConcretionFrame()
    {
        // Вычисляем, какой кадр показать (1-5)
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

        string innerItemId = RollInnerItem();
        
        if (string.IsNullOrEmpty(innerItemId))
        {
            GD.PrintErr("[ConcretionOpeningUI] Не удалось определить содержимое конкреции!");
            CloseUI();
            return;
        }

        // Удаляем конкрецию из инвентаря
        InventorySystem.Instance.RemoveItem(_concretionId, 1);

        // Добавляем внутренний предмет
        InventorySystem.Instance.AddItem(innerItemId, 1);

        // === НОВОЕ: Анимация появления экспоната ===
        AnimateReveal(innerItemId);
    }

        private void AnimateReveal(string innerItemId)
    {
        var innerResource = GameData.GetResource(innerItemId);
        string resultText = $"Вы нашли: {innerResource?.DisplayName ?? innerItemId}!";
        
        _titleLabel.Text = "✨ Конкреция раскрыта! ✨";
        _progressLabel.Text = resultText;
        _progressLabel.AddThemeColorOverride("font_color", Colors.LimeGreen);
        _hitButton.Visible = false;

        // === НОВОЕ: Загружаем случайный вариант экспоната ===
        string innerIconPath = GetRandomVariantIconPath(innerItemId);
        
        var newTexture = GD.Load<Texture2D>(innerIconPath);

        // Анимация: конкреция исчезает, экспонат появляется
        var tween = CreateTween();
        
        tween.TweenProperty(_concretionSprite, "position:y", -50f, 0.3f);
        tween.Parallel().TweenProperty(_concretionSprite, "modulate:a", 0f, 0.3f);
        
        tween.TweenCallback(Callable.From(() => {
            _concretionSprite.Texture = newTexture;
            _concretionSprite.Position = new Vector2(0, 50);
            _concretionSprite.Modulate = new Color(1, 1, 1, 0);
        }));
        
        tween.TweenProperty(_concretionSprite, "position:y", 0f, 0.3f);
        tween.Parallel().TweenProperty(_concretionSprite, "modulate:a", 1f, 0.3f);
        
        tween.TweenProperty(_concretionSprite, "scale", new Vector2(1.2f, 1.2f), 0.1f);
        tween.TweenProperty(_concretionSprite, "scale", Vector2.One, 0.1f);

        GD.Print($"[ConcretionOpeningUI] Получен предмет: {innerItemId}");
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
        
        // Fallback на обычную иконку
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

        // Если ничего не выпало, возвращаем первый предмет из пула
        return _definition.PossibleContents[0].Resource.Id;
    }

    private void CloseUI()
    {
        QueueFree();
    }
}