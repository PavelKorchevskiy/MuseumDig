using Godot;
using System.Collections.Generic;

public partial class FossilUI : CanvasLayer
{
	private Label _fossilLabel;
	private Label _coinsLabel;
	private Label _energyLabel;
	private ProgressBar _energyBar;
	private Button _shopButton;
	private CanvasLayer _shop;
	private Button _saveQuitButton;
	private CanvasLayer _inventory;
	private Button _inventoryButton;
	private Button _shovelButton;
	private Button _pickaxeButton;
	private CanvasLayer _worldMap;
	private Button _worldMapButton;
	private Button _backToMuseumButton;
	private VBoxContainer _upgradePanel;
private Label _toolNameLabel;
private Label _toolStatsLabel;
private Label _upgradeCostLabel;
private Button _upgradeButton;
	
	public override void _Ready()
{
	// ===== НАСТРОЙКА ФОНА =====
	var background = GetNodeOrNull<PanelContainer>("Background");
	if (background != null)
	{
		background.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		background.OffsetLeft = 10;
		background.OffsetTop = 10;
		background.CustomMinimumSize = new Vector2(260, 0);
		
		// Полупрозрачный фон
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0, 0, 0, 0.6f);
		styleBox.CornerRadiusTopLeft = 8;
		styleBox.CornerRadiusTopRight = 8;
		styleBox.CornerRadiusBottomLeft = 8;
		styleBox.CornerRadiusBottomRight = 8;
		styleBox.ContentMarginLeft = 10;
		styleBox.ContentMarginTop = 10;
		styleBox.ContentMarginRight = 10;
		styleBox.ContentMarginBottom = 10;
		background.AddThemeStyleboxOverride("panel", styleBox);
	}
	
	// ===== НАСТРОЙКА КОНТЕЙНЕРА =====
	var mainContainer = GetNodeOrNull<VBoxContainer>("Background/MainContainer");
	if (mainContainer != null)
	{
		mainContainer.AddThemeConstantOverride("separation", 10);
	}
	
	// ===== ПОИСК УЗЛОВ (с Background/ в начале!) =====
	_fossilLabel = GetNodeOrNull<Label>("Background/MainContainer/TopSection/FossilLabel");
	_coinsLabel = GetNodeOrNull<Label>("Background/MainContainer/TopSection/CoinsLabel");
	_energyLabel = GetNodeOrNull<Label>("Background/MainContainer/EnergySection/EnergyLabel");
	_energyBar = GetNodeOrNull<ProgressBar>("Background/MainContainer/EnergySection/EnergyBar");
	_shopButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/ShopButton");
	_shop = GetNodeOrNull<CanvasLayer>("Shop");
	_inventoryButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/InventoryButton");
	_inventory = GetNodeOrNull<CanvasLayer>("Inventory");
	if (_inventoryButton != null) _inventoryButton.Pressed += OnInventoryPressed;
	_shovelButton = GetNodeOrNull<Button>("Background/MainContainer/ToolsSection/ShovelButton");
	_pickaxeButton = GetNodeOrNull<Button>("Background/MainContainer/ToolsSection/PickaxeButton");
	_backToMuseumButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/BackToMuseumButton");
	if (_backToMuseumButton != null) _backToMuseumButton.Pressed += OnBackToMuseumPressed;

	if (_shovelButton != null) _shovelButton.Pressed += OnShovelPressed;
	if (_pickaxeButton != null) _pickaxeButton.Pressed += OnPickaxePressed;
	_worldMapButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/WorldMapButton");
	_worldMap = GetNodeOrNull<CanvasLayer>("WorldMap");
	if (_worldMapButton != null) _worldMapButton.Pressed += OnWorldMapPressed;
	
	
	// ===== ПРОВЕРКА =====
	if (_fossilLabel == null) GD.PrintErr("⚠️ FossilLabel not found!");
	if (_coinsLabel == null) GD.PrintErr("⚠️ CoinsLabel not found!");
	if (_energyLabel == null) GD.PrintErr("⚠️ EnergyLabel not found!");
	if (_energyBar == null) GD.PrintErr("⚠️ EnergyBar not found!");
	if (_shopButton == null) GD.PrintErr("⚠️ ShopButton not found!");
	if (_shop == null) GD.PrintErr("⚠️ Shop not found!");
	
