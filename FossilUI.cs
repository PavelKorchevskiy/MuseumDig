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

// ===== ТЕСТ InventorySystem =====
GD.Print("=== InventorySystem TEST ===");

// 1. Добавляем 2 хороших куска золота
InventorySystem.Instance.AddItem("gold_nugget", Quality.Good, 2);

// 2. Добавляем 1 хороший и 1 повреждённый череп трицератопса
InventorySystem.Instance.AddItem("triceratops_skull", Quality.Good, 1);
InventorySystem.Instance.AddItem("triceratops_skull", Quality.Damaged, 1);

// 3. Проверяем общее количество черепов (должно быть 2)
GD.Print($"Total triceratops_skull: {InventorySystem.Instance.GetTotalAmount("triceratops_skull")}");

// 4. Проверяем продажу (продаём 1 хороший череп)
int earned = InventorySystem.Instance.SellItem("triceratops_skull", Quality.Good, 1);
GD.Print($"Sold 1 Good Skull for {earned} coins. Wallet now: {Wallet.Instance.GetCoins()}");

// 5. Выводим весь инвентарь
GD.Print("--- Current Inventory ---");
foreach (var item in InventorySystem.Instance.GetAllItems())
{
	var res = GameData.GetResource(item.ResourceId);
	GD.Print($"  {res.DisplayName} x{item.Amount} ({item.Quality})");
}
GD.Print("=========================");

	_saveQuitButton = GetNodeOrNull<Button>("Background/MainContainer/ButtonsSection/SaveQuitButton");
if (_saveQuitButton != null) 
{
	_saveQuitButton.Pressed += OnSaveQuitPressed;
}
}
	
	private void OnShopPressed()
	{
		if (_shop != null)
		{
			_shop.Visible = true;
		}
	}
	
	
	public override void _Process(double delta)
{
	UpdateFossilDisplay();
	UpdateCoinsDisplay();
	UpdateEnergyDisplay();
	UpdateToolButtons();
	// Офлайн-награды теперь показываются только в сцене музея
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
}
