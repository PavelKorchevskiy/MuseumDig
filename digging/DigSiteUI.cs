using Godot;

public partial class DigSiteUI : CanvasLayer
{
    private Label _coinsLabel;
    private Label _locationLabel;

    public override void _Ready()
    {
        this.Layer = 50; // Поверх игры, но под инвентарем
        CreateTopBar();
    }

    public override void _Process(double delta)
    {
        if (Wallet.Instance != null && _coinsLabel != null)
        {
            _coinsLabel.Text = $"{Wallet.Instance.GetCoins()} 🪙";
        }
    }

        private void CreateTopBar()
    {
        // Верхняя панель
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

        var hbox = new HBoxContainer();
        topPanel.AddChild(hbox);

        // === Левая часть: Название локации ===
        var location = LocationSystem.Instance.GetActiveLocation();
        _locationLabel = new Label();
        _locationLabel.Text = location != null ? $"📍 {location.DisplayName}" : "Раскопки";
        _locationLabel.AddThemeFontSizeOverride("font_size", 18);
        _locationLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        hbox.AddChild(_locationLabel);

        // === РАСПОРКА: занимает всё свободное пространство между левой и правой частями ===
        var spacer = new Control();
        spacer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(spacer);

        // === Правая часть: Монеты и кнопка возврата ===
        var rightBox = new HBoxContainer();
        rightBox.AddThemeConstantOverride("separation", 30);
        hbox.AddChild(rightBox);

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

    private void OnReturnPressed()
    {
        GD.Print("[DigSiteUI] Возврат в музей");
        GetTree().ChangeSceneToFile("res://Museum.tscn");
    }
}