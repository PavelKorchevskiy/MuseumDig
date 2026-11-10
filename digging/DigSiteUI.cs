using Godot;

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
        topPanel.OffsetBottom = 104;

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
            location != null ? $"📍 {location.DisplayName}" : "Раскопки",
            MobileUI.BodyFontSize, MobileUI.TitleColor);
        _mainHBox.AddChild(_locationLabel);

        // Основная распорка (LootSummaryBar встаёт перед ней, см. AddLootSummaryBar)
        _spacer = new Control();
        _spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _mainHBox.AddChild(_spacer);

        _coinsLabel = MobileUI.CreateLabel("0 🪙", MobileUI.BodyFontSize, Colors.Gold);
        _mainHBox.AddChild(_coinsLabel);

        // === Строка 2: кнопка возврата — отдельной строкой на всю ширину,
        // чтобы не тесниться с остальным содержимым верхней плашки ===
        var returnButton = MobileUI.CreateButton(
            MobileUI.Tr("ui.digging.return_to_museum", "🏛️ Вернуться в музей"),
            MobileUI.PrimaryColor, MobileUI.RowButtonHeight, 0, MobileUI.SmallFontSize);
        returnButton.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        returnButton.Pressed += OnReturnPressed;
        rows.AddChild(returnButton);
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

    private void OnReturnPressed()
    {
        GetTree().ChangeSceneToFile("res://Museum.tscn");
    }
}