	// ===== ПОДПИСКА =====
	if (_shopButton != null) _shopButton.Pressed += OnShopPressed;
SetupUpgradeUI();
    UpdateUpgradeUI();


	_saveQuitButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/SaveQuitButton");
if (_saveQuitButton != null) 
{
	_saveQuitButton.Pressed += OnSaveQuitPressed;
}
}
	
	private void OnShopPressed()
{
    GD.Print("[DEBUG] OnShopPressed вызван!");
    GD.Print($"[DEBUG] _shop = {_shop != null}");
    
    if (_shop != null)
    {
        _shop.Visible = true;
        GD.Print("[DEBUG] Shop.Visible = true");
    }
    else
    {
        GD.PrintErr("[DEBUG] _shop равен null!");
    }
}
	
	
	public override void _Process(double delta)
{
	UpdateFossilDisplay();
	UpdateCoinsDisplay();
	UpdateEnergyDisplay();
	UpdateToolButtons();
}


	private void UpdateFossilDisplay()
	{
		if (_fossilLabel == null) return; // ← ЗАЩИТА
		
		var pieces = FossilInventory.Instance.GetAllPieces();
		
		if (pieces.Count == 0)
		{
			_fossilLabel.Text = "No fossils found yet";
			return;
		}
		
		string text = "Fossils:\n";
		foreach (var kvp in pieces)
		{
			string fossilId = kvp.Key;
			int count = kvp.Value.Count;
			string status = FossilInventory.Instance.IsFossilComplete(fossilId) ? "✓" : "";
			text += $"- {fossilId}: {count}/4 {status}\n";
		}
		
		_fossilLabel.Text = text;
	}
	
	private void UpdateCoinsDisplay()
	{
		if (_coinsLabel == null) return; // ← ЗАЩИТА
		
		int coins = Wallet.Instance.GetCoins();
		int income = MuseumSystem.Instance.GetTotalIncomePerSecond();
		_coinsLabel.Text = $"Coins: {coins}\nIncome: {income}/sec";
	}
	
	private void UpdateEnergyDisplay()
	{
		if (_energyLabel == null || _energyBar == null) return; // ← ЗАЩИТА
		if (EnergySystem.Instance == null) return;
		
		int current = EnergySystem.Instance.GetCurrentEnergy();
		int max = EnergySystem.Instance.GetMaxEnergy();
		float ratio = EnergySystem.Instance.GetEnergyRatio();
		
		_energyLabel.Text = $"Energy: {current}/{max}";
		_energyBar.Value = ratio * 100f;
		
		// Меняем цвет бара в зависимости от количества энергии
		if (ratio > 0.5f)
			_energyBar.Modulate = new Color(0.3f, 0.8f, 0.3f); // Зелёный
		else if (ratio > 0.2f)
			_energyBar.Modulate = new Color(0.9f, 0.8f, 0.2f); // Жёлтый
		else
			_energyBar.Modulate = new Color(0.9f, 0.3f, 0.3f); // Красный
	}

private void OnSaveQuitPressed()
{
	GD.Print("Manual save and quit triggered!");
	SaveSystem.Instance.ForceSaveAndQuit();
}

private void OnInventoryPressed()
{
	if (_inventory != null)
	{
		_inventory.Visible = true;
	}
}
private void OnShovelPressed()
{
	ToolSystem.Instance.SetCurrentTool(ToolType.Shovel);
	UpdateToolButtons();
}

private void OnPickaxePressed()
{
	ToolSystem.Instance.SetCurrentTool(ToolType.Pickaxe);
	UpdateToolButtons();
}

