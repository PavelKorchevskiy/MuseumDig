using Godot;

public partial class DigShopUI : CanvasLayer
{
    private VBoxContainer _container;
    private Button _pickaxeButton;
    private Button _shovelButton;
    private Button _maxEnergyButton;
    private Button _regenButton;
    private Button _closeButton;
    
    public override void _Ready()
    {
        Layer = 100;
        
        // Создаём контейнер
        _container = new VBoxContainer();
        _container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _container.OffsetLeft = 50;
        _container.OffsetTop = 50;
        _container.OffsetRight = -50;
        _container.OffsetBottom = -50;
        _container.AddThemeConstantOverride("separation", 20);
        
        // Оборачиваем в ScrollContainer
        var scroll = new ScrollContainer();
        scroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(scroll);
        scroll.AddChild(_container);
        
        _container.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        _container.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        
        // Заголовок
        var title = new Label();
        title.Text = "⛏️ УЛУЧШЕНИЯ ДЛЯ РАСКОПОК";
        title.AddThemeFontSizeOverride("font_size", 22);
        title.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f));
        title.HorizontalAlignment = HorizontalAlignment.Center;
        _container.AddChild(title);
        
        // Кнопка кирки
        _pickaxeButton = new Button();
        _pickaxeButton.CustomMinimumSize = new Vector2(0, 60);
        _pickaxeButton.Pressed += OnPickaxePressed;
        _container.AddChild(_pickaxeButton);
        
        // Кнопка лопаты
        _shovelButton = new Button();
        _shovelButton.CustomMinimumSize = new Vector2(0, 60);
        _shovelButton.Pressed += OnShovelPressed;
        _container.AddChild(_shovelButton);
        
        // Разделитель
        _container.AddChild(new HSeparator());
        
        // Кнопка максимальной энергии
        _maxEnergyButton = new Button();
        _maxEnergyButton.CustomMinimumSize = new Vector2(0, 60);
        _maxEnergyButton.Pressed += OnMaxEnergyPressed;
        _container.AddChild(_maxEnergyButton);
        
        // Кнопка регенерации
        _regenButton = new Button();
        _regenButton.CustomMinimumSize = new Vector2(0, 60);
        _regenButton.Pressed += OnRegenPressed;
        _container.AddChild(_regenButton);
        
        // Кнопка закрытия
        _closeButton = new Button();
        _closeButton.Text = "Закрыть";
        _closeButton.CustomMinimumSize = new Vector2(0, 40);
        _closeButton.Pressed += OnClosePressed;
        _container.AddChild(_closeButton);
        
        Visible = false;
        UpdateButtons();
    }
    
    public override void _Process(double delta)
    {
        if (Visible) UpdateButtons();
    }
    
    private void UpdateButtons()
    {
        var upgrades = UpgradeSystem.Instance;
        int coins = Wallet.Instance.GetCoins();
        
        // Кирка
        int pickaxeCost = upgrades.GetPickaxeCost();
        _pickaxeButton.Text = $"⛏️ Кирка Lv.{upgrades.GetPickaxeLevel()}\n" +
                              $"Урон: {upgrades.GetToolDamage(ToolType.Pickaxe)} | Задержка: {upgrades.GetToolDelay(ToolType.Pickaxe):F2}s\n" +
                              $"Стоимость: {pickaxeCost} монет";
        _pickaxeButton.Disabled = coins < pickaxeCost || upgrades.GetPickaxeLevel() >= 20;
        
        // Лопата
        int shovelCost = upgrades.GetShovelCost();
        _shovelButton.Text = $"🔨 Лопата Lv.{upgrades.GetShovelLevel()}\n" +
                             $"Задержка: {upgrades.GetToolDelay(ToolType.Shovel):F2}s | Шанс повреждения: {upgrades.GetToolDamageChance(ToolType.Shovel):P1}\n" +
                             $"Стоимость: {shovelCost} монет";
        _shovelButton.Disabled = coins < shovelCost || upgrades.GetShovelLevel() >= 20;
        
        // Максимальная энергия
        int maxEnergyCost = EnergySystem.Instance.GetMaxEnergyCost();
        _maxEnergyButton.Text = $"⚡ Максимальная энергия Lv.{EnergySystem.Instance.GetEnergyLevel()}\n" +
                                $"Максимум: {EnergySystem.Instance.GetMaxEnergy()}\n" +
                                $"Стоимость: {maxEnergyCost} монет";
        _maxEnergyButton.Disabled = coins < maxEnergyCost || EnergySystem.Instance.GetEnergyLevel() >= 20;
        
        // Регенерация
        int regenCost = EnergySystem.Instance.GetRegenCost();
        _regenButton.Text = $"💚 Регенерация Lv.{EnergySystem.Instance.GetRegenLevel()}\n" +
                            $"Скорость: {EnergySystem.Instance.GetRegenPerMinute()}/мин\n" +
                            $"Стоимость: {regenCost} монет";
        _regenButton.Disabled = coins < regenCost || EnergySystem.Instance.GetRegenLevel() >= 20;
    }
    
    private void OnPickaxePressed() => UpgradeSystem.Instance.TryBuyPickaxe();
    private void OnShovelPressed() => UpgradeSystem.Instance.TryBuyShovel();
    private void OnMaxEnergyPressed() => EnergySystem.Instance.TryBuyMaxEnergy();
    private void OnRegenPressed() => EnergySystem.Instance.TryBuyRegen();
    private void OnClosePressed() => Visible = false;
}