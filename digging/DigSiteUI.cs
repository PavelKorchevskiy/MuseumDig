using Godot;

public partial class DigSiteUI : CanvasLayer
{
    private Label _coinsLabel;
    private Label _locationLabel;
    private HBoxContainer _mainHBox; // === НОВОЕ: Ссылка на основной контейнер ===
    private Control _spacer; // === НОВОЕ: Ссылка на распорку ===
private int _lastCoins = -1;
    public override void _Ready()
    {
        this.Layer = 50;
        CreateTopBar();
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
        topPanel.OffsetBottom = 60;
        
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.05f, 0.05f, 0.08f, 0.9f);
        style.ContentMarginLeft = 20;
        style.ContentMarginRight = 20;
        style.ContentMarginTop = 10;
        style.ContentMarginBottom = 10;
        topPanel.AddThemeStyleboxOverride("panel", style);
        AddChild(topPanel);

        _mainHBox = new HBoxContainer(); // === Сохраняем ссылку ===
        topPanel.AddChild(_mainHBox);

        // Левая часть: Название локации
        var location = LocationSystem.Instance.GetActiveLocation();
        _locationLabel = new Label();
        _locationLabel.Text = location != null ? $"📍 {location.DisplayName}" : "Раскопки";
        _locationLabel.AddThemeFontSizeOverride("font_size", 18);
        _locationLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        _mainHBox.AddChild(_locationLabel);

        // === НОВОЕ: Маленькая распорка после локации ===
        var spacerAfterLocation = new Control();
        spacerAfterLocation.CustomMinimumSize = new Vector2(20, 0);
        _mainHBox.AddChild(spacerAfterLocation);

        // Основная распорка (сохраняем ссылку)
        _spacer = new Control();
        _spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _mainHBox.AddChild(_spacer);

        // Правая часть: Монеты и кнопка
        var rightBox = new HBoxContainer();
        rightBox.AddThemeConstantOverride("separation", 30);
        _mainHBox.AddChild(rightBox);

        _coinsLabel = new Label();
        _coinsLabel.Text = "0 🪙";
        _coinsLabel.AddThemeFontSizeOverride("font_size", 18);
        _coinsLabel.AddThemeColorOverride("font_color", Colors.Gold);
        rightBox.AddChild(_coinsLabel);

        var returnButton = new Button();
        returnButton.Text = "🏛️ Вернуться в музей";
        returnButton.CustomMinimumSize = new Vector2(180, 40);
        returnButton.Pressed += OnReturnPressed;
        rightBox.AddChild(returnButton);
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
        
        GD.Print("[DigSiteUI] LootSummaryBar вставлен справа от локации");
    }

    private void OnReturnPressed()
    {
        GD.Print("[DigSiteUI] Возврат в музей");
        GetTree().ChangeSceneToFile("res://Museum.tscn");
    }
}