private void UpdateToolButtons()
{
	if (ToolSystem.Instance == null) return;
	
	var currentTool = ToolSystem.Instance.GetCurrentToolType();
	
	if (_shovelButton != null)
	{
		// Подсвечиваем активный инструмент
		_shovelButton.Modulate = currentTool == ToolType.Shovel 
			? new Color(1.2f, 1.2f, 1.2f)  // Ярче
			: new Color(0.7f, 0.7f, 0.7f); // Тусклее
		
		// Добавляем индикатор выбора
		_shovelButton.Text = currentTool == ToolType.Shovel 
			? "▶ 🔨 Shovel" 
			: "🔨 Shovel";
	}
	
	if (_pickaxeButton != null)
	{
		_pickaxeButton.Modulate = currentTool == ToolType.Pickaxe 
			? new Color(1.2f, 1.2f, 1.2f) 
			: new Color(0.7f, 0.7f, 0.7f);
		
		_pickaxeButton.Text = currentTool == ToolType.Pickaxe 
			? "▶ ⛏️ Pickaxe" 
			: "⛏️ Pickaxe";
	}
	UpdateUpgradeUI();
}

private void OnWorldMapPressed()
{
	if (_worldMap != null)
	{
		_worldMap.Visible = true;
	}
}

private void OnBackToMuseumPressed()
{
	// Сохраняем перед выходом
	SaveSystem.Instance?.SaveGame();
	
	// Возвращаемся в музей
	GetTree().ChangeSceneToFile("res://Museum.tscn");
}

private T FindChildRecursive<T>(Node parent, string name) where T : Node
{
	foreach (var child in parent.GetChildren())
	{
		if (child.Name == name && child is T result)
			return result;
		
		var found = FindChildRecursive<T>(child, name);
		if (found != null)
			return found;
	}
	return null;
}

    // ===== НАСТРОЙКА UI УЛУЧШЕНИЙ =====
    
       private void SetupUpgradeUI()
    {
        // Создаем панель улучшений как отдельный элемент
        _upgradePanel = new VBoxContainer();
        
        // Якоря: Правый нижний угол (чтобы не перекрывать монеты сверху и основные кнопки)
        _upgradePanel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        _upgradePanel.OffsetLeft = -240;   // Ширина панели
        _upgradePanel.OffsetTop = -160;    // Высота панели
        _upgradePanel.OffsetRight = -20;   // Отступ справа от края экрана
        _upgradePanel.OffsetBottom = -20;  // Отступ снизу от края экрана
        
        // ВАЖНО: Позволяем кликам проходить сквозь пустые места панели, 
        // но сама панель и её кнопки будут ловить клики.
        _upgradePanel.MouseFilter = Control.MouseFilterEnum.Pass;

        // Стиль для панели (полупрозрачный темный фон)
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        panelStyle.CornerRadiusTopLeft = 8;
        panelStyle.CornerRadiusTopRight = 8;
        panelStyle.CornerRadiusBottomLeft = 8;
        panelStyle.CornerRadiusBottomRight = 8;
        panelStyle.ContentMarginLeft = 12;
        panelStyle.ContentMarginTop = 12;
        panelStyle.ContentMarginRight = 12;
        panelStyle.ContentMarginBottom = 12;
        _upgradePanel.AddThemeStyleboxOverride("panel", panelStyle);

        _toolNameLabel = new Label();
        _toolNameLabel.AddThemeFontSizeOverride("font_size", 18);
        _toolNameLabel.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.5f)); // Золотистый
        _upgradePanel.AddChild(_toolNameLabel);

        _toolStatsLabel = new Label();
        _toolStatsLabel.AddThemeFontSizeOverride("font_size", 14);
        _toolStatsLabel.AddThemeColorOverride("font_color", Colors.White);
        _upgradePanel.AddChild(_toolStatsLabel);

        _upgradeCostLabel = new Label();
        _upgradeCostLabel.AddThemeFontSizeOverride("font_size", 14);
        _upgradeCostLabel.AddThemeColorOverride("font_color", new Color(0.7f, 1f, 0.7f)); // Зелёный
        _upgradePanel.AddChild(_upgradeCostLabel);

        _upgradeButton = new Button();
        _upgradeButton.Text = "Upgrade Tool";
        _upgradeButton.CustomMinimumSize = new Vector2(0, 35);
        _upgradeButton.Pressed += OnUpgradeToolPressed;
        _upgradePanel.AddChild(_upgradeButton);

        // Добавляем прямо в корень Background, чтобы не ломать существующую иерархию кнопок
        AddChild(_upgradePanel);
    }

    private void UpdateUpgradeUI()
{
    if (_upgradePanel == null || UpgradeSystem.Instance == null || ToolSystem.Instance == null) return;

    var currentToolType = ToolSystem.Instance.GetCurrentToolType();
    var toolDef = ToolSystem.Instance.GetCurrentTool();
    
    int currentLevel = currentToolType == ToolType.Pickaxe 
        ? UpgradeSystem.Instance.GetPickaxeLevel() 
        : UpgradeSystem.Instance.GetShovelLevel();
        
    int nextLevelCost = currentToolType == ToolType.Pickaxe 
        ? UpgradeSystem.Instance.GetPickaxeCost() 
        : UpgradeSystem.Instance.GetShovelCost();

    int maxLevel = 20;

    // ===== ТЕКУЩИЕ ЗНАЧЕНИЯ =====
    float currentDelay = UpgradeSystem.Instance.GetToolDelay(currentToolType);
    int currentDamage = UpgradeSystem.Instance.GetToolDamage(currentToolType);
    float currentDmgChance = UpgradeSystem.Instance.GetToolDamageChance(currentToolType);

    // ===== ЗНАЧЕНИЯ ДЛЯ СЛЕДУЮЩЕГО УРОВНЯ =====
    // Вычисляем, что будет при уровне +1, используя те же формулы из UpgradeSystem
    float nextDelay = currentDelay * 0.95f;
    int nextDamage = currentDamage + 1;
    float nextDmgChance = currentDmgChance * 0.90f;

    // ===== ОТОБРАЖЕНИЕ =====
    
    // Заголовок с текущим и следующим уровнем
    if (currentLevel >= maxLevel)
    {
        _toolNameLabel.Text = $"{toolDef.DisplayName} (Lv. {currentLevel})";
    }
    else
    {
        _toolNameLabel.Text = $"{toolDef.DisplayName} (Lv. {currentLevel} → {currentLevel + 1})";
    }
    
    // Характеристики с показом прогресса
    string statsText = "";
    
    // Урон
    if (currentLevel >= maxLevel)
    {
        statsText += $"Damage: {currentDamage}";
    }
    else
    {
        statsText += $"Damage: {currentDamage} → {nextDamage}";
    }
    
    // Задержка
    if (currentLevel >= maxLevel)
    {
        statsText += $"\nDelay: {currentDelay:F2}s";
    }
    else
    {
        statsText += $"\nDelay: {currentDelay:F2}s → {nextDelay:F2}s";
    }
    
    // Шанс повреждения (только для лопаты)
    if (currentToolType == ToolType.Shovel)
    {
        if (currentLevel >= maxLevel)
        {
            statsText += $"\nFossil Damage: {currentDmgChance:P1}";
        }
        else
        {
            statsText += $"\nFossil Damage: {currentDmgChance:P1} → {nextDmgChance:P1}";
        }
    }
    
    _toolStatsLabel.Text = statsText;

    // Стоимость и кнопка
    if (currentLevel >= maxLevel)
    {
        _upgradeCostLabel.Text = "MAX LEVEL REACHED";
        _upgradeButton.Disabled = true;
        _upgradeButton.Text = "MAXED";
    }
    else
    {
        _upgradeCostLabel.Text = $"Cost: {nextLevelCost} coins";
        _upgradeButton.Disabled = Wallet.Instance.GetCoins() < nextLevelCost;
        _upgradeButton.Text = "Upgrade";
    }
}

    private void OnUpgradeToolPressed()
    {
        var currentToolType = ToolSystem.Instance.GetCurrentToolType();
        bool success = false;

        if (currentToolType == ToolType.Pickaxe)
        {
            success = UpgradeSystem.Instance.TryBuyPickaxe();
        }
        else
        {
            success = UpgradeSystem.Instance.TryBuyShovel();
        }

        if (success)
        {
            UpdateUpgradeUI(); // Обновляем UI сразу после покупки
        }
    }
